using System.ComponentModel.DataAnnotations.Schema;

namespace BiteOrderWeb.Models
{
    public class ShoppingCartProduct
    {
        public int ShoppingCartId { get; set; }
        public ShoppingCart ShoppingCart { get; set; }
        public string? Notes { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
        public string Size { get; set; } = string.Empty;

        public int Quantity { get; set; }
        [NotMapped]
        public decimal? SizePrice
        {
            get
            {
                return Product?.ProductSizes?.FirstOrDefault(s => s.Size == Size)?.Price;
            }
        }

    }
}
