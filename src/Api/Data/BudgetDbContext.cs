using Api.Features.Accounts;
using Api.Features.Budgets;
using Api.Features.Categories;
using Api.Features.Import;
using Api.Features.Tags;
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
    public DbSet<Tags.Tag> Tags => Set<Tags.Tag>();
    public DbSet<Tags.TransactionTag> TransactionTags => Set<Tags.TransactionTag>();
    public DbSet<Import.CsvImportMapping> CsvImportMappings => Set<Import.CsvImportMapping>();
    public DbSet<Import.ImportIgnoreRule> ImportIgnoreRules => Set<Import.ImportIgnoreRule>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Accounts.Account>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // NOCASE makes the unique index reject "Groceries" against "groceries", and makes the
        // name sort the way a person reads it rather than putting every capital ahead of every
        // lowercase letter.
        builder.Entity<Categories.Category>()
            .Property(c => c.Name)
            .UseCollation("NOCASE");

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

        builder.Entity<Tags.Tag>()
            .Property(t => t.Name)
            .UseCollation("NOCASE");

        builder.Entity<Tags.Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();

        builder.Entity<Tags.Tag>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Tags.TransactionTag>()
            .HasKey(tt => new { tt.TransactionId, tt.TagId });

        // Cascading both ways: an assignment has no meaning once either side is gone, so deleting
        // a tag or a transaction clears its assignments instead of being blocked by them.
        builder.Entity<Tags.TransactionTag>()
            .HasOne<Transactions.Transaction>()
            .WithMany()
            .HasForeignKey(tt => tt.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Tags.TransactionTag>()
            .HasOne<Tags.Tag>()
            .WithMany()
            .HasForeignKey(tt => tt.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // One mapping per account is the point: a bank's export format is described once and then
        // remembered, so the unique index is the rule rather than a guard against a race.
        builder.Entity<Import.CsvImportMapping>()
            .HasIndex(m => m.AccountId)
            .IsUnique();

        builder.Entity<Import.CsvImportMapping>()
            .HasOne<Accounts.Account>()
            .WithMany()
            .HasForeignKey(m => m.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Import.CsvImportMapping>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Matching is case-insensitive, so the index that refuses a duplicate rule has to be too —
        // otherwise "contains AUTOPAY" and "contains autopay" are two rules that strike the same
        // rows and each claim credit for it.
        builder.Entity<Import.ImportIgnoreRule>()
            .Property(r => r.MatchText)
            .UseCollation("NOCASE");

        // Same words, same kind of match, same scope is the same rule. SQLite treats NULLs as
        // distinct in a unique index, so two global rules with the same text would slip past this;
        // the filtered pair below closes that.
        builder.Entity<Import.ImportIgnoreRule>()
            .HasIndex(r => new { r.AccountId, r.MatchText, r.MatchType })
            .IsUnique();

        builder.Entity<Import.ImportIgnoreRule>()
            .HasIndex(r => new { r.MatchText, r.MatchType })
            .IsUnique()
            .HasFilter("\"AccountId\" IS NULL");

        builder.Entity<Import.ImportIgnoreRule>()
            .HasOne<Accounts.Account>()
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Import.ImportIgnoreRule>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
