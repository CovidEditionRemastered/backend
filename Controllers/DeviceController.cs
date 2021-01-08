using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoapyBackend.Data;

namespace SoapyBackend.Controllers
{
    public class DevicePrincipalResponse
    {
        public DevicePrincipalResponse(Guid id, string name, string description, bool powerState)
        {
            Id = id;
            Name = name;
            Description = description;
            PowerState = powerState;
        }

        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        
        public bool PowerState { get; }
    }

    public class DeviceResponse
    {
        public DeviceResponse(DevicePrincipalResponse principal, IEnumerable<TriggerResponse> triggers,
            IEnumerable<ProgramResponse> programs)
        {
            Principal = principal;
            Triggers = triggers;
            Programs = programs;
        }

        public DevicePrincipalResponse Principal { get; }
        public IEnumerable<TriggerResponse> Triggers { get; }
        public IEnumerable<ProgramResponse> Programs { get; }
    }

    public class TriggerResponse
    {
        public TriggerResponse(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public Guid Id { get; }
        public string Name { get; }
    }

    public class ProgramResponse
    {
        public ProgramResponse(Guid id, string name, string code)
        {
            Id = id;
            Name = name;
            Code = code;
        }

        public Guid Id { get; }
        public string Name { get; }
        public string Code { get; }
    }

    public class UpdateDeviceRequest
    {
        [Required] public string Name { get; set; }
        [Required] public string Description { get; set; }
    }

    public class CreateDeviceRequest
    {
        [Required] public string Password { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string Description { get; set; }
    }

    public class ProgramRequest
    {
        [Required] public string Name { get; set; }
        [Required] public string Code { get; set; }
    }

    public class TriggerRequest
    {
        [Required] public string Name { get; set; }
        [Required] public string Code { get; set; }
    }

    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly CoreDbContext Db;

        public DeviceController(CoreDbContext db)
        {
            Db = db;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<DevicePrincipalResponse>>> GetAll()
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return Unauthorized("Unknown user");

            var devices = await Db.Users
                .Include(x => x.Device)
                .Where(x => x.Aud == user)
                .ToArrayAsync();
            if (devices == null) return NotFound("Cannot find users");

            return Ok(devices.Select(x => new DevicePrincipalResponse(
                x.Device.DeviceId,
                x.Name,
                x.Description,
                x.Device.PowerState
            )));
        }

        [HttpGet("{deviceId}")]
        public async Task<ActionResult<DeviceResponse>> Get(Guid deviceId)
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return Unauthorized("Unknown user");

            var d = await Db.Users
                .Where(x => x.Aud == user)
                .Include(x => x.Device)
                .ThenInclude(x => x.Programs)
                .Include(x => x.Device)
                .ThenInclude(x => x.Triggers)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId);
            if (d == null) return NotFound("Cannot find device");


            var device = new DevicePrincipalResponse(
                d.Device.DeviceId,
                d.Name,
                d.Description,
                d.Device.PowerState
            );

            var triggers = d.Device.Triggers.Select(x => new TriggerResponse(x.Id, x.Name));
            var programs = d.Device.Programs.Select(x => new ProgramResponse(x.Id, x.Name, x.Code));

            var resp = new DeviceResponse(device, triggers, programs);
            return Ok(resp);
        }

        [HttpPut("{deviceId}")]
        public async Task<ActionResult<DevicePrincipalResponse>> Update(Guid deviceId,
            [FromBody] UpdateDeviceRequest request)
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return NotFound("Cannot find user");

            var ud = await Db.Users.Where(x => x.Aud == user)
                .Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId);
            if (ud == null) return NotFound("Device Not Found");

            ud.Name = request.Name;
            ud.Description = request.Description;
            await Db.SaveChangesAsync();
            return Ok(new DevicePrincipalResponse(deviceId, request.Name, request.Description, ud.Device.PowerState));
        }

        [HttpPost("{deviceId}")]
        public async Task<ActionResult<DevicePrincipalResponse>> Create(Guid deviceId,
            [FromBody] CreateDeviceRequest request)
        {
            var device = await Db.Devices.FirstOrDefaultAsync(x => x.DeviceId == deviceId);
            if (device == null) return NotFound("Cannot find device");

            if (device.Password != request.Password) return Unauthorized("Incorrect Password");

            var user = HttpContext.User.Identity?.Name;
            if (user == null) return NotFound("Cannot find user");

            var ud = new UserData
            {
                Aud = user,
                Description = request.Description,
                DeviceId = device.Id,
                Name = request.Name,
            };

            var saved = await Db.Users.AddAsync(ud);
            await Db.SaveChangesAsync();
            var e = saved.Entity;
            return Ok(new DevicePrincipalResponse(deviceId, e.Name, e.Description, false   ));
        }

        [HttpDelete("{deviceId}")]
        public async Task<ActionResult> Delete(Guid deviceId)
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return NotFound("Cannot find user");

            var ud = await Db.Users.Where(x => x.Aud == user)
                .Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId);
            if (ud == null) return NotFound("Device Not Found");

            Db.Users.Remove(ud);
            await Db.SaveChangesAsync();
            return NoContent();
        }


        [HttpDelete("{deviceId}/Trigger/{triggerId}")]
        public async Task<ActionResult> DeleteTrigger(Guid deviceId, Guid triggerId)
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return NotFound("Cannot find user");

            var ud = await Db.Users.Where(x => x.Aud == user)
                .Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId);
            if (ud == null) return NotFound("Device Not Found");

            var td = await Db.Triggers.Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId && x.Id == triggerId);
            if (td == null) return NotFound("Trigger Not Found");

            Db.Triggers.Remove(td);
            await Db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{deviceId}/Program/{programId}")]
        public async Task<ActionResult> DeleteProgram(Guid deviceId, Guid programId)
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return NotFound("Cannot find user");

            var ud = await Db.Users.Where(x => x.Aud == user)
                .Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId);
            if (ud == null) return NotFound("Device Not Found");

            var td = await Db.Programs.Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId && x.Id == programId);
            if (td == null) return NotFound("Trigger Not Found");

            Db.Programs.Remove(td);
            await Db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{deviceId}/Program/{programId}")]
        public async Task<ActionResult<TriggerResponse>> UpdateProgram(Guid deviceId,
            Guid programId, [FromBody] ProgramRequest req)
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return NotFound("Cannot find user");

            var ud = await Db.Users.Where(x => x.Aud == user)
                .Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId);
            if (ud == null) return NotFound("Device Not Found");

            var td = await Db.Programs.Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId && x.Id == programId);
            if (td == null) return NotFound("Program Not Found");

            td.Code = req.Code;
            td.Name = req.Name;
            await Db.SaveChangesAsync();
            return Ok(new TriggerResponse(td.Id, td.Name));
        }

        [HttpPut("{deviceId}/Trigger/{triggerId}")]
        public async Task<ActionResult<TriggerResponse>> UpdateTrigger(Guid deviceId,
            Guid triggerId, [FromBody] TriggerRequest req)
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return NotFound("Cannot find user");

            var ud = await Db.Users.Where(x => x.Aud == user)
                .Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId);
            if (ud == null) return NotFound("Device Not Found");

            var td = await Db.Triggers.Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId && x.Id == triggerId);
            if (td == null) return NotFound("Trigger Not Found");

            td.Code = req.Code;
            td.Name = req.Name;
            await Db.SaveChangesAsync();
            return Ok(new TriggerResponse(td.Id, td.Name));
        }

        [HttpPost("{deviceId}/Trigger")]
        public async Task<ActionResult<TriggerResponse>> AddTrigger(Guid deviceId, [FromBody] TriggerRequest req)
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return NotFound("Cannot find user");

            var ud = await Db.Users.Where(x => x.Aud == user)
                .Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId);
            if (ud == null) return NotFound("Device Not Found");

            var pd = new TriggerData
            {
                Name = req.Name,
                Code = req.Code,
                DeviceId = ud.DeviceId,
            };

            var saved = await Db.Triggers
                .AddAsync(pd);
            await Db.SaveChangesAsync();
            var ok = saved.Entity!;
            return Ok(new TriggerResponse(ok.Id, ok.Name));
        }

        [HttpPost("{deviceId}/Program")]
        public async Task<ActionResult<ProgramResponse>> AddProgram(Guid deviceId, [FromBody] ProgramRequest req)
        {
            var user = HttpContext.User.Identity?.Name;
            if (user == null) return NotFound("Cannot find user");

            var ud = await Db.Users.Where(x => x.Aud == user)
                .Include(x => x.Device)
                .FirstOrDefaultAsync(x => x.Device.DeviceId == deviceId);
            if (ud == null) return NotFound("Device Not Found");

            var pd = new ProgramData
            {
                Name = req.Name,
                Code = req.Code,
                DeviceId = ud.DeviceId,
            };

            var saved = await Db.Programs
                .AddAsync(pd);
            await Db.SaveChangesAsync();
            var ok = saved.Entity!;
            return Ok(new ProgramResponse(ok.Id, ok.Name, ok.Code));
        }
    }
}