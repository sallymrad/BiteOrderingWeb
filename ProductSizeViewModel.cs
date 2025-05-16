using System.ComponentModel.DataAnnotations;

namespace BiteOrderWeb.ViewModels
{
    public class ProductSizeViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Size is required.")]
        public string Size { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsNew { get; set; }
    }
}
