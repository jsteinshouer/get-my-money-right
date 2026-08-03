using Api.Features.Accounts;
using Api.Features.Budgets;
using Api.Features.Categories;
using Api.Features.Transactions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class BudgetDbContext : IdentityDbContext<ApplicationUser>
{
    public BudgetDbContext(DbContextOptions<BudgetDbContext> options) : base(options)
    {
    }

    public DbSet<Accounts.Account> Accounts => Set<Accounts.Account>();
    public DbSet<Categories.Category> Categories => Set<Categories.Category>();
    public DbSet<Transactions.Transaction> Transactions => Set<Transactions.Transaction>();
    public DbSet<Budgets.Budget> Budgets => Set<Budgets.Budget>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Accounts.Account>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Categories.Category>()
            .HasIndex(c => c.Name)
            .IsUnique();

        builder.Entity<Categories.Category>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Transactions.Transaction>()
            .Property(t => t.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Transactions.Transaction>()
            .HasOne<Accounts.Account>()
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Transactions.Transaction>()
            .HasOne<Categories.Category>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Transactions.Transaction>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Budgets.Budget>()
            .Property(b => b.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Budgets.Budget>()
            .HasIndex(b => new { b.CategoryId, b.Year, b.Month })
            .IsUnique();

        builder.Entity<Budgets.Budget>()
            .HasOne<Categories.Category>()
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
