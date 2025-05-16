
using System.ComponentModel.DataAnnotations;

namespace BiteOrderWeb.ViewModels
{

    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
      
        public string? NewCategory { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        [Required]
        public List<ProductSizeViewModel> Sizes { get; set; } = new List<ProductSizeViewModel>();
        public IFormFile? ImageFile { get; set; }
        
    }
}