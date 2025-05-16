using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BiteOrderWeb.ViewModels
{
    public class EditRestaurantViewModel
    {
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public string PhoneNumber { get; set; }
        [Required]
        public TimeSpan OpeningTime { get; set; }

        [Required]
        public TimeSpan ClosingTime { get; set; }


        [Required]
        public int AreaId { get; set; }

        public List<SelectListItem> Areas { get; set; } = new();
        public string Street { get; set; }
        public string? Building { get; set; }
        public string? Floor { get; set; }
        public IFormFile? ImageFile { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}