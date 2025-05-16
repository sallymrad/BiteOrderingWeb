using System.ComponentModel.DataAnnotations;

namespace BiteOrderWeb.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public bool IsActive { get; set; } = true;
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public ICollection<ProductSize> ProductSizes { get; set; }
        public ICollection<OrderProduct> OrderProducts { get; set; }
        public ICollection<RestaurantProduct> RestaurantProducts { get; set; }
        public ICollection<ShoppingCartProduct> ShoppingCartProducts { get; set; }
        public bool IsDeactivated { get; set; } = false;

    }
}
