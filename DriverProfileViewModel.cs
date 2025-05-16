using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BiteOrderWeb.ViewModels
{
    public class DriverProfileViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string DeliveryArea { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
        public string? NewPassword { get; set; }
        public int? AreaId { get; set; }
        public List<SelectListItem>? Areas { get; set; }
        public int AcceptedOrdersCount { get; set; }
        public int RejectedOrdersCount { get; set; }
        public string CurrentPassword { get; set; }

    }


}
