using Microsoft.AspNetCore.Mvc;
using org.qubic.common.caching;
using org.qubic.proposal.api.Cache;
using org.qubic.proposal.api.Model;
using System.Globalization;

namespace org.qubic.proposal.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProposalController : ControllerBase
    {
        private RedisService _redisService;

        public ProposalController(
            RedisService redisService
            ) {
            _redisService = redisService;
        }


        /// <summary>
        /// returns a complete list of proposals per epoch
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        public ActionResult<List<ProposalModel>> Get(int epoch)
        {
            var epochPropoals = _redisService.GetEntries<ProposalModel>(ProposalCacheKey.CcfEpochIndex(epoch));
            return Ok(epochPropoals.ToList());
        }

    }
}
