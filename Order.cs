using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiteOrderWeb.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DeliveryPrice { get; set; }
        public DateTime Date { get; set; }
        public DateTime? AcceptedAt { get; set; }      
        public DateTime? DeliveredAt { get; set; }
        public string? AcceptedDriverName { get; set; } 

        public enum OrderStatus
        {
            Pending,
            InProgress,
            Accepted,
            Rejected,
            Done
        }
        public OrderStatus Status { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public Users User { get; set; }

        public ICollection<OrderProduct> OrderProducts { get; set; }

        [ForeignKey("Driver")]
        public string? DriverId { get; set; }
        public Users? Driver { get; set; }
        public int? AddressId { get; set; }  
        public Address Address { get; set; }
    }
}
