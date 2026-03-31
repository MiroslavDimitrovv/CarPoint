using System.Threading;
using System.Threading.Tasks;

namespace CarPoint.Services.CarValuation
{
    public interface ICarValuationService
    {
        Task<CarValuationResult> GetValuationAsync(
            CarValuationRequest request,
            CancellationToken ct = default);
    }
}
