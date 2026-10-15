using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

internal static class C141OracleParameters
{
    internal const int AdmissionDateLength = 7;
    internal const int AdmissionTimeLength = 1;
    internal const int AdmissionRoomLength = 6;
    internal const int MedicalRecordNumberLength = 10;

    internal static OracleParameter AddFixedChar(
        OracleCommand command,
        string name,
        string? value,
        int size)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        object parameterValue = DBNull.Value;
        if (value is not null)
        {
            if (value.Any(character => character > 0x7f))
                throw new ArgumentException("C141 固定長度識別值必須是 ASCII。", nameof(value));

            if (value.Length > size)
                throw new ArgumentException("C141 查詢值超過 Oracle 欄位長度。", nameof(value));

            parameterValue = value.PadRight(size, ' ');
        }

        var parameter = new OracleParameter(name, OracleDbType.Char, size, parameterValue, ParameterDirection.Input);
        command.Parameters.Add(parameter);
        return parameter;
    }

    internal static OracleParameter AddAdmissionNumber(OracleCommand command, string name, decimal value)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var parameter = new OracleParameter(name, OracleDbType.Decimal, value, ParameterDirection.Input);
        command.Parameters.Add(parameter);
        return parameter;
    }

    internal static OracleParameter AddInt32(OracleCommand command, string name, int value)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var parameter = new OracleParameter(name, OracleDbType.Int32, value, ParameterDirection.Input);
        command.Parameters.Add(parameter);
        return parameter;
    }

    internal static OracleParameter AddVarchar2(OracleCommand command, string name, string? value, int size)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        if (value is not null && value.Length > size)
            throw new ArgumentException("C141 查詢值超過 Oracle 欄位長度。", nameof(value));

        var parameter = new OracleParameter(
            name,
            OracleDbType.Varchar2,
            size,
            (object?)value ?? DBNull.Value,
            ParameterDirection.Input);
        command.Parameters.Add(parameter);
        return parameter;
    }
}
