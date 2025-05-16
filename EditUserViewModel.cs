using System.ComponentModel.DataAnnotations;

namespace BiteOrderWeb.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }
        public string? DeliveryArea { get; set; }


        public string? Role { get; set; }
        public string? ProfilePictureUrl { get; set; } 

        public IFormFile? ProfilePicture { get; set; }
        public bool DeletePicture { get; set; }
        public string? PhoneNumber { get; set; }
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }
       


        public string? Address { get; set; }
    }
}
