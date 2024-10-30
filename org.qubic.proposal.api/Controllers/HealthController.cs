using Microsoft.AspNetCore.Mvc;

namespace org.qubic.proposal.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {

        /// <summary>
        /// should return ok if the health of this service is good
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        public ActionResult Get()
        {
            return Ok();
        }
    }
}
