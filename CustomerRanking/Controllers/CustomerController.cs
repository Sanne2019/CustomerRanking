using CustomerRanking.Application.Iservice;
using Microsoft.AspNetCore.Mvc;

namespace CustomerRanking.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly IRankingService _rankingService;
        public CustomerController(IRankingService rankingService)
        {
            _rankingService = rankingService;
        }
        [HttpPost("{customerId}/score/{score}")]
        public IActionResult UpdateScore(long customerId, decimal score)
        {
            var result = _rankingService.UpdateScore(customerId, score);
            return Ok(result);
        }
    }
}
