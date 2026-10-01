using ExpenseTracker.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Data
{
    public class PostgreSqlMigrationsDbContext : IdentityDbContext<ApplicationUser>
    {
        public PostgreSqlMigrationsDbContext(DbContextOptions<PostgreSqlMigrationsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(category => category.Title).HasColumnType("character varying(50)");
                entity.Property(category => category.Icon).HasColumnType("character varying(5)");
                entity.Property(category => category.Type).HasColumnType("character varying(10)");
                entity.Property(category => category.MonthlyBudgetLimit).HasColumnType("numeric(18,2)");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(transaction => transaction.Note).HasColumnType("character varying(75)");
                entity.Property(transaction => transaction.Date).HasColumnType("timestamp without time zone");
            });
        }
    }
}
