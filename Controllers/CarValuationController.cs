using CarPoint.Models.ViewModels;
using CarPoint.Services.AdminEvents;
using CarPoint.Services.CarValuation;
using Microsoft.AspNetCore.Mvc;

namespace CarPoint.Controllers
{
    public class CarValuationController : Controller
    {
        private readonly ICarValuationService _carValuationService;
        private readonly IAdminEventLogger _events;

        public CarValuationController(
            ICarValuationService carValuationService,
            IAdminEventLogger events)
        {
            _carValuationService = carValuationService;
            _events = events;
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
            {
                return View(vm);
            }

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

                await _events.LogAsync(
                    type: "CarValuationCalculated",
                    title: "Изчислена е ориентировъчна оценка",
                    details: $"{vm.Brand} {vm.Model}, {vm.Year}, {vm.Mileage} км, резултат: {vm.EstimatedPrice} {vm.Currency}");
            }
            catch (InvalidOperationException ex)
            {
                vm.ErrorMessage = ex.Message;

                await _events.LogAsync(
                    type: "CarValuationFailed",
                    title: "Неуспешна заявка за оценка",
                    details: $"{vm.Brand} {vm.Model}, {vm.Year}, причина: {ex.Message}");
            }
            catch (Exception)
            {
                vm.ErrorMessage = "Възникна грешка при изчислението. Моля, опитайте отново.";

                await _events.LogAsync(
                    type: "CarValuationFailed",
                    title: "Грешка при заявка за оценка",
                    details: $"{vm.Brand} {vm.Model}, {vm.Year}");
            }

            return View(vm);
        }
    }
}
