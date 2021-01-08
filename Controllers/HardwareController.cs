using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using SoapyBackend.Data;

namespace SoapyBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HardwareController : ControllerBase
    {
        private readonly CoreDbContext Db;

        public HardwareController(CoreDbContext db)
        {
            Db = db;
        }


        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Guid>>> GetAllId()
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return NotFound("User Not Found");

            var devices = Db.Users.Select(x => x.DeviceId).Distinct();

            var ids = await Db.Devices.Where(x => !devices.Contains(x.Id))
                .Select(x => x.DeviceId)
                .ToArrayAsync();
            return Ok(ids);
        }

        [HttpPost("user/{uuid}")]
        [Authorize]
        public async Task<ActionResult> ToggleFromUser(Guid uuid, bool state)
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return NotFound("User Not Found");

            var count = await Db.Users
                .Include(x => x.Device)
                .CountAsync(x => x.Aud == user && uuid == x.Device.DeviceId);

            if (count == 0) return NotFound("No such device");

            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithClientId("Backend_Server")
                .WithTcpServer("node02.myqtthub.com")
                .WithCredentials("server", "password")
                .WithCleanSession()
                .Build();

            mqttClient.UseConnectedHandler(async e =>
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("esp32/update")
                    .WithPayload($"{uuid}:{state}")
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();

                await mqttClient.PublishAsync(message, CancellationToken.None);
                await mqttClient.DisconnectAsync();
            });
            await mqttClient.ConnectAsync(options, CancellationToken.None);

            return NoContent();
        }

        // For Middleware to post
        [HttpPost("{uuid}")]
        public async Task<ActionResult> ToggleDevice(Guid uuid, bool state)
        {
            var prev = await Db.Devices.FirstOrDefaultAsync(x => x.DeviceId == uuid);
            if (prev == null) return NotFound();

            if (prev.PowerState != state)
            {
                // Fire Event Trigger
            }

            prev.PowerState = state;
            await Db.SaveChangesAsync();
            return NoContent();
        }

        // To Register from ESP 32
        [HttpPut("{uuid}")]
        public async Task<ActionResult> RegisterDevice(Guid uuid, string password)
        {
            var c = await Db.Devices.CountAsync(x => x.DeviceId == uuid);
            if (c == 0)
            {
                await Db.Devices.AddAsync(new DeviceData
                {
                    DeviceId = uuid,
                    Password = password,
                    PowerState = false,
                });
            }
            else
            {
                var d = await Db.Devices.FirstOrDefaultAsync(x => x.DeviceId == uuid);
                d.Password = password;
            }

            await Db.SaveChangesAsync();
            return NoContent();
        }
    }
}