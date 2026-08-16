using DLI.Connect.Models;

namespace DLI.Connect.Services.Interfaces;

public interface IPartyService
{
    Task<Party?> GetCurrentPartyAsync();
    Task<Party?> CreatePartyAsync();
    Task LeavePartyAsync();
    Task DisbandPartyAsync();
    Task<string?> InviteFriendAsync(string friendUid);
    Task AcceptInviteAsync(string inviteId);
    Task DeclineInviteAsync(string inviteId);
    Task CancelInviteAsync(string inviteId);
    Task KickMemberAsync(string memberUid);
    Task TransferLeadershipAsync(string memberUid);
    Task<List<PartyInvite>> GetPendingInvitesAsync();
    Task<bool> IsInPartyAsync();
    Task<int> GetPartyMemberCountAsync();
}