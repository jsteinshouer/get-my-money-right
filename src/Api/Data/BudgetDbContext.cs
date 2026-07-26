using Api.Features.Accounts;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class BudgetDbContext : IdentityDbContext<ApplicationUser>
{
    public BudgetDbContext(DbContextOptions<BudgetDbContext> options) : base(options)
    {
    }

    public DbSet<Accounts.Account> Accounts => Set<Accounts.Account>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Accounts.Account>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
