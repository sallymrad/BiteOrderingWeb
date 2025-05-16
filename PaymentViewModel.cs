namespace BiteOrderWeb.ViewModels
{
    public class PaymentViewModel
    {
        public int OrderId { get; set; }  
        public decimal Amount { get; set; }  

        public string? PaymentIntentId { get; set; }  
        public string? Status { get; set; } 

        public DateTime Date { get; set; } = DateTime.UtcNow;
        public decimal TotalPrice => Amount;

        public string PublishableKey { get; set; } = "your_stripe_publishable_key_here";
    }
}
