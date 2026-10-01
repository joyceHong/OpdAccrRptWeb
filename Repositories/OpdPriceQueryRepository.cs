using System.Data;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class OpdPriceQueryRepository(IConnectionStringProvider connections) : IOpdPriceQueryRepository
{
    public int CountVisits(string mrNo, string date, string? section)
    {
        using OracleConnection connection = CreateConnection(); connection.Open();
        using OracleCommand command = CreateCommand(connection, OpdPriceQuerySql.VisitCount);
        AddVisitFilters(command, mrNo, date, section);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public async Task<IReadOnlyList<OpdPriceVisitSource>> QueryVisitsAsync(string mrNo,
        string date, string? section, int offset, int pageSize, CancellationToken token)
    {
        await using OracleConnection connection = CreateConnection(); await connection.OpenAsync(token);
        await using OracleCommand command = CreateCommand(connection, OpdPriceQuerySql.Visits);
        AddVisitFilters(command, mrNo, date, section); Add(command,"Offset",OracleDbType.Int32,offset);
        Add(command,"PageEnd",OracleDbType.Int32,checked(offset+pageSize));
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        var rows = new List<OpdPriceVisitSource>();
        while (await reader.ReadAsync(token)) rows.Add(Visit(reader)); return rows;
    }

    public async Task<OpdPriceVisitSource?> QueryVisitAsync(OpdPriceVisitKey key, CancellationToken token)
    {
        await using OracleConnection connection=CreateConnection(); await connection.OpenAsync(token);
        await using OracleCommand command=CreateCommand(connection,OpdPriceQuerySql.Visit); AddKey(command,key);
        await using OracleDataReader reader=await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token)?Visit(reader):null;
    }

    public async Task<OpdPricePatient?> QueryPatientAsync(string mrNo,CancellationToken token)
    {
        await using OracleConnection connection=CreateConnection(); await connection.OpenAsync(token);
        await using OracleCommand command=CreateCommand(connection,OpdPriceQuerySql.Patient);
        Add(command,"MrNo",OracleDbType.Varchar2,mrNo);
        await using OracleDataReader reader=await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token)?new(Text(reader,0),Text(reader,1),Text(reader,2),Text(reader,3)):null;
    }

    public Task<IReadOnlyList<OpdPriceChargeSource>> QueryDrugsAsync(OpdPriceVisitKey key,bool showDc,CancellationToken token) =>
        QueryChargesAsync(OpdPriceQuerySql.Drugs,key,showDc,true,token);
    public Task<IReadOnlyList<OpdPriceChargeSource>> QueryOrdersAsync(OpdPriceVisitKey key,bool showDc,CancellationToken token) =>
        QueryChargesAsync(OpdPriceQuerySql.Orders,key,showDc,false,token);

    private async Task<IReadOnlyList<OpdPriceChargeSource>> QueryChargesAsync(string sql,
        OpdPriceVisitKey key,bool showDc,bool drug,CancellationToken token)
    {
        await using OracleConnection connection=CreateConnection(); await connection.OpenAsync(token);
        await using OracleCommand command=CreateCommand(connection,sql); AddKey(command,key);
        Add(command,"ShowDc",OracleDbType.Int32,showDc?1:0);
        await using OracleDataReader reader=await command.ExecuteReaderAsync(token);
        var rows=new List<OpdPriceChargeSource>();
        while(await reader.ReadAsync(token))
        {
            int shift=drug?0:1;
            string code=Text(reader,0); string extended=drug?string.Empty:Text(reader,1);
            rows.Add(new(drug,code,extended,Text(reader,1+shift),Decimal(reader,2+shift),
                Decimal(reader,3+shift),Text(reader,4+shift),Decimal(reader,5+shift),Decimal(reader,6+shift),
                Decimal(reader,7+shift),Decimal(reader,8+shift),Text(reader,9+shift),Text(reader,10+shift),
                Text(reader,11+shift),Text(reader,12+shift),Text(reader,13+shift),
                [Decimal(reader,14+shift),Decimal(reader,15+shift),Decimal(reader,16+shift),
                 Decimal(reader,17+shift),Decimal(reader,18+shift),Decimal(reader,19+shift)],
                Text(reader,20+shift),Text(reader,21+shift)));
        }
        return rows;
    }

    public async Task<IReadOnlyList<OpdPriceReceiptSource>> QueryReceiptsAsync(OpdPriceVisitKey key,
        bool showDc,CancellationToken token)
    {
        await using OracleConnection connection=CreateConnection(); await connection.OpenAsync(token);
        await using OracleCommand command=CreateCommand(connection,OpdPriceQuerySql.Receipts); AddKey(command,key);
        Add(command,"ShowDc",OracleDbType.Int32,showDc?1:0);
        await using OracleDataReader reader=await command.ExecuteReaderAsync(token);
        var rows=new List<OpdPriceReceiptSource>();
        while(await reader.ReadAsync(token))
        {
            var receiptKey=new OpdPriceReceiptKey(key.VisitDate,key.VisitTime,key.Room,key.RegistrationNo,
                Decimal(reader,12),key.MedicalRecordNo);
            rows.Add(new(receiptKey,Text(reader,0),Text(reader,1),Text(reader,2),Decimal(reader,3),
                Decimal(reader,4),Decimal(reader,5),Decimal(reader,6),Decimal(reader,7),Decimal(reader,8),
                Text(reader,9),Text(reader,10),Text(reader,11)));
        }
        return rows;
    }

    public async Task<OpdReceiptHeader?> QueryReceiptHeaderAsync(OpdPriceReceiptKey key,CancellationToken token)
    {
        await using OracleConnection connection=CreateConnection(); await connection.OpenAsync(token);
        await using OracleCommand command=CreateCommand(connection,OpdPriceQuerySql.ReceiptHeader);
        AddReceiptKey(command,key); await using OracleDataReader reader=await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token)?new(Text(reader,0),Text(reader,1),Text(reader,2),Text(reader,3),
            Text(reader,4),Text(reader,5),Text(reader,6),Text(reader,7),Text(reader,8),Text(reader,9),
            Text(reader,10),Text(reader,11)):null;
    }

    public async Task<IReadOnlyList<OpdReceiptChargeAggregate>> QueryReceiptChargesAsync(
        OpdPriceReceiptKey key,CancellationToken token)
    {
        await using OracleConnection connection=CreateConnection(); await connection.OpenAsync(token);
        await using OracleCommand command=CreateCommand(connection,OpdPriceQuerySql.ReceiptCharges);
        AddReceiptKey(command,key); await using OracleDataReader reader=await command.ExecuteReaderAsync(token);
        var rows=new List<OpdReceiptChargeAggregate>();
        while(await reader.ReadAsync(token)) rows.Add(new(Text(reader,0),Decimal(reader,1),Decimal(reader,2),
            Decimal(reader,3),Decimal(reader,4),Decimal(reader,5))); return rows;
    }

    public async Task<IReadOnlyDictionary<string,string>> QueryChargeNamesAsync(IEnumerable<string> values,CancellationToken token)
    {
        string[] codes=values.Select(x=>x.Trim()).Where(x=>x.Length>0).Distinct(StringComparer.Ordinal).ToArray();
        if(codes.Length==0)return new Dictionary<string,string>();
        string placeholders=string.Join(",",codes.Select((_,i)=>$":Code{i}"));
        string sql=$"SELECT chDctItem,chDctItemName FROM GenDctItemTbl WHERE chDctItem IN ({placeholders})";
        await using OracleConnection connection=CreateConnection(); await connection.OpenAsync(token);
        await using OracleCommand command=CreateCommand(connection,sql);
        for(int i=0;i<codes.Length;i++)Add(command,$"Code{i}",OracleDbType.Varchar2,codes[i]);
        await using OracleDataReader reader=await command.ExecuteReaderAsync(token);
        var result=new Dictionary<string,string>(StringComparer.Ordinal);
        while(await reader.ReadAsync(token))result[Text(reader,0)]=Text(reader,1); return result;
    }

    private OracleConnection CreateConnection()=>new(connections.GetConnectionString());
    internal static OracleCommand CreateCommand(OracleConnection connection,string sql)=>new(sql,connection){BindByName=true};
    private static void AddVisitFilters(OracleCommand c,string mrNo,string date,string? section)
    { Add(c,"ApplyDate",OracleDbType.Int32,string.IsNullOrWhiteSpace(date)?0:1);
      Add(c,"VisitDate",OracleDbType.Char,string.IsNullOrWhiteSpace(date)?DBNull.Value:date);Add(c,"MrNo",OracleDbType.Varchar2,mrNo);
      Add(c,"ApplySection",OracleDbType.Int32,string.IsNullOrWhiteSpace(section)?0:1);
      Add(c,"LegacySection",OracleDbType.Varchar2,string.IsNullOrWhiteSpace(section)?DBNull.Value:section); }
    private static void AddKey(OracleCommand c,OpdPriceVisitKey key)
    { Add(c,"VisitDate",OracleDbType.Char,key.VisitDate);Add(c,"VisitTime",OracleDbType.Char,key.VisitTime);
      Add(c,"Room",OracleDbType.Char,key.Room);Add(c,"RegistrationNo",OracleDbType.Decimal,key.RegistrationNo); }
    private static void AddReceiptKey(OracleCommand c,OpdPriceReceiptKey key)
    { AddKey(c,new(key.VisitDate,key.VisitTime,key.Room,key.RegistrationNo,key.MedicalRecordNo));
      Add(c,"ReceiptSequence",OracleDbType.Decimal,key.ReceiptSequence); }
    private static void Add(OracleCommand c,string name,OracleDbType type,object value)=>
        c.Parameters.Add(name,type,value,ParameterDirection.Input);
    private static OpdPriceVisitSource Visit(OracleDataReader r)=>new(new(Text(r,0),Text(r,1),Text(r,2),
        Decimal(r,3),Text(r,4)),Text(r,5),Text(r,6),!r.IsDBNull(9)&&Convert.ToInt32(r.GetValue(9))==1,
        Text(r,7),Text(r,8));
    private static string Text(OracleDataReader r,int i)=>r.IsDBNull(i)?string.Empty:Convert.ToString(r.GetValue(i))?.Trim()??string.Empty;
    private static decimal Decimal(OracleDataReader r,int i)=>r.IsDBNull(i)?0m:Convert.ToDecimal(r.GetValue(i));
}
