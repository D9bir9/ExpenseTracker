using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Models.Account
{
    public class DeleteAccountViewModel
    {
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^DELETE$", ErrorMessage = "Type DELETE to confirm account removal.")]
        public string Confirmation { get; set; } = string.Empty;
    }
}
