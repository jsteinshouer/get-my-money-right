using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class BudgetDbContext : IdentityDbContext<ApplicationUser>
{
    public BudgetDbContext(DbContextOptions<BudgetDbContext> options) : base(options)
    {
    }
}
