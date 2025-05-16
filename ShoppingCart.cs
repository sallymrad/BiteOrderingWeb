using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BiteOrderWeb.Models
{
    public class ShoppingCart
    {
        [Key]
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [ForeignKey("User")]
        public string UserId { get; set; }
        public Users User { get; set; }

        public ICollection<ShoppingCartProduct> ShoppingCartProducts { get; set; }
        [NotMapped]
        public decimal TotalPrice => ShoppingCartProducts?.Sum(p => (p.SizePrice ?? 0) * p.Quantity) ?? 0;
    
}
}
