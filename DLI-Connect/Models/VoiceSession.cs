using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace DLI.Connect.Models;

public enum VoiceState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}

public enum ConnectionQuality
{
    Excellent,
    Good,
    Fair,
    Poor
}

public class VoiceSession : INotifyPropertyChanged
{
    private string _partyId = "";
    private VoiceState _state = VoiceState.Disconnected;
    private DateTime _connectedAt;

    public string PartyId
    {
        get => _partyId;
        set { _partyId = value; OnPropertyChanged(); }
    }

    public VoiceState State
    {
        get => _state;
        set { _state = value; OnPropertyChanged(); }
    }

    public DateTime ConnectedAt
    {
        get => _connectedAt;
        set { _connectedAt = value; OnPropertyChanged(); }
    }

    public bool IsConnected => _state == VoiceState.Connected;
    public bool IsConnecting => _state == VoiceState.Connecting;
    public bool IsDisconnected => _state == VoiceState.Disconnected;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class VoiceParticipantState : INotifyPropertyChanged
{
    private string _uid = "";
    private string _displayName = "";
    private string _username = "";
    private string _avatar = "";
    private bool _isLeader;
    private bool _isMuted;
    private bool _isDeafened;
    private bool _isSpeaking;
    private ConnectionQuality _quality = ConnectionQuality.Excellent;
    private int _ping;
    private double _packetLoss;

    public string Uid
    {
        get => _uid;
        set { _uid = value; OnPropertyChanged(); }
    }

    public string DisplayName
    {
        get => _displayName;
        set { _displayName = value; OnPropertyChanged(); }
    }

    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    public string Avatar
    {
        get => _avatar;
        set { _avatar = value; OnPropertyChanged(); }
    }

    public bool IsLeader
    {
        get => _isLeader;
        set { _isLeader = value; OnPropertyChanged(); }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set { _isMuted = value; OnPropertyChanged(); }
    }

    public bool IsDeafened
    {
        get => _isDeafened;
        set { _isDeafened = value; OnPropertyChanged(); }
    }

    public bool IsSpeaking
    {
        get => _isSpeaking;
        set { _isSpeaking = value; OnPropertyChanged(); }
    }

    public ConnectionQuality Quality
    {
        get => _quality;
        set
        {
            _quality = value;
            OnPropertyChanged(nameof(Quality));
            OnPropertyChanged(nameof(QualityText));
        }
    }

    public int Ping
    {
        get => _ping;
        set { _ping = value; OnPropertyChanged(); }
    }

    public double PacketLoss
    {
        get => _packetLoss;
        set { _packetLoss = value; OnPropertyChanged(); }
    }

    public string QualityText => _quality switch
    {
        ConnectionQuality.Excellent => "Mükemmel",
        ConnectionQuality.Good => "İyi",
        ConnectionQuality.Fair => "Orta",
        ConnectionQuality.Poor => "Kötü",
        _ => "Bilinmiyor"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class VoiceSettings : INotifyPropertyChanged
{
    private string _inputDeviceId = "";
    private string _outputDeviceId = "";
    private float _inputVolume = 1.0f;
    private float _outputVolume = 1.0f;
    private bool _noiseSuppression = true;
    private bool _echoCancellation = true;
    private bool _automaticGainControl = true;
    private VoiceActivationMode _activationMode = VoiceActivationMode.VoiceActivity;
    private float _voiceActivityThreshold = 0.4f;
    private string _pushToTalkKey = "";

    public string InputDeviceId
    {
        get => _inputDeviceId;
        set { _inputDeviceId = value; OnPropertyChanged(); }
    }

    public string OutputDeviceId
    {
        get => _outputDeviceId;
        set { _outputDeviceId = value; OnPropertyChanged(); }
    }

    public float InputVolume
    {
        get => _inputVolume;
        set { _inputVolume = Math.Clamp(value, 0f, 1f); OnPropertyChanged(); }
    }

    public float OutputVolume
    {
        get => _outputVolume;
        set { _outputVolume = Math.Clamp(value, 0f, 1f); OnPropertyChanged(); }
    }

    public bool NoiseSuppression
    {
        get => _noiseSuppression;
        set { _noiseSuppression = value; OnPropertyChanged(); }
    }

    public bool EchoCancellation
    {
        get => _echoCancellation;
        set { _echoCancellation = value; OnPropertyChanged(); }
    }

    public bool AutomaticGainControl
    {
        get => _automaticGainControl;
        set { _automaticGainControl = value; OnPropertyChanged(); }
    }

    public VoiceActivationMode ActivationMode
    {
        get => _activationMode;
        set { _activationMode = value; OnPropertyChanged(); }
    }

    public float VoiceActivityThreshold
    {
        get => _voiceActivityThreshold;
        set { _voiceActivityThreshold = Math.Clamp(value, 0f, 1f); OnPropertyChanged(); }
    }

    public string PushToTalkKey
    {
        get => _pushToTalkKey;
        set { _pushToTalkKey = value; OnPropertyChanged(); }
    }

    public bool IsPushToTalkActive { get; set; }

    public void ResetDefaults()
    {
        _inputDeviceId = "";
        _outputDeviceId = "";
        _inputVolume = 1.0f;
        _outputVolume = 1.0f;
        _noiseSuppression = true;
        _echoCancellation = true;
        _automaticGainControl = true;
        _activationMode = VoiceActivationMode.VoiceActivity;
        _voiceActivityThreshold = 0.4f;
        _pushToTalkKey = "";
        OnPropertyChanged(nameof(InputDeviceId));
        OnPropertyChanged(nameof(OutputDeviceId));
        OnPropertyChanged(nameof(InputVolume));
        OnPropertyChanged(nameof(OutputVolume));
        OnPropertyChanged(nameof(NoiseSuppression));
        OnPropertyChanged(nameof(EchoCancellation));
        OnPropertyChanged(nameof(AutomaticGainControl));
        OnPropertyChanged(nameof(ActivationMode));
        OnPropertyChanged(nameof(VoiceActivityThreshold));
        OnPropertyChanged(nameof(PushToTalkKey));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum VoiceActivationMode
{
    VoiceActivity,
    PushToTalk
}

public class VoiceSignalDoc
{
    public string SignalId { get; set; } = "";
    public string PartyId { get; set; } = "";
    public string FromUid { get; set; } = "";
    public string ToUid { get; set; } = "";
    public string? Offer { get; set; }
    public string? Answer { get; set; }
    public List<string> OffererCandidates { get; set; } = new();
    public List<string> AnswererCandidates { get; set; } = new();
    public long UpdatedAt { get; set; }
}

public class VoiceSessionMemberState
{
    public bool IsMuted { get; set; }
    public bool IsDeafened { get; set; }
    public bool IsSpeaking { get; set; }
    public bool IsConnected { get; set; }
}

public class VoiceConnectionQuality : INotifyPropertyChanged
{
    private int _ping;
    private double _packetLoss;
    private int _bytesSent;
    private int _bytesReceived;
    private ConnectionQuality _quality;

    public int Ping
    {
        get => _ping;
        set { _ping = value; UpdateQuality(); OnPropertyChanged(); }
    }

    public double PacketLoss
    {
        get => _packetLoss;
        set { _packetLoss = value; UpdateQuality(); OnPropertyChanged(); }
    }

    public int BytesSent
    {
        get => _bytesSent;
        set { _bytesSent = value; OnPropertyChanged(); }
    }

    public int BytesReceived
    {
        get => _bytesReceived;
        set { _bytesReceived = value; OnPropertyChanged(); }
    }

    public ConnectionQuality Quality
    {
        get => _quality;
        set { _quality = value; OnPropertyChanged(); }
    }

    private void UpdateQuality()
    {
        if (_ping < 30 && _packetLoss < 1)
            _quality = ConnectionQuality.Excellent;
        else if (_ping < 80 && _packetLoss < 3)
            _quality = ConnectionQuality.Good;
        else if (_ping < 150 && _packetLoss < 5)
            _quality = ConnectionQuality.Fair;
        else
            _quality = ConnectionQuality.Poor;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
