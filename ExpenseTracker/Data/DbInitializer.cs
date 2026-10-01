using ExpenseTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Data
{
    public static class DbInitializer
    {
        public static async Task SeedCategoriesAsync(ApplicationDbContext context)
        {
            if (await context.Categories.AnyAsync())
                return;

            context.Categories.AddRange(
                new Category { Title = "Research", Icon = "🔍", Type = "Expense" },
                new Category { Title = "Open Balance", Icon = "🔑", Type = "Income" },
                new Category { Title = "Salary", Icon = "👔", Type = "Income" },
                new Category { Title = "Garment", Icon = "👕", Type = "Expense" },
                new Category { Title = "Grocery", Icon = "🫑", Type = "Expense" },
                new Category { Title = "Education", Icon = "📚", Type = "Expense" },
                new Category { Title = "Entertainment", Icon = "🍿", Type = "Expense" });

            await context.SaveChangesAsync();
        }
    }
}
