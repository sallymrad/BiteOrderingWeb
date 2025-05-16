using BiteOrderWeb.Models;

namespace BiteOrderWeb.ViewModels
{
    public class ProductDetailsViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }

        public List<ProductSize> Sizes { get; set; }
        public string? RestaurantName { get; set; }

        public string? Notes { get; set; }

        public string SelectedSize { get; set; }
        public int Quantity { get; set; }
       
    }
}
