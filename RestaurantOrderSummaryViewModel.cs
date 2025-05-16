namespace BiteOrderWeb.ViewModels
{
    public class RestaurantOrderSummaryViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public string Time { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Products { get; set; }
        public string Status { get; set; }
        public string Date { get; set; }
        public List<int> Quantities { get; set; }

    }
}
