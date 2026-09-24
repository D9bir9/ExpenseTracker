using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ExpenseTracker.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage ="Please select a category")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        [Range(1, int.MaxValue, ErrorMessage ="Amount Should be greater than 0")]
        public int Amount { get; set; }
        [Column(TypeName = "nvarchar(75)")]
        public string? Note { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string CultureCode { get; set; } = System.Globalization.CultureInfo.CurrentCulture.Name;// e.g. "en-US", "en-GB" — set at creation time

        [NotMapped]
        public string? CategoryTitleWithIcon {
            get
            {
                return Category == null ? "" : Category.Icon + " " + Category.Title;
            } 
        }

        [NotMapped]
        public string? FormattedAmount {
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