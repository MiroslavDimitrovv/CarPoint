using CarPoint.Models;

namespace CarPoint.Models.ViewModels
{
    public class AdminUserRentRowVm
    {
        public int Id { get; set; }

        public int CarId { get; set; }
        public string CarName { get; set; } = "";

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int Days { get; set; }
        public decimal TotalPrice { get; set; }

        public Rental.RentalStatus Status { get; set; }
        public Rental.PaymentMethod PayMethod { get; set; }

        public bool IsPaid { get; set; }

        public OfficeLocation PickupOffice { get; set; }
        public OfficeLocation ReturnOffice { get; set; }
    }
}
