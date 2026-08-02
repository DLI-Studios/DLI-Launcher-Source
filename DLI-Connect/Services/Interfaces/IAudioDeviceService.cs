using System.Collections.Generic;
using DLI.Connect.Models;

namespace DLI.Connect.Services.Interfaces;

public interface IAudioDeviceService
{
    event System.EventHandler<string>? DeviceChanged;

    /// <summary>
    /// Raised with processed PCM samples (16-bit mono, 48 kHz) while capture is active.
    /// </summary>
    event System.Action<byte[]>? PcmCaptured;

    List<AudioDevice> AvailableInputDevices { get; }
    List<AudioDevice> AvailableOutputDevices { get; }

    bool IsInitialized { get; }
    string? SelectedInputDeviceId { get; }
    string? SelectedOutputDeviceId { get; }

    Task InitializeAsync();
    void Dispose();

    Task RefreshDevicesAsync();
    Task SetInputDeviceAsync(string deviceId);
    Task SetOutputDeviceAsync(string deviceId);

    Task StartCaptureAsync(VoiceSettings settings);
    Task StopCaptureAsync();

    Task StartPlaybackAsync(VoiceSettings settings);
    Task StopPlaybackAsync();

    /// <summary>
    /// Writes a PCM buffer (16-bit mono, 48 kHz) to the playback device. Thread-safe.
    /// </summary>
    void WritePlaybackData(byte[] pcm);

    void ClearPlaybackBuffer();

    Task ToggleMuteAsync(bool muted);
    Task SetInputVolumeAsync(float volume);
    Task SetOutputVolumeAsync(float volume);
}

public class AudioDevice
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public DeviceType Type { get; set; }
}

public enum DeviceType
{
    Input,
    Output
}
