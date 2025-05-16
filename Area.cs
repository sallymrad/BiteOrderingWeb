namespace BiteOrderWeb.Models
{
    public class Area
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal DeliveryPrice { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<Address> Addresses { get; set; }
        public ICollection<Users> Users { get; set; }
    }
}
