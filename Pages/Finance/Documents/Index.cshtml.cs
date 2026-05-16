using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance.Documents;

public class IndexModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    public IList<FinanceDocument> Documents { get; set; } = new List<FinanceDocument>();

    [BindProperty]
    public FinanceDocument FinanceDocument { get; set; } = new() { DocumentDate = DateTime.Today };

    public SelectList FolioOptions { get; set; } = null!;
    public SelectList ReservationOptions { get; set; } = null!;
    public SelectList GuestOptions { get; set; } = null!;
    public SelectList SalesAccountOptions { get; set; } = null!;
    public SelectList BanquetEventOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(FinanceDocument.DocumentNumber))
        {
            FinanceDocument.DocumentNumber = await _financeService.GenerateDocumentNumberAsync(FinanceDocument.DocumentType);
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        FinanceDocument.Status = FinanceDocumentStatus.Draft;
        FinanceDocument.CreatedAt = DateTime.Now;
        FinanceDocument.CreatedBy = User.Identity?.Name ?? "System";
        _context.FinanceDocuments.Add(FinanceDocument);
        await _context.SaveChangesAsync();
        return RedirectToPage("Details", new { id = FinanceDocument.Id });
    }

    private async Task LoadAsync()
    {
        Documents = await _context.FinanceDocuments
            .AsNoTracking()
            .Include(document => document.Guest)
            .Include(document => document.SalesAccount)
            .Include(document => document.BanquetEvent)
            .OrderByDescending(document => document.DocumentDate)
            .ThenByDescending(document => document.Id)
            .Take(100)
            .ToListAsync();

        var folios = await _context.Folios.AsNoTracking().OrderByDescending(folio => folio.Id).Select(folio => new { folio.Id, Name = folio.FolioNumber }).ToListAsync();
        var reservations = await _context.Reservations.AsNoTracking().OrderByDescending(reservation => reservation.Id).Select(reservation => new { reservation.Id, Name = reservation.ConfirmationNumber }).ToListAsync();
        var guests = await _context.Guests.AsNoTracking().OrderBy(guest => guest.LastName).Select(guest => new { guest.Id, Name = guest.LastName + ", " + guest.FirstName }).ToListAsync();
        var accounts = await _context.SalesAccounts.AsNoTracking().OrderBy(account => account.AccountName).Select(account => new { account.Id, account.AccountName }).ToListAsync();
        var events = await _context.BanquetEvents.AsNoTracking().OrderByDescending(evt => evt.EventDate).Select(evt => new { evt.Id, evt.EventName }).ToListAsync();

        FolioOptions = new SelectList(folios, "Id", "Name", FinanceDocument.FolioId);
        ReservationOptions = new SelectList(reservations, "Id", "Name", FinanceDocument.ReservationId);
        GuestOptions = new SelectList(guests, "Id", "Name", FinanceDocument.GuestId);
        SalesAccountOptions = new SelectList(accounts, "Id", "AccountName", FinanceDocument.SalesAccountId);
        BanquetEventOptions = new SelectList(events, "Id", "EventName", FinanceDocument.BanquetEventId);
    }
}
