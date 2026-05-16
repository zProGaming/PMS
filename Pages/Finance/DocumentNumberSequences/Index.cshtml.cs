using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.Finance.DocumentNumberSequences;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<DocumentNumberSequence> Sequences { get; set; } = new List<DocumentNumberSequence>();

    [BindProperty]
    public DocumentNumberSequence Sequence { get; set; } = new() { IsActive = true, NextNumber = 1, PaddingLength = 6 };

    public async Task OnGetAsync(int? id)
    {
        await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(Sequence.Id == 0 ? null : Sequence.Id);
            return Page();
        }

        if (Sequence.Id == 0)
        {
            _context.DocumentNumberSequences.Add(Sequence);
        }
        else
        {
            var existing = await _context.DocumentNumberSequences.FindAsync(Sequence.Id);
            if (existing is null)
            {
                return NotFound();
            }

            existing.DocumentType = Sequence.DocumentType;
            existing.Prefix = Sequence.Prefix;
            existing.NextNumber = Sequence.NextNumber;
            existing.PaddingLength = Sequence.PaddingLength;
            existing.IsActive = Sequence.IsActive;
        }

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadAsync(int? id)
    {
        Sequences = await _context.DocumentNumberSequences
            .AsNoTracking()
            .OrderBy(sequence => sequence.DocumentType)
            .ToListAsync();

        if (id is not null)
        {
            var existing = await _context.DocumentNumberSequences.AsNoTracking().FirstOrDefaultAsync(sequence => sequence.Id == id);
            if (existing is not null)
            {
                Sequence = existing;
            }
        }
    }
}
