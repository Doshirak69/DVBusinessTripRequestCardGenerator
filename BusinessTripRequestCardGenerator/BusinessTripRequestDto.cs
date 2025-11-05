
namespace BusinessTripRequestCardGenerator
{
    class BusinessTripRequestDto
    {
        public string View { get; set; } = string.Empty;
        public string CardKindName { get; set; } = string.Empty;
        public string TravelingEmployeeAccount { get; set; } = string.Empty;
        public string? ManagerAccount { get; set; }
        public string? PhoneNumber { get; set; }
        public decimal TripAllowanceAmount { get; set; }
        public int? NumberOfDaysOnBusinessTrip { get; set; }
        public string? ReasonForTravelItem { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string City { get; set; } = string.Empty;
        public string? Tickets { get; set; }
        public string? OrganizationName { get; set; }
        public string ProcessingEmployee { get; set; } = string.Empty;

        public string? AttachmentFilePath { get; set; }
        public List<string>? ApproverAccountNames { get; set; }
        public string? WorkflowState { get; set; }
    }
}
