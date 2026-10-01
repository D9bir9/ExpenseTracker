using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace ExpenseTracker.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public int Amount { get; set; }

        [Column(TypeName = "nvarchar(75)")]
        public string? Note { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;
        public string CultureCode { get; set; } = System.Globalization.CultureInfo.CurrentCulture.Name;

        [NotMapped]
        public string? CategoryTitleWithIcon
        {
            get
            {
                if (Category == null)
                    return "";

                return string.IsNullOrWhiteSpace(Category.Icon)
                    ? Category.Title
                    : $"{Category.Icon} {Category.Title}";
            }
        }

        [NotMapped]
        public string? FormattedAmount
        {
            get
            {
                CultureInfo culture;
                try
                {
                    culture = string.IsNullOrWhiteSpace(CultureCode)
                        ? CultureInfo.CurrentCulture
                        : CultureInfo.CreateSpecificCulture(CultureCode);
                }
                catch (CultureNotFoundException)
                {
                    culture = CultureInfo.CurrentCulture;
                }

                return (Category == null || Category.Type == "Expense" ? "-" : "+") + Amount.ToString("c0", culture);
            }
        }
    }
}