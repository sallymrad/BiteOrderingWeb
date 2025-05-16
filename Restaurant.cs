using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiteOrderWeb.Models
{
    public class Restaurant
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Phone { get; set; }
        public string? ImageUrl { get; set; } = null;

        [ForeignKey("AdminUser")]
        public string AdminId { get; set; }
        public Users AdminUser { get; set; }

        public bool IsDeactivated { get; set; }

        public int? AddressId { get; set; }
        public Address Address { get; set; }

        public ICollection<RestaurantProduct> RestaurantProducts { get; set; }
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }


    }
}
