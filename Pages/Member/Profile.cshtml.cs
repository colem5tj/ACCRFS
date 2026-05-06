using ACC_Demo.Data;
using ACC_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;

namespace ACC_Demo.Pages.Member
{
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
        public List<User> LinkedChildren { get; set; } = new();
        public double? LocationLat { get; set; }
        public double? LocationLng { get; set; }

        [BindProperty, Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [BindProperty, Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [BindProperty, Phone, StringLength(25)]
        public string? PhoneNumber { get; set; }

        [BindProperty, StringLength(50)]
        public string? Pronouns { get; set; }

        [BindProperty, StringLength(100)]
        public string? Occupation { get; set; }

        [BindProperty, StringLength(1000)]
        public string? Bio { get; set; }

        [BindProperty]
        [StringLength(5)]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "Please enter a valid 5-digit ZIP code.")]
        public string? ZipCode { get; set; }

        [BindProperty]
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
            if (userId == null) return RedirectToPage("/Account/Login");

            CurrentUser = _db.Users.Find(userId.Value)!;
            if (CurrentUser == null) return RedirectToPage("/Account/Login");

            FullName    = CurrentUser.FullName;
            Email       = CurrentUser.Email;
            PhoneNumber = CurrentUser.PhoneNumber;
            Pronouns    = CurrentUser.Pronouns;
            Occupation  = CurrentUser.Occupation;
            Bio         = CurrentUser.Bio;

            LinkedChildren = _db.Users
                .Where(u => u.ParentUserId == userId.Value)
                .ToList();

            LoadLocationForDisplay(userId.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/Account/Login");

            // Treat blank ZIP as null so the regex validator skips it
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

            user.FullName    = FullName;
            user.Email       = Email;
            user.PhoneNumber = PhoneNumber;
            user.Pronouns    = Pronouns;
            user.Occupation  = Occupation;
            user.Bio         = Bio;

            // ── Location ──────────────────────────────────────────────────────
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

            if (isNew)
                _db.UserLocationPreferences.Add(locPref);

            _db.SaveChanges();
            HttpContext.Session.SetString("UserName", user.FullName);

            if (locPref.ApproxLatitude.HasValue)
            {
                LocationLat = (double)locPref.ApproxLatitude;
                LocationLng = (double)locPref.ApproxLongitude!.Value;
            }

            SuccessMessage ??= "Profile updated successfully!";
            CurrentUser = user;
            LinkedChildren = _db.Users.Where(u => u.ParentUserId == userId.Value).ToList();
            return Page();
        }

        public IActionResult OnPostChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/Account/Login");

            CurrentUser = _db.Users.Find(userId.Value)!;
            if (CurrentUser == null) return RedirectToPage("/Account/Login");

            FullName    = CurrentUser.FullName;
            Email       = CurrentUser.Email;
            PhoneNumber = CurrentUser.PhoneNumber;
            Pronouns    = CurrentUser.Pronouns;
            Occupation  = CurrentUser.Occupation;
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

        // ── Helpers ───────────────────────────────────────────────────────────

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
                using var doc  = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.GetArrayLength() > 0)
                {
                    var first = root[0];
                    double lat = double.Parse(first.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
                    double lng = double.Parse(first.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);
                    return (lat, lng);
                }
            }
            catch { /* geocoding failure is non-fatal */ }
            return (null, null);
        }
    }
}
