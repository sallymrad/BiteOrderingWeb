using System.ComponentModel.DataAnnotations;

namespace BiteOrderWeb.ViewModels
{
    public class RestaurantViewModel
    {
        [Required(ErrorMessage = "Restaurant name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "Phone number must be between 8 and 15 digits.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Admin email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string AdminEmail { get; set; }

        [Required(ErrorMessage = "Admin password is required.")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 20 characters.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number.")]
        public string AdminPassword { get; set; }
    }
}

