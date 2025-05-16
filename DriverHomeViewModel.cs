namespace BiteOrderWeb.ViewModels
{
    public class DriverHomeViewModel
    {
        public string FullName { get; set; }
        public int AcceptedOrdersCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public DateTime? LastOrderTime { get; set; }
    }
}
