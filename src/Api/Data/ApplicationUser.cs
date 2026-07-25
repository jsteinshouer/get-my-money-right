using Microsoft.AspNetCore.Identity;

namespace Api.Data;

public class ApplicationUser : IdentityUser
{
    public required string DisplayName { get; set; }
}
