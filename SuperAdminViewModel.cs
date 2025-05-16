namespace BiteOrderWeb.ViewModels
{
    public class SuperAdminViewModel
    {
        public string Id { get; set; } = string.Empty; 
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = "/default-profile.png";
        public int RestaurantsCount { get; set; }
        public int AdminsCount { get; set; }
        public int ClientsCount { get; set; }
        public int DriversCount { get; set; }
    }
}
