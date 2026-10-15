namespace OpdAccrRptWeb.Services;

public interface IC141EntryDateTicketService
{
    string Create(DateOnly entryDate, string userId);

    bool TryRead(string? ticket, string userId, out DateOnly entryDate);
}
