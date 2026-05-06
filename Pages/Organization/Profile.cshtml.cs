using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;

namespace ACC_Demo.Pages.Organization;

public class ProfileModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public ProfileModel(ApplicationDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public User CurrentUser { get; set; } = default!;
    public double? LocationLat { get; set; }
    public double? LocationLng { get; set; }

    [BindProperty, Required, StringLength(150)]
    [Display(Name = "Organization Name")]
    public string OrgName { get; set; } = string.Empty;

    [BindProperty, Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [BindProperty, Phone, StringLength(25)]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [BindProperty, StringLength(1000)]
    public string? Bio { get; set; }

    [BindProperty]
    [StringLength(5)]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Please enter a valid 5-digit ZIP code.")]
    [Display(Name = "ZIP Code")]
    public string? ZipCode { get; set; }

    [BindProperty]
    [Display(Name = "Show my general area on the community map")]
    public bool ShareLocation { get; set; }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty, DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [BindProperty, DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }

    public string? PasswordSuccessMessage { get; set; }
    public string? PasswordErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("UserRole");
        if (userId == null || role != "OrganizationRep")
            return RedirectToPage("/Account/Login");

        CurrentUser = _db.Users.Find(userId.Value)!;
        if (CurrentUser == null) return RedirectToPage("/Account/Login");

        OrgName     = CurrentUser.FullName;
        Email       = CurrentUser.Email;
        PhoneNumber = CurrentUser.PhoneNumber;
        Bio         = CurrentUser.Bio;

        LoadLocationForDisplay(userId.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("UserRole");
        if (userId == null || role != "OrganizationRep")
            return RedirectToPage("/Account/Login");

        if (string.IsNullOrWhiteSpace(ZipCode))
            ZipCode = null;

        if (!ModelState.IsValid)
        {
            CurrentUser = _db.Users.Find(userId.Value)!;
            LoadLocationForDisplay(userId.Value);
            return Page();
        }

        var user = _db.Users.Find(userId.Value);
        if (user == null) return RedirectToPage("/Account/Login");

        user.FullName    = OrgName;
        user.Email       = Email;
        user.PhoneNumber = PhoneNumber;
        user.Bio         = Bio;

        // Update the linked Organization name to stay in sync
        if (user.OrganizationId.HasValue)
        {
            var org = _db.Organizations.Find(user.OrganizationId.Value);
            if (org != null) org.Name = OrgName;
        }

        var locPref = _db.UserLocationPreferences.FirstOrDefault(l => l.UserId == userId.Value);
        bool isNew = locPref == null;
        locPref ??= new UserLocationPreference { UserId = userId.Value };

        string? oldZip = locPref.ZipCode;
        locPref.ZipCode          = ZipCode;
        locPref.IsLocationHidden = !ShareLocation;

        if (!string.IsNullOrWhiteSpace(ZipCode) && (ZipCode != oldZip || !locPref.ApproxLatitude.HasValue))
        {
            var (lat, lng) = await GeocodeZipAsync(ZipCode);
            if (lat.HasValue)
            {
                locPref.ApproxLatitude  = (decimal)lat.Value;
                locPref.ApproxLongitude = (decimal)lng!.Value;
            }
            else
            {
                ErrorMessage = "ZIP code not found — location preview not updated.";
            }
        }
        else if (string.IsNullOrWhiteSpace(ZipCode))
        {
            locPref.ApproxLatitude  = null;
            locPref.ApproxLongitude = null;
        }

        if (isNew) _db.UserLocationPreferences.Add(locPref);

        _db.SaveChanges();
        HttpContext.Session.SetString("UserName", user.FullName);

        if (locPref.ApproxLatitude.HasValue)
        {
            LocationLat = (double)locPref.ApproxLatitude;
            LocationLng = (double)locPref.ApproxLongitude!.Value;
        }

        SuccessMessage ??= "Profile updated successfully!";
        CurrentUser = user;
        return Page();
    }

    public IActionResult OnPostChangePassword()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("UserRole");
        if (userId == null || role != "OrganizationRep")
            return RedirectToPage("/Account/Login");

        CurrentUser = _db.Users.Find(userId.Value)!;
        OrgName     = CurrentUser.FullName;
        Email       = CurrentUser.Email;
        PhoneNumber = CurrentUser.PhoneNumber;
        Bio         = CurrentUser.Bio;
        LoadLocationForDisplay(userId.Value);

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            PasswordErrorMessage = "New password must be at least 6 characters.";
            return Page();
        }
        if (NewPassword != ConfirmPassword)
        {
            PasswordErrorMessage = "Passwords do not match. Please try again.";
            return Page();
        }

        CurrentUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
        _db.SaveChanges();

        PasswordSuccessMessage = "Password changed successfully.";
        NewPassword     = null;
        ConfirmPassword = null;
        return Page();
    }

    private void LoadLocationForDisplay(int userId)
    {
        var locPref = _db.UserLocationPreferences.FirstOrDefault(l => l.UserId == userId);
        if (locPref == null) return;
        ZipCode       = locPref.ZipCode;
        ShareLocation = !locPref.IsLocationHidden;
        if (locPref.ApproxLatitude.HasValue)
        {
            LocationLat = (double)locPref.ApproxLatitude;
            LocationLng = (double)locPref.ApproxLongitude!.Value;
        }
    }

    private async Task<(double? Lat, double? Lng)> GeocodeZipAsync(string zip)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Nominatim");
            var url    = $"https://nominatim.openstreetmap.org/search?postalcode={Uri.EscapeDataString(zip)}&countrycodes=US&format=json&limit=1";
            var json   = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.GetArrayLength() > 0)
            {
                var first = root[0];
                double lat = double.Parse(first.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
                double lng = double.Parse(first.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);
                return (lat, lng);
            }
        }
        catch { }
        return (null, null);
    }
}
