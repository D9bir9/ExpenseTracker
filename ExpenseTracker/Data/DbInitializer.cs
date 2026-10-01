using ExpenseTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Data
{
    public static class DbInitializer
    {
        public static async Task ClaimLegacyDataAndSeedCategoriesAsync(
            ApplicationDbContext context,
            string userId)
        {
            await context.Categories
                .Where(category => category.OwnerId == null)
                .ExecuteUpdateAsync(update => update.SetProperty(category => category.OwnerId, userId));
            await context.Transactions
                .Where(transaction => transaction.OwnerId == null)
                .ExecuteUpdateAsync(update => update.SetProperty(transaction => transaction.OwnerId, userId));

            if (await context.Categories.AnyAsync(category => category.OwnerId == userId))
                return;

            context.Categories.AddRange(
                new Category { OwnerId = userId, Title = "Research", Icon = "🔍", Type = "Expense" },
                new Category { OwnerId = userId, Title = "Open Balance", Icon = "🔑", Type = "Income" },
                new Category { OwnerId = userId, Title = "Salary", Icon = "👔", Type = "Income" },
                new Category { OwnerId = userId, Title = "Garment", Icon = "👕", Type = "Expense" },
                new Category { OwnerId = userId, Title = "Grocery", Icon = "🫑", Type = "Expense" },
                new Category { OwnerId = userId, Title = "Education", Icon = "📚", Type = "Expense" },
                new Category { OwnerId = userId, Title = "Entertainment", Icon = "🍿", Type = "Expense" });

            await context.SaveChangesAsync();
        }
    }
}
