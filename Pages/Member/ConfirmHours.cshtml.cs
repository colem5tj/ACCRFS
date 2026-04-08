using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ACC_Demo.Data;

namespace ACC_Demo.Pages.Member;

public class ConfirmHoursModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ConfirmHoursModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ConfirmHoursInputModel Input { get; set; } = new();

    public int TransactionId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string RequestTitle { get; set; } = string.Empty;

    public IActionResult OnGet(int transactionId)
    {
        var transaction = _context.Transactions
            .Include(t => t.Provider)
            .Include(t => t.Receiver)
            .Include(t => t.Request)
            .FirstOrDefault(t => t.TransactionId == transactionId);

        if (transaction == null)
        {
            return RedirectToPage("/Member/MyRequests");
        }

        TransactionId = transaction.TransactionId;
        ProviderName = transaction.Provider?.FullName ?? "Unknown";
        ReceiverName = transaction.Receiver?.FullName ?? "Unknown";
        RequestTitle = transaction.Request?.Title ?? "Unknown Request";
        Input.ActualHoursWorked = transaction.HoursTransferred;

        return Page();
    }

    public IActionResult OnPost(int transactionId)
    {
        if (!ModelState.IsValid)
        {
            return OnGet(transactionId);
        }

        var transaction = _context.Transactions
            .Include(t => t.Provider)
            .Include(t => t.Receiver)
            .FirstOrDefault(t => t.TransactionId == transactionId);

        if (transaction == null)
        {
            return RedirectToPage("/Member/MyRequests");
        }

        transaction.HoursTransferred = Input.ActualHoursWorked;
        transaction.Status = "Confirmed";
        transaction.ConfirmedAt = DateTime.UtcNow;

        if (transaction.Provider != null)
        {
            transaction.Provider.CurrentBalance += Input.ActualHoursWorked;
        }

        if (transaction.Receiver != null)
        {
            transaction.Receiver.CurrentBalance -= Input.ActualHoursWorked;
        }

        _context.TimeLedgers.Add(new Models.TimeLedger
        {
            UserId = transaction.ProviderId,
            TransactionId = transaction.TransactionId,
            HoursChange = Input.ActualHoursWorked,
            BalanceAfter = transaction.Provider?.CurrentBalance ?? 0
        });

        _context.TimeLedgers.Add(new Models.TimeLedger
        {
            UserId = transaction.ReceiverId,
            TransactionId = transaction.TransactionId,
            HoursChange = Input.ActualHoursWorked * -1,
            BalanceAfter = transaction.Receiver?.CurrentBalance ?? 0
        });

        _context.SaveChanges();

        return RedirectToPage("/Member/Ledger");
    }

    public class ConfirmHoursInputModel
    {
        [Display(Name = "Actual Hours Worked")]
        [Range(0.25, 24, ErrorMessage = "Hours must be between 0.25 and 24.")]
        public decimal ActualHoursWorked { get; set; }

        [Display(Name = "Service Notes")]
        public string? ServiceNotes { get; set; }

        [Display(Name = "I confirm these hours are accurate")]
        public bool IsConfirmed { get; set; }
    }
}
