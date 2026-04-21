using Hmqs.Api.Dtos;
using Hmqs.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hmqs.Api.Controllers;

[ApiController]
[Route("api/ethical-calculator")]
public class EthicalCalculatorController : AuthorizedApiController
{
    private readonly EthicalCalculatorService _ethicalCalculatorService;

    public EthicalCalculatorController(EthicalCalculatorService ethicalCalculatorService)
    {
        _ethicalCalculatorService = ethicalCalculatorService;
    }

    [HttpGet]
    public async Task<ActionResult<EthicalCalculatorResultDto>> GetCalculator([FromQuery] decimal budget = 500m, CancellationToken cancellationToken = default)
    {
        var listenerId = GetCurrentListenerId();
        var calculator = await _ethicalCalculatorService.GetCalculatorAsync(listenerId, budget, cancellationToken);
        return Ok(calculator);
    }

    [HttpPost("dummy-purchase")]
    public async Task<ActionResult<EthicalDummyPurchaseResultDto>> ApplyDummyPurchase([FromBody] EthicalDummyPurchaseRequestDto model, CancellationToken cancellationToken = default)
    {
        if (model.PurchasedArtistId == Guid.Empty)
        {
            return BadRequest("PurchasedArtistId must be a valid artist id.");
        }

        var listenerId = GetCurrentListenerId();
        var result = await _ethicalCalculatorService.ApplyDummyPurchaseAsync(listenerId, model.PurchasedArtistId, model.MonthlyBudget, cancellationToken);
        return Ok(result);
    }
}