using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RealtimeService.Hubs;

namespace RealtimeService.Controllers;

[ApiController]
[Route("api/notify")]
public class NotifyController : ControllerBase
{
    private readonly IHubContext<PollHub> _hub;

    public NotifyController(IHubContext<PollHub> hub)
    {
        _hub = hub;
    }

    // VoteService gọi endpoint này sau khi có vote mới 
    [HttpPost("vote")]
    public async Task<IActionResult> NotifyVote([FromBody]
NotifyVoteRequest request)
    {
        await _hub.Clients.Group(request.Code)
            .SendAsync("VoteReceived", request.Results);

        return Ok();
    }

    // PollService gọi khi đóng poll 
    [HttpPost("close")]
    public async Task<IActionResult> NotifyClose([FromBody]
NotifyCloseRequest request)
    {
        await _hub.Clients.Group(request.Code)
            .SendAsync("PollClosed");

        return Ok();
    }
}

public class NotifyVoteRequest
{
    public string Code { get; set; } = "";
    public object Results { get; set; } = new();
}

public class NotifyCloseRequest
{
    public string Code { get; set; } = "";
}