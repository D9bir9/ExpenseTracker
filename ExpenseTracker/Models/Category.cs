using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ExpenseTracker.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        [Required(ErrorMessage = "Please enter a category name.")]
        [StringLength(50, ErrorMessage = "Category name must be 50 characters or less.")]
        public string Title { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(5)")]
        public string? Icon { get; set; } = "";

        [Column(TypeName = "nvarchar(10)")]
        public string Type { get; set; } = "Expense";

        [NotMapped]
        public string? TitleWithIcon
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Title))
                    return "";

                return string.IsNullOrWhiteSpace(Icon)
                    ? Title
                    : $"{Icon} {Title}";
            }
        }
    }
}