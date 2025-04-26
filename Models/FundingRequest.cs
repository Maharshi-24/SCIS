
namespace SCIS.Models
{
    public class FundingRequest
    {
        public int FundingRequestId { get; set; }
        public int ClubId { get; set; }
        public int EventId { get; set; }
        public decimal AmountRequested { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; } // Pending, Approved, Rejected
    }
}
