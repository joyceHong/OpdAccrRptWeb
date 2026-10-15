using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C141LineBotSoapClient(HttpClient httpClient, IOptions<C141LineBotOptions> options)
    : IC141LineBotClient
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string ServiceNamespace = "http://tempuri.org/";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = null
    };

    public async Task<C141LineBotSendResult> SendAsync(
        string senderId,
        C141LineBotPayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senderId);
        ArgumentNullException.ThrowIfNull(payload);
        Uri endpoint = new(options.Value.Endpoint, UriKind.Absolute);
        string data = JsonSerializer.Serialize(payload, JsonOptions);
        XNamespace soap = SoapNamespace;
        XNamespace service = ServiceNamespace;
        var envelope = new XDocument(
            new XElement(soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", SoapNamespace),
                new XElement(soap + "Body",
                    new XElement(service + "JsonSingleSendByPId",
                        new XElement(service + "id", senderId),
                        new XElement(service + "data", data)))));

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml")
        };
        request.Headers.Add("SOAPAction", $"\"{ServiceNamespace}JsonSingleSendByPId\"");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        XDocument responseDocument = XDocument.Parse(responseBody);
        string? result = responseDocument
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "JsonSingleSendByPIdResult")?
            .Value;

        return new C141LineBotSendResult(
            result?.Contains("true", StringComparison.OrdinalIgnoreCase) == true);
    }

}
