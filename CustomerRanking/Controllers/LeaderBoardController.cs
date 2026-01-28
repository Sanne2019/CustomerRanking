using CustomerRanking.Application.Iservice;
using Microsoft.AspNetCore.Mvc;

namespace CustomerRanking.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LeaderBoardController : ControllerBase
    {
        private readonly IRankingService _rankingService;
        public LeaderBoardController(IRankingService rankingService)
        {
            _rankingService = rankingService;
        }
        [HttpGet]
        public IActionResult GetCustomersByRank(int start, int end)
        {
            var result = _rankingService.GetCustomersByRank(start, end);
            return Ok(result);
        }
        [HttpGet("{customerId}")]

        public IActionResult GetCustomersById([FromRoute] long customerId, int high, int low)
        {
            var result = _rankingService.GetCustomersById(customerId, high, low);

            return Ok(result);
        }
    }
}
