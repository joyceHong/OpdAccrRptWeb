using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Controllers;

[Route("Report/C141")]
[Authorize]
public sealed class C141LineBotController(IC141LineBotService service) : Controller
{
    private const int MaximumCasesPerRequest = 500;

    [HttpPost("LineBot")]
    public async Task<IActionResult> Send(
        [FromBody] C141LineBotRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(User.Identity.Name))
            return Unauthorized();
        if (request.Cases is null || request.Cases.Count == 0)
            return Invalid("請至少選取一筆 C141 個案。");
        if (request.Cases.Count > MaximumCasesPerRequest)
            return Invalid($"每次最多可處理 {MaximumCasesPerRequest} 筆 C141 個案。");
        if (request.Cases.Any(key => !IsValid(key)))
            return Invalid("C141 個案四鍵格式無效。");

        try
        {
            C141LineBotBatchResult result = await service.SendAsync(
                request.Kind,
                request.Cases,
                User.Identity.Name,
                cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(499);
        }
    }

    private static bool IsValid(C141AdmissionKey key) =>
        key is not null
        && IsAsciiWithin(key.AdmissionDate, 7)
        && IsAsciiWithin(key.AdmissionTime, 1)
        && IsAsciiWithin(key.AdmissionRoom, 6)
        && key.AdmissionNumber >= 0m
        && decimal.Truncate(key.AdmissionNumber) == key.AdmissionNumber
        && key.AdmissionNumber <= 9999m;

    private static bool IsAsciiWithin(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value)
        && value == value.Trim()
        && value.Length <= maximumLength
        && value.All(char.IsAscii);

    private BadRequestObjectResult Invalid(string title) =>
        BadRequest(new ProblemDetails { Title = title, Status = StatusCodes.Status400BadRequest });
}
