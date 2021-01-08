using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace SoapyBackend.StatusManager
{
    [ApiController]
    [Route("")]
    public class MainController : ControllerBase
    {
        private readonly IServerStatusService _status;

        public MainController(IServerStatusService status)
        {
            _status = status;
        }

        [HttpGet]
        public async Task<ActionResult<ServerStatusResponse>> Get()
        {
            var status = await _status.Get();
            var r = new ServerStatusResponse(status);
            Log.Information("Status: {@Status}", r);
            return Ok(r);
        }
    }
}