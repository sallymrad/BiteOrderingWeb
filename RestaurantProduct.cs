namespace BiteOrderWeb.Models
{
    public class RestaurantProduct
    {
        public int RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
      public int? Quality { get; set; }
    }
}
