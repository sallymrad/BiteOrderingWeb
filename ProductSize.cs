using System.ComponentModel.DataAnnotations;

namespace BiteOrderWeb.Models
{
    public class ProductSize
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
        public decimal Price { get; set; }
        public string Size { get; set; } 
        public int Quantity { get; set; }
    }
}
