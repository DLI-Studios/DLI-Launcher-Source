using System.Collections.ObjectModel;
using DLI.Connect.Models;

namespace DLI.Connect.Services.Interfaces;

public interface IVoiceChatService
{
    event System.EventHandler<VoiceParticipantState>? ParticipantStateChanged;
    event System.EventHandler? VoiceDisconnected;
    event System.EventHandler<string>? ErrorOccurred;

    VoiceSession CurrentSession { get; }
    VoiceSettings Settings { get; }
    VoiceConnectionQuality LocalConnectionQuality { get; }

    bool IsInVoiceChannel { get; }
    bool IsMuted { get; }
    bool IsDeafened { get; }

    ObservableCollection<VoiceParticipantState> Participants { get; }

    Task JoinVoiceChannelAsync(string partyId);
    Task LeaveVoiceChannelAsync();

    Task ToggleMuteAsync();
    Task ToggleDeafenAsync();

    Task SetInputDeviceAsync(string deviceId);
    Task SetOutputDeviceAsync(string deviceId);
    Task SetInputVolumeAsync(float volume);
    Task SetOutputVolumeAsync(float volume);
    Task ApplySettingsAsync(VoiceSettings settings);
    void SetPushToTalkState(bool active);

    void UpdateParticipantState(string uid, Action<VoiceParticipantState> updateAction);
}
