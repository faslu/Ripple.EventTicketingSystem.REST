using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ripple.EventTicketingSystem.Application.DTOs.Reports;
using Ripple.EventTicketingSystem.Application.Interfaces;

namespace Ripple.EventTicketingSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]   
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("sales")]
        [ProducesResponseType(
            typeof(List<SalesSummaryResponse>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSalesSummary(
            CancellationToken cancellationToken)
        {
            var result = await _reportService.GetSalesSummaryAsync(
                cancellationToken);

            return Ok(result);
        }
    }
}
