using Microsoft.AspNetCore.Identity;

namespace academic_weapon.Models;

// Application-specific user. Extends ASP.NET Core Identity's IdentityUser
// (which already provides UserName, Email, PasswordHash, etc.) with a
// friendly display name shown next to exam uploads and ratings.
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
