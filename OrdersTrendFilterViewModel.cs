namespace BiteOrderWeb.ViewModels
{
    public class OrdersTrendFilterViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<OrdersPerDayViewModel> OrdersPerDay { get; set; }
    }
}
