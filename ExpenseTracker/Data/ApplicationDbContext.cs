using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExpenseTracker.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                var localTimestampConverter = new ValueConverter<DateTime, DateTime>(
                    value => DateTime.SpecifyKind(value, DateTimeKind.Unspecified),
                    value => DateTime.SpecifyKind(value, DateTimeKind.Unspecified));

                modelBuilder.Entity<Transaction>()
                    .Property(transaction => transaction.Date)
                    .HasConversion(localTimestampConverter)
                    .HasColumnType("timestamp without time zone");
            }
        }
    }
}