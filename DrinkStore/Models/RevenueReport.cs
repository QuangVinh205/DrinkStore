
namespace DrinkStore.Models
{
    public class RevenueReport
    {
        public int ReportId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public decimal TotalRevenue { get; set; }

        public int TotalOrders { get; set; }
    }
}
