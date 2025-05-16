using Microsoft.AspNetCore.Mvc.Rendering;

namespace BiteOrderWeb.ViewModels
{
    public class EditDriverViewModel
    {

        public string Id { get; set; } 
        public string FullName { get; set; } 
        public string Email { get; set; }
      
        public string PhoneNumber { get; set; }
      
        public int? AreaId { get; set; }
        public List<SelectListItem>? Areas { get; set; }
    }
}
