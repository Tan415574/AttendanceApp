using Microsoft.AspNetCore.SignalR;

namespace AttendanceApp.Hubs;

// Groups are keyed by session id, so a lecturer's board only receives events for
// the session currently open on their screen, not every check-in system-wide.
public class AttendanceHub : Hub
{
    public async Task JoinBoard(int sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(sessionId));
    }

    public static string GroupName(int sessionId) => $"session-{sessionId}";
}

// Shape of the payload pushed to the board when a student checks in.
// Kept as a simple record so both the hub and the check-in page share one contract.
public record CheckInEvent(string StudentName, string StudentNumber, int AvatarIndex, int TotalCheckedIn);
