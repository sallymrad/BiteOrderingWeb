using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace BiteOrderWeb.Models
{
    public class Users : IdentityUser
    {
        public string? OTP { get; set; }
        public DateTime? OTPGeneratedAt { get; set; }
        [NotMapped]
        public bool HasOrders { get; set; }

        public string FullName {get;set;}

        public int? AreaId { get; set; }
        public Area Area { get; set; }
        public int? AddressId { get; set; }
        public Address Address { get; set; }
        public ShoppingCart ShoppingCart { get; set; }
        public ICollection<Order> Orders { get; set; }
        public string? DeliveryArea { get; set; }
        public string? ProfilePicture { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsDeactivated { get; set; } = false;

    }
}
