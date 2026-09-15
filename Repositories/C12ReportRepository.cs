using System.Data;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C12ReportRepository(IConnectionStringProvider connectionStringProvider) : IC12ReportRepository
{
    private static readonly IReadOnlyDictionary<string, string> FixedMappings =
        new Dictionary<string, string>(StringComparer.Ordinal)
        { ["11910"]="0201", ["11920"]="0281", ["11930"]="0220", ["11309"]="0230" };
    private static readonly HashSet<string> FixedIdentityMappings = new(
        new[] { "15HD3","15HD4","1013P","150MI","15OPD","1OR4F","12710","1510M","15WD1" }
            .Concat(Enumerable.Range(1,27).Select(value=>$"1OR{value:00}"))
            .Concat(Enumerable.Range(1,4).Select(value=>$"1CV{value:00}"))
            .Concat(Enumerable.Range(1,4).Select(value=>$"1XA{value:00}"))
            .Concat(Enumerable.Range(1,20).Select(value=>$"1ED{value:00}")), StringComparer.Ordinal);

    public async Task<string?> ResolveOldSectionCodeAsync(string code, CancellationToken ct)
    {
        code = code.Trim().ToUpperInvariant();
        if (FixedMappings.TryGetValue(code, out string? mapped)) return mapped;
        if (FixedIdentityMappings.Contains(code)) return code;
        await using OracleConnection connection = CreateConnection(); await connection.OpenAsync(ct);
        await using OracleCommand command = CreateCommand(connection, C12Sql.SectionMapping);
        Add(command, "NewSectionCode", OracleDbType.Varchar2, code);
        return Trim(await command.ExecuteScalarAsync(ct));
    }

    public async Task<string?> ResolveMedicalRecordNoAsync(string identity, CancellationToken ct)
    {
        await using OracleConnection connection = CreateConnection(); await connection.OpenAsync(ct);
        await using OracleCommand command = CreateCommand(connection, C12Sql.ResolveMedicalRecord);
        Add(command, "InputIdentity", OracleDbType.Varchar2, identity);
        return Trim(await command.ExecuteScalarAsync(ct));
    }

    public async Task<IReadOnlyList<C12VisitRow>> QueryVisitsAsync(C12ReportRequest request, string mrNo, CancellationToken ct)
    {
        await using OracleConnection connection = CreateConnection(); await connection.OpenAsync(ct);
        await using OracleCommand command = CreateCommand(connection, request.Source == C12Source.Inpatient ? C12Sql.InpatientVisits : C12Sql.OutpatientVisits);
        Add(command,"StartDate",OracleDbType.Char,request.StartDate); Add(command,"EndDate",OracleDbType.Char,request.EndDate);
        Add(command,"ApplySection",OracleDbType.Int32,string.IsNullOrWhiteSpace(request.OldSectionCode)?0:1);
        Add(command,"SectionPrefix",OracleDbType.Varchar2,string.IsNullOrWhiteSpace(request.OldSectionCode)?DBNull.Value:request.OldSectionCode.Trim()+"%");
        command.Parameters.Add(CreateMedicalRecordParameter(mrNo));
        if (request.Source == C12Source.OutpatientAndEmergency) Add(command,"RoomType",OracleDbType.Int32,request.RoomType);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<C12VisitRow>();
        while (await reader.ReadAsync(ct)) rows.Add(new(new(
            Text(reader,0),Text(reader,1),Text(reader,2),Convert.ToDecimal(reader.GetValue(3))),
            Text(reader,4),Text(reader,5),Text(reader,6),Text(reader,7)=="1"));
        return rows;
    }

    public async Task<IReadOnlyList<C12ChargeRow>> QueryVisitChargesAsync(C12Source source, C12VisitKey key, CancellationToken ct)
    {
        await using OracleConnection connection = CreateConnection(); await connection.OpenAsync(ct);
        await using OracleCommand command = CreateCommand(connection, source == C12Source.Inpatient ? C12Sql.InpatientCharges : C12Sql.OutpatientCharges);
        Add(command,"VisitDate",OracleDbType.Char,key.Date); Add(command,"VisitTime",OracleDbType.Char,key.Time);
        Add(command,"VisitRoom",OracleDbType.Char,key.Room); Add(command,"VisitNo",OracleDbType.Decimal,key.Number);
        if (source == C12Source.OutpatientAndEmergency) Add(command,"IsEmergencyTransfer",OracleDbType.Int32,key.Room.Trim()=="0000"?1:0);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<C12ChargeRow>();
        while (await reader.ReadAsync(ct)) rows.Add(new(Text(reader,0),Decimal(reader,1),Decimal(reader,2),Decimal(reader,3)));
        return rows;
    }

    public async Task<IReadOnlyDictionary<string,string?>> QuerySectionNamesAsync(IEnumerable<string> sectionNos, CancellationToken ct)
    {
        string[] values = sectionNos.Select(x=>x.Trim()).Where(x=>x.Length>0).Distinct(StringComparer.Ordinal).ToArray();
        if (values.Length == 0) return new Dictionary<string,string?>();
        string names = string.Join(",", values.Select((_,i)=>$":Section{i}"));
        await using OracleConnection connection = CreateConnection(); await connection.OpenAsync(ct);
        await using OracleCommand command = CreateCommand(connection,string.Format(C12Sql.SectionName,names));
        for(int i=0;i<values.Length;i++) Add(command,$"Section{i}",OracleDbType.Char,values[i]);
        await using OracleDataReader reader=await command.ExecuteReaderAsync(ct); var result=new Dictionary<string,string?>(StringComparer.Ordinal);
        while(await reader.ReadAsync(ct)) result.TryAdd(Text(reader,0),Text(reader,1)); return result;
    }

    public async Task<C12PatientRow?> QueryPatientAsync(string mrNo, CancellationToken ct)
    {
        await using OracleConnection connection=CreateConnection(); await connection.OpenAsync(ct);
        await using OracleCommand command=CreateCommand(connection,C12Sql.Patient); Add(command,"MrNo",OracleDbType.Varchar2,mrNo);
        await using OracleDataReader reader=await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new(Text(reader,0),Text(reader,1),Text(reader,2)) : null;
    }

    private OracleConnection CreateConnection()=>new(connectionStringProvider.GetConnectionString());
    private static OracleCommand CreateCommand(OracleConnection c,string sql)=>new(sql,c){BindByName=true};
    private static void Add(OracleCommand c,string n,OracleDbType t,object value)=>c.Parameters.Add(n,t,value,ParameterDirection.Input);
    internal static OracleParameter CreateMedicalRecordParameter(string medicalRecordNo) => new("MrNo",OracleDbType.Char,10,medicalRecordNo,ParameterDirection.Input);
    private static string Text(OracleDataReader r,int i)=>r.IsDBNull(i)?string.Empty:Convert.ToString(r.GetValue(i))?.Trim()??string.Empty;
    private static string? Trim(object? value)=>value is null or DBNull?null:Convert.ToString(value)?.Trim();
    private static decimal? Decimal(OracleDataReader r,int i)=>r.IsDBNull(i)?null:Convert.ToDecimal(r.GetValue(i));
}
