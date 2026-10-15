using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C141LineBotServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 10, 13, 8, 30, 0, TimeSpan.FromHours(8));

    [Fact]
    public async Task SendAsync_ResolvesCurrentContactAndBuildsPaymentPayloadFromDatabaseValues()
    {
        C141AdmissionKey key = Key(1);
        var repository = new FakeRepository(Case(key));
        var client = new FakeClient(new(true));
        var service = CreateService(repository, client);

        C141LineBotBatchResult result = await service.SendAsync(
            C141LineBotMessageKind.PaymentNotice,
            [key],
            "REPORTER");

        Assert.Equal("Sent", Assert.Single(result.Results).Status);
        Assert.Equal(8_500m, repository.RecordedCase!.NetAmount);
        Assert.Equal("REPORTER", repository.RecordedUserId);
        Assert.Equal(FixedNow, repository.RecordedAt);
        Assert.Equal(1, client.Calls);
        Assert.Equal("BID2", client.SenderId);
        Assert.Equal(2, client.Payload!.Type);
        Assert.Equal("20200708000006", client.Payload.Num);
        Assert.Equal("A123456789", client.Payload.ReceivePId);
        Assert.Equal(
            new[] { "林小明", "C36979", "A12", "115/10/13", "8500" },
            client.Payload.MessageParms);
    }

    [Theory]
    [InlineData(null, "A123456789", "沒有找到本案病歷號對應的優先聯絡人。")]
    [InlineData("C36979", "123456789", "本案優先聯絡人的 PID 格式不符，已略過。")]
    [InlineData("OTHER", "A123456789", "沒有找到本案病歷號對應的優先聯絡人。")]
    public async Task SendAsync_InvalidOrMismatchedRecipientIsSkippedWithoutSending(
        string? contactMedicalRecordNumber,
        string? patientId,
        string expectedMessage)
    {
        C141AdmissionKey key = Key(1);
        var repository = new FakeRepository(Case(key) with
        {
            ContactMedicalRecordNumber = contactMedicalRecordNumber,
            ContactPatientId = patientId
        });
        var client = new FakeClient(new(true));
        var service = CreateService(repository, client);

        C141LineBotBatchResult result = await service.SendAsync(
            C141LineBotMessageKind.PaymentNotice,
            [key],
            "REPORTER");

        C141LineBotDelivery delivery = Assert.Single(result.Results);
        Assert.Equal("Skipped", delivery.Status);
        Assert.Equal(expectedMessage, delivery.Message);
        Assert.Equal(0, client.Calls);
        Assert.Null(repository.RecordedCase);
    }

    [Theory]
    [InlineData("01", "", "A12", "讀卡通知僅適用於健保身分 30。")]
    [InlineData("30", "CARD", "A12", "健保卡已有讀卡資料，已略過讀卡通知。")]
    [InlineData("30", "", "04E0-01", "此床位不適用讀卡通知。")]
    [InlineData("30", "", "04B2-01", "此床位不適用讀卡通知。")]
    [InlineData("30", "", "04D3-01", "此床位不適用讀卡通知。")]
    public async Task SendAsync_CardReminderHonorsLegacyExclusions(
        string insuranceIdentity,
        string healthCardNumber,
        string bedNumber,
        string expectedMessage)
    {
        C141AdmissionKey key = Key(1);
        var repository = new FakeRepository(Case(key) with
        {
            InsuranceIdentity = insuranceIdentity,
            HealthCardNumber = healthCardNumber,
            BedNumber = bedNumber
        });
        var client = new FakeClient(new(true));
        var service = CreateService(repository, client);

        C141LineBotBatchResult result = await service.SendAsync(
            C141LineBotMessageKind.CardReminder,
            [key],
            "REPORTER");

        Assert.Equal("Skipped", Assert.Single(result.Results).Status);
        Assert.Equal(expectedMessage, result.Results[0].Message);
        Assert.Equal(0, client.Calls);
        Assert.Null(repository.RecordedCase);
    }

    [Fact]
    public async Task SendAsync_PaymentSoapSuccessThenWriteFailureIsReportedWithoutRetry()
    {
        C141AdmissionKey key = Key(1);
        var repository = new FakeRepository(Case(key)) { ThrowOnRecord = true };
        var client = new FakeClient(new(true));
        var service = CreateService(repository, client);

        C141LineBotBatchResult result = await service.SendAsync(
            C141LineBotMessageKind.PaymentNotice,
            [key],
            "REPORTER");

        Assert.Equal("SentButNotRecorded", Assert.Single(result.Results).Status);
        Assert.Equal(1, client.Calls);
        Assert.Equal(1, repository.RecordCalls);
    }

    [Fact]
    public async Task SendAsync_TransportFailureIsUnknownAndDoesNotRetry()
    {
        C141AdmissionKey key = Key(1);
        var repository = new FakeRepository(Case(key));
        var client = new FakeClient(exception: new HttpRequestException("Unavailable"));
        var service = CreateService(repository, client);

        C141LineBotBatchResult result = await service.SendAsync(
            C141LineBotMessageKind.PaymentNotice,
            [key],
            "REPORTER");

        Assert.Equal("Unknown", Assert.Single(result.Results).Status);
        Assert.Equal(1, client.Calls);
        Assert.Equal(0, repository.RecordCalls);
    }

    [Fact]
    public async Task SendAsync_CardReminderSuccessDoesNotWritePaymentRecords()
    {
        C141AdmissionKey key = Key(1);
        var repository = new FakeRepository(Case(key) with { HealthCardNumber = null });
        var client = new FakeClient(new(true));
        var service = CreateService(repository, client);

        C141LineBotBatchResult result = await service.SendAsync(
            C141LineBotMessageKind.CardReminder,
            [key],
            "REPORTER");

        Assert.Equal("Sent", Assert.Single(result.Results).Status);
        Assert.Equal("20200923000026", client.Payload!.Num);
        Assert.Equal(new[] { "林小明", "C36979", "A12" }, client.Payload.MessageParms);
        Assert.Equal(0, repository.RecordCalls);
    }

    [Fact]
    public void LineBotSql_SelectsNewestContactWithDeterministicPidTieBreak()
    {
        Assert.Contains("ORDER BY ChOp1Date DESC NULLS LAST,VchPid ASC", C141LineBotSql.Case, StringComparison.Ordinal);
        Assert.Contains("TRIM(B.chOp1MrNo)=Contact.NormalizedMrNo", C141LineBotSql.Case, StringComparison.Ordinal);
        Assert.Contains("B.chOp1ClmFlg='1'", C141LineBotSql.Case, StringComparison.Ordinal);
    }

    [Fact]
    public void PaymentSuccessSql_UpdatesNoticeLogAndZeroSequencePrepayment()
    {
        Assert.Contains("SET CHLISTDATE=:serverDate7,vchAPayDate=:serverDate7", C141LineBotSql.UpdateNoticeDates, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO IpdTranTbl", C141LineBotSql.InsertNoticeLog, StringComparison.Ordinal);
        Assert.Equal("LineBot發送繳款通知單", C141LineBotRepository.PaymentNoticePrintType);
        Assert.Contains("AND INTOP1OrdSeq=0", C141LineBotSql.DeleteZeroSequencePrepayment, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO OPDPSSFEEPREPAYTbl", C141LineBotSql.InsertPrepayment, StringComparison.Ordinal);
        Assert.Equal(
            "11501011A12   000101",
            C141LineBotRepository.BuildNoticeUniqueNumber(Key(1)));
        Assert.Equal("C369791", C141LineBotRepository.BuildTransactionMedicalRecordNumber(Case(Key(1))));
    }

    [Fact]
    public async Task SoapClient_UsesConfiguredAsmxOperationAndParsesResult()
    {
        var handler = new CapturingHandler("""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body><JsonSingleSendByPIdResponse xmlns="http://tempuri.org/">
                <JsonSingleSendByPIdResult>true</JsonSingleSendByPIdResult>
              </JsonSingleSendByPIdResponse></soap:Body>
            </soap:Envelope>
            """);
        using var httpClient = new HttpClient(handler);
        var client = new C141LineBotSoapClient(httpClient, Options.Create(new C141LineBotOptions
        {
            Endpoint = "https://linebot.example/ExtLinebotWebService.asmx"
        }));
        var payload = new C141LineBotPayload(
            null, 2, "20200708000006", "A123456789", 0, null, null,
            Array.Empty<string>(), ["Name", "MRNO", "Bed", "Date", "Amount"], false);

        C141LineBotSendResult result = await client.SendAsync("BID2", payload, default);

        Assert.True(result.Success);
        Assert.Equal("POST", handler.Method);
        Assert.Equal("https://linebot.example/ExtLinebotWebService.asmx", handler.RequestUri?.ToString());
        Assert.Equal("\"http://tempuri.org/JsonSingleSendByPId\"", handler.SoapAction);
        XDocument envelope = XDocument.Parse(handler.Body!);
        string serializedPayload = envelope.Descendants().Single(element => element.Name.LocalName == "data").Value;
        using JsonDocument data = JsonDocument.Parse(serializedPayload);
        Assert.Equal(2, data.RootElement.GetProperty("Type").GetInt32());
        Assert.Equal("20200708000006", data.RootElement.GetProperty("Num").GetString());
        Assert.Equal("A123456789", data.RootElement.GetProperty("ReceivePId").GetString());
        Assert.False(data.RootElement.GetProperty("LogErrorMessage").GetBoolean());
    }

    private static C141LineBotService CreateService(FakeRepository repository, FakeClient client) =>
        new(repository, client, Options.Create(new C141LineBotOptions { Enabled = true }));

    private static C141AdmissionKey Key(decimal admissionNumber) => new("1150101", "1", "A12", admissionNumber);

    private static C141LineBotCase Case(C141AdmissionKey key) => new(
        key,
        "C36979",
        "林小明",
        "A12",
        "30",
        "000",
        "1",
        "1",
        null,
        9_000m,
        500m,
        "C36979",
        "A123456789");

    private sealed class FakeRepository(C141LineBotCase item) : IC141LineBotRepository
    {
        public bool ThrowOnRecord { get; init; }
        public int RecordCalls { get; private set; }
        public C141LineBotCase? RecordedCase { get; private set; }
        public string? RecordedUserId { get; private set; }
        public DateTimeOffset RecordedAt { get; private set; }

        public DateTimeOffset GetOracleNow(CancellationToken cancellationToken) => FixedNow;

        public C141LineBotCase? LoadCase(C141AdmissionKey key, CancellationToken cancellationToken) =>
            item.Key == key ? item : null;

        public void RecordPaymentNoticeSuccess(
            C141LineBotCase lineBotCase,
            string userId,
            DateTimeOffset recordedAt,
            CancellationToken cancellationToken)
        {
            RecordCalls++;
            if (ThrowOnRecord)
                throw new InvalidOperationException("database error");
            RecordedCase = lineBotCase;
            RecordedUserId = userId;
            RecordedAt = recordedAt;
        }
    }

    private sealed class FakeClient(C141LineBotSendResult? result = null, Exception? exception = null)
        : IC141LineBotClient
    {
        public int Calls { get; private set; }
        public string? SenderId { get; private set; }
        public C141LineBotPayload? Payload { get; private set; }

        public Task<C141LineBotSendResult> SendAsync(
            string senderId,
            C141LineBotPayload payload,
            CancellationToken cancellationToken)
        {
            Calls++;
            SenderId = senderId;
            Payload = payload;
            return exception is null
                ? Task.FromResult(result!)
                : Task.FromException<C141LineBotSendResult>(exception);
        }
    }

    private sealed class CapturingHandler(string responseBody) : HttpMessageHandler
    {
        public string? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? SoapAction { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method.Method;
            RequestUri = request.RequestUri;
            SoapAction = request.Headers.GetValues("SOAPAction").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "text/xml")
            };
        }
    }
}
