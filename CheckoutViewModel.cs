

using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BiteOrderWeb.ViewModels
{
    public class CheckoutViewModel
    {
        [Required]
        public int AreaId { get; set; }

        [Required]
        public string Street { get; set; }

        [Required]
        public string Building { get; set; }

        [Required]
        public string Floor { get; set; }

        public List<SelectListItem>? Areas { get; set; }
       

    }
}
