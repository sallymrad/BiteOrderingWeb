using System.Globalization;

namespace BiteOrderWeb.ViewModels
{
    public class SuperAdminDashboardStatsViewModel
    {
        public List<DailyOrdersStatViewModel> OrdersPerDay { get; set; } = new();
        public List<TopRestaurantViewModel> TopRestaurants { get; set; } = new();
        public List<TopDriverViewModel> TopDrivers { get; set; } = new();

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class DailyOrdersStatViewModel
    {
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }
        public string FormattedDate => Date.ToString("MM/dd", CultureInfo.InvariantCulture);
    }

    public class TopRestaurantViewModel
    {
        public string RestaurantName { get; set; }
        public int OrdersCount { get; set; }
    }

    public class TopDriverViewModel
    {
        public string DriverName { get; set; }
        public int DeliveredOrders { get; set; }
        public string? AreaName { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
