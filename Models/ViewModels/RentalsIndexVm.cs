using CarPoint.Models;

namespace CarPoint.Models.ViewModels
{
    public class RentalsIndexVm
    {
        public CarFilterVm Filter { get; set; } = new();
        public List<Car> Cars { get; set; } = new();
        public OfficeLocation? Office { get; set; }

    }
}
