using CarPoint.Models.ViewModels;
using CarPoint.Services.CarValuation;
using Microsoft.AspNetCore.Mvc;

namespace CarPoint.Controllers
{
    public class CarValuationController : Controller
    {
        private readonly ICarValuationService _carValuationService;

        public CarValuationController(ICarValuationService carValuationService)
        {
            _carValuationService = carValuationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new CarValuationVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CarValuationVm vm, CancellationToken ct)
        {
            vm.ErrorMessage = null;
            vm.IsCalculated = false;
            vm.EstimatedPrice = null;

            if (!ModelState.IsValid)
                return View(vm);

            var request = new CarValuationRequest
            {
                Brand = vm.Brand,
                Model = vm.Model,
                Year = vm.Year!.Value,
                Mileage = vm.Mileage!.Value,
                FuelType = vm.FuelType,
                Transmission = vm.Transmission,
                Engine = vm.Engine,
                HorsePower = vm.HorsePower!.Value,
                BodyType = vm.BodyType,
                Condition = vm.Condition,
                HadAccident = vm.HadAccident,
                OwnersCount = vm.OwnersCount
            };

            try
            {
                var result = await _carValuationService.GetValuationAsync(request, ct);

                vm.EstimatedPrice = result.EstimatedPrice;
                vm.IsCalculated = true;

                vm.Currency = result.Currency;

            }
            catch (InvalidOperationException ex)
            {
                vm.ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                vm.ErrorMessage = "Възникна грешка при изчислението. Моля, опитайте отново.";
            }

            return View(vm);
        }
    }
}
