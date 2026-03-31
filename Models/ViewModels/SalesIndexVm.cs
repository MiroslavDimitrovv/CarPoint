using CarPoint.Models;

namespace CarPoint.Models.ViewModels
{
    public class SalesIndexVm
    {
        public CarFilterVm Filter { get; set; } = new();
        public List<Car> Cars { get; set; } = new();
    }
}
