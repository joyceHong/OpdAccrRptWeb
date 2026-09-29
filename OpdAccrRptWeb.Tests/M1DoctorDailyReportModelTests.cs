using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Tests;

public sealed class M1DoctorDailyReportModelTests
{
    [Fact]
    public void Create_OutpatientRow_TrimsFieldsAndDerivesPeopleTotal()
    {
        M1DoctorDailyReportRow row = M1DoctorDailyReportRow.Create(
            " 0450 ", " 急診醫學科 ", " D1 ", " 王醫師 ", "R", 3, 7, 5, 3, 2, 4);

        Assert.Equal("0450", row.SectionNo);
        Assert.Equal("D1", row.DoctorNo);
        Assert.Equal(10, row.TotalCount);
        Assert.Equal(4, row.AppointmentCount);
    }

    [Fact]
    public void Create_RejectsInvalidVisitTypeAndEmergencyAppointment()
    {
        Assert.Throws<ArgumentException>(() => M1DoctorDailyReportRow.Create(
            "0450", "急診", "D1", "王醫師", "X", 1, 2, 1, 1, 1, 0));
        Assert.Throws<ArgumentException>(() => M1DoctorDailyReportRow.Create(
            "0450", "急診", "D1", "王醫師", "E", 1, 2, 1, 1, 1, 1));
    }
}
