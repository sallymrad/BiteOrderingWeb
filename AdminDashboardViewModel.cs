using System.Collections.Generic;

namespace BiteOrderWeb.ViewModels
{
    public class AdminDashboardViewModel
    {
        public string RestaurantName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string? Building { get; set; }
        public string Floor { get; set; }
        public string PhoneNumber { get; set; }
        public string? ImageUrl { get; set; }
        public int ProductCount { get; set; }
        public int TotalOrders { get; set; }
        public string AreaName { get; set; }
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }


    }
}
