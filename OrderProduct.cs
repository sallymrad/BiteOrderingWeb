namespace BiteOrderWeb.Models
{
    public class OrderProduct
    {
        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
        public string? Size { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }

    }
}
 