using System.ComponentModel.DataAnnotations;

namespace BiteOrderWeb.Models
{
    public class Address
    {
        [Key]
        public int Id { get; set; }
        public string Street { get; set; }
       
        public string Building { get; set; }
        public string Floor { get; set; }
        public int? AreaId { get; set; }
        public Area Area { get; set; }
        public ICollection<Users> Users { get; set; }
        public ICollection<Restaurant> Restaurants { get; set; }
    }
}
