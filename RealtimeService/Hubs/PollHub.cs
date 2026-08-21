using Microsoft.AspNetCore.SignalR;

namespace RealtimeService.Hubs;

public class PollHub : Hub
{
    // Client gọi hàm này để tham gia group theo mã poll 
    public async Task JoinPoll(string code)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
    }

    public async Task LeavePoll(string code)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
    }
}