using OpdAccrRptWeb.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class C141OracleParametersTests
{
    [Fact]
    public void ShortAdmissionRoom_IsPaddedAndBoundAsFixedChar()
    {
        using var command = new OracleCommand();

        OracleParameter parameter = C141OracleParameters.AddFixedChar(
            command, "AdmissionRoom", "A12", C141OracleParameters.AdmissionRoomLength);

        Assert.Equal(OracleDbType.Char, parameter.OracleDbType);
        Assert.Equal(6, parameter.Size);
        Assert.Equal("A12   ", parameter.Value);
        Assert.Same(parameter, command.Parameters["AdmissionRoom"]);
    }

    [Fact]
    public void ShortMedicalRecordNumber_IsPaddedToDescLength()
    {
        using var command = new OracleCommand();

        OracleParameter parameter = C141OracleParameters.AddFixedChar(
            command, "MedicalRecordNumber", "C36979", C141OracleParameters.MedicalRecordNumberLength);

        Assert.Equal(OracleDbType.Char, parameter.OracleDbType);
        Assert.Equal(10, parameter.Size);
        Assert.Equal("C36979    ", parameter.Value);
    }

    [Fact]
    public void AdmissionKeys_UseDescLengthsAndNumericParameter()
    {
        using var command = new OracleCommand();

        OracleParameter date = C141OracleParameters.AddFixedChar(
            command, "AdmissionDate", "1151001", C141OracleParameters.AdmissionDateLength);
        OracleParameter time = C141OracleParameters.AddFixedChar(
            command, "AdmissionTime", "1", C141OracleParameters.AdmissionTimeLength);
        OracleParameter room = C141OracleParameters.AddFixedChar(
            command, "AdmissionRoom", "A12", C141OracleParameters.AdmissionRoomLength);
        OracleParameter number = C141OracleParameters.AddAdmissionNumber(command, "AdmissionNumber", 7);

        Assert.Equal((OracleDbType.Char, 7, "1151001"), (date.OracleDbType, date.Size, date.Value));
        Assert.Equal((OracleDbType.Char, 1, "1"), (time.OracleDbType, time.Size, time.Value));
        Assert.Equal(OracleDbType.Decimal, number.OracleDbType);
        Assert.Equal(7m, number.Value);
    }

    [Fact]
    public void OverlongMedicalRecordNumber_IsRejectedWithoutTruncation()
    {
        using var command = new OracleCommand();

        Assert.Throws<ArgumentException>(() => C141OracleParameters.AddFixedChar(
            command, "MedicalRecordNumber", "12345678901", C141OracleParameters.MedicalRecordNumberLength));
        Assert.Empty(command.Parameters);
    }

    [Fact]
    public void MissingOptionalCharValue_BindsOracleNull()
    {
        using var command = new OracleCommand();

        OracleParameter parameter = C141OracleParameters.AddFixedChar(
            command, "MedicalRecordNumber", null, C141OracleParameters.MedicalRecordNumberLength);

        Assert.Equal(OracleDbType.Char, parameter.OracleDbType);
        Assert.Equal(10, parameter.Size);
        Assert.Equal(DBNull.Value, parameter.Value);
    }
}
