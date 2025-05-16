using BiteOrderWeb.Models;

public class OrderRejection
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; }

    public string DriverId { get; set; }
    public Users Driver { get; set; }

    public DateTime RejectedAt { get; set; }
    public bool ShowAgain { get; set; } = false;

}
