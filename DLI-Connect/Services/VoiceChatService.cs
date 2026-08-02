using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;

namespace DLI.Connect.Services;

public class VoiceChatService : IVoiceChatService, IDisposable
{
    private readonly IPartyService _partyService;
    private readonly IFirebaseFirestore _firestore;
    private readonly IAudioDeviceService _audioDeviceService;
    private readonly ISessionManager _sessionManager;
    private readonly AudioEncoder _audioEncoder;

    private readonly object _lock = new();
    private readonly object _audioLock = new();
    private readonly Dictionary<string, VoicePeerConnection> _peerConnections = new();
    private readonly MemoryStream _captureBuffer = new();
    private CancellationTokenSource? _reconnectCts;
    private CancellationTokenSource? _pingCts;

    private VoiceSession _session = new();
    private VoiceSettings _settings = new();
    private VoiceConnectionQuality _connectionQuality = new();
    private bool _isMuted;
    private bool _isDeafened;
    private bool _isSpeaking;
    private string? _currentPartyId;
    private DateTime _lastSpeakingWrite = DateTime.MinValue;

    public VoiceSession CurrentSession => _session;
    public VoiceSettings Settings => _settings;
    public VoiceConnectionQuality LocalConnectionQuality => _connectionQuality;

    public bool IsInVoiceChannel => _currentPartyId != null && _session.IsConnected;
    public bool IsMuted => _isMuted;
    public bool IsDeafened => _isDeafened;

    public ObservableCollection<VoiceParticipantState> Participants { get; } = new();

    public event EventHandler<VoiceParticipantState>? ParticipantStateChanged;
    public event EventHandler? VoiceDisconnected;
    public event EventHandler<string>? ErrorOccurred;

    public VoiceChatService(IPartyService partyService, IFirebaseFirestore firestore, IAudioDeviceService audioDeviceService, ISessionManager sessionManager)
    {
        _partyService = partyService;
        _firestore = firestore;
        _audioDeviceService = audioDeviceService;
        _sessionManager = sessionManager;
        _audioEncoder = new AudioEncoder(false, true);

        _audioDeviceService.PcmCaptured += OnPcmCaptured;
        _session.PropertyChanged += OnSessionPropertyChanged;

        BindingOperations.EnableCollectionSynchronization(Participants, _lock);
    }

    private void OnSessionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VoiceSession.State))
        {
            if (_session.State == VoiceState.Connected)
            {
                StartPingPong();
            }
            else if (_session.State == VoiceState.Disconnected)
            {
                StopPingPong();
            }
        }
    }

    public async Task JoinVoiceChannelAsync(string partyId)
    {
        lock (_lock)
        {
            if (_currentPartyId == partyId && _session.IsConnected) return;

            _currentPartyId = partyId;
            _session.PartyId = partyId;
            _session.State = VoiceState.Connecting;

            Participants.Clear();
            foreach (var peer in _peerConnections.Values)
            {
                try { peer.Pc?.Dispose(); } catch { }
            }
            _peerConnections.Clear();
        }

        try
        {
            await CreateVoiceSessionDocumentAsync(partyId);

            var party = await _partyService.GetCurrentPartyAsync();
            if (party == null)
            {
                await LeaveVoiceChannelAsync();
                return;
            }

            var selfUid = _sessionManager.CurrentUser?.Uid;
            var selfParticipant = new VoiceParticipantState
            {
                Uid = selfUid ?? "",
                DisplayName = _sessionManager.Profile?.DisplayName ?? "Sen",
                Username = _sessionManager.Profile?.Username ?? "",
                Avatar = _sessionManager.Profile?.Avatar ?? "",
                IsLeader = party.LeaderUid == selfUid,
                IsMuted = _isMuted,
                IsDeafened = _isDeafened,
                IsSpeaking = false
            };

            lock (_lock)
            {
                Participants.Add(selfParticipant);
            }

            foreach (var member in party.Members)
            {
                if (member.Uid == selfUid) continue;

                var participantState = new VoiceParticipantState
                {
                    Uid = member.Uid,
                    DisplayName = member.DisplayName,
                    Username = member.Username,
                    Avatar = member.Avatar,
                    IsLeader = member.IsLeader,
                    IsMuted = member.IsVoiceMuted,
                    IsDeafened = member.IsVoiceDeafened,
                    IsSpeaking = member.IsSpeaking,
                    Quality = ConnectionQuality.Excellent,
                    Ping = 0,
                    PacketLoss = 0
                };

                lock (_lock)
                {
                    Participants.Add(participantState);
                }

                SetupPeerConnection(member.Uid, member.DisplayName, member.Username, member.Avatar, participantState);
            }

            _session.State = VoiceState.Connected;
            _session.ConnectedAt = DateTime.Now;

            await InitializeAudioAsync();

            await StartReconnectWatcherAsync();
            StartSessionListener(partyId);
        }
        catch (Exception ex)
        {
            _session.State = VoiceState.Disconnected;
            ErrorOccurred?.Invoke(this, $"Ses kanalına bağlanılamadı: {ex.Message}");
        }
    }

    public async Task LeaveVoiceChannelAsync()
    {
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _reconnectCts = null;

        _pingCts?.Cancel();
        _pingCts?.Dispose();
        _pingCts = null;

        var signalDocIds = new List<string>();
        lock (_lock)
        {
            foreach (var peer in _peerConnections.Values)
            {
                peer.SignalingCts?.Cancel();
                peer.SignalingCts?.Dispose();
                try { peer.Pc?.Dispose(); } catch { }
                signalDocIds.Add(peer.SignalDocId);
            }
            _peerConnections.Clear();
            Participants.Clear();
        }

        try
        {
            if (!string.IsNullOrEmpty(_currentPartyId))
            {
                await RemoveVoiceSessionParticipantAsync(_currentPartyId);
            }
        }
        catch { }

        try
        {
            if (!string.IsNullOrEmpty(_currentPartyId))
            {
                foreach (var signalDocId in signalDocIds)
                {
                    await _firestore.DeleteVoiceSignalAsync(_currentPartyId, signalDocId);
                }
            }
        }
        catch { }

        try
        {
            await _audioDeviceService.StopCaptureAsync();
        }
        catch { }

        try
        {
            await _audioDeviceService.StopPlaybackAsync();
        }
        catch { }

        lock (_lock)
        {
            _currentPartyId = null;
            _session.State = VoiceState.Disconnected;
            _session.PartyId = "";
        }

        VoiceDisconnected?.Invoke(this, EventArgs.Empty);
    }

    public async Task ToggleMuteAsync()
    {
        lock (_lock)
        {
            _isMuted = !_isMuted;
        }

        await _audioDeviceService.ToggleMuteAsync(_isMuted);
        UpdateSelfParticipantState();
        _ = WriteSelfParticipantStateAsync();
    }

    public Task ToggleDeafenAsync()
    {
        lock (_lock)
        {
            _isDeafened = !_isDeafened;
        }

        if (_isDeafened)
        {
            _audioDeviceService.ClearPlaybackBuffer();
        }

        UpdateSelfParticipantState();
        _ = WriteSelfParticipantStateAsync();
        return Task.CompletedTask;
    }

    public async Task SetInputDeviceAsync(string deviceId)
    {
        await _audioDeviceService.SetInputDeviceAsync(deviceId);
        _settings.InputDeviceId = deviceId;
    }

    public async Task SetOutputDeviceAsync(string deviceId)
    {
        await _audioDeviceService.SetOutputDeviceAsync(deviceId);
        _settings.OutputDeviceId = deviceId;
    }

    public async Task SetInputVolumeAsync(float volume)
    {
        await _audioDeviceService.SetInputVolumeAsync(volume);
        _settings.InputVolume = volume;
    }

    public async Task SetOutputVolumeAsync(float volume)
    {
        await _audioDeviceService.SetOutputVolumeAsync(volume);
        _settings.OutputVolume = volume;
    }

    public async Task ApplySettingsAsync(VoiceSettings settings)
    {
        lock (_lock)
        {
            _settings = settings;
        }

        if (!string.IsNullOrEmpty(settings.InputDeviceId))
        {
            await SetInputDeviceAsync(settings.InputDeviceId);
        }

        if (!string.IsNullOrEmpty(settings.OutputDeviceId))
        {
            await SetOutputDeviceAsync(settings.OutputDeviceId);
        }

        await SetInputVolumeAsync(settings.InputVolume);
        await SetOutputVolumeAsync(settings.OutputVolume);
    }

    public void SetPushToTalkState(bool active)
    {
        _settings.IsPushToTalkActive = active;
    }

    public void UpdateParticipantState(string uid, Action<VoiceParticipantState> updateAction)
    {
        VoiceParticipantState? participant;
        lock (_lock)
        {
            participant = Participants.FirstOrDefault(p => p.Uid == uid);
            if (participant == null)
            {
                participant = new VoiceParticipantState { Uid = uid };
                Participants.Add(participant);
            }

            updateAction(participant);
        }

        ParticipantStateChanged?.Invoke(this, participant);
    }

    // ---- Audio pipeline ----

    private async Task InitializeAudioAsync()
    {
        try
        {
            await _audioDeviceService.InitializeAsync();

            await _audioDeviceService.StartCaptureAsync(_settings);
            await _audioDeviceService.StartPlaybackAsync(_settings);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Ses başlatılamadı: {ex.Message}");
        }
    }

    private void OnPcmCaptured(byte[] pcm)
    {
        bool shouldSend;
        lock (_lock)
        {
            shouldSend = _session.IsConnected && !_isMuted && !_isDeafened;
        }

        if (!shouldSend) return;

        lock (_audioLock)
        {
            _captureBuffer.Write(pcm, 0, pcm.Length);

            const int frameBytes = 960 * 2; // 20ms mono 48kHz 16-bit
            while (_captureBuffer.Length >= frameBytes)
            {
                var frame = new byte[frameBytes];
                _captureBuffer.Read(frame, 0, frameBytes);

                var samples = new short[960];
                for (int i = 0; i < 960; i++)
                {
                    samples[i] = (short)(frame[i * 2] | (frame[i * 2 + 1] << 8));
                }

                ProcessAudioFrame(samples);
            }

            if (_captureBuffer.Length >= frameBytes * 2)
            {
                _captureBuffer.SetLength(0);
            }
        }
    }

    private void ProcessAudioFrame(short[] samples)
    {
        bool speaking = DetectSpeech(samples);
        UpdateSpeakingState(speaking);

        bool transmit;
        lock (_lock)
        {
            transmit = _session.IsConnected && !_isMuted && !_isDeafened;
        }

        if (!transmit) return;

        if (_settings.ActivationMode == VoiceActivationMode.VoiceActivity && !speaking) return;
        if (_settings.ActivationMode == VoiceActivationMode.PushToTalk && !_settings.IsPushToTalkActive) return;

        try
        {
            var encoded = _audioEncoder.EncodeAudio(samples, AudioCommonlyUsedFormats.OpusWebRTC);
            if (encoded.Length == 0) return;

            List<VoicePeerConnection> connected;
            lock (_lock)
            {
                connected = _peerConnections.Values.Where(p => p.IsConnected && p.Pc != null).ToList();
            }

            foreach (var peer in connected)
            {
                try
                {
                    peer.Pc!.SendAudio(960, encoded);
                }
                catch { }
            }
        }
        catch { }
    }

    private bool DetectSpeech(short[] samples)
    {
        double sumSquares = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            double v = samples[i];
            sumSquares += v * v;
        }

        double rms = Math.Sqrt(sumSquares / samples.Length);
        double normalized = rms / 32768.0;

        return normalized >= _settings.VoiceActivityThreshold;
    }

    private void UpdateSpeakingState(bool speaking)
    {
        bool changed;
        lock (_lock)
        {
            changed = _isSpeaking != speaking;
            _isSpeaking = speaking;
        }

        if (!changed) return;

        UpdateSelfParticipantState();

        if (DateTime.UtcNow - _lastSpeakingWrite > TimeSpan.FromMilliseconds(500))
        {
            _lastSpeakingWrite = DateTime.UtcNow;
            _ = WriteSelfParticipantStateAsync();
        }
    }

    private async Task WriteSelfParticipantStateAsync()
    {
        var partyId = _currentPartyId;
        var uid = _sessionManager.CurrentUser?.Uid;
        if (string.IsNullOrEmpty(partyId) || string.IsNullOrEmpty(uid)) return;

        try
        {
            var session = await _firestore.GetVoiceSessionAsync(partyId);
            if (session == null || session.Members.All(m => m.Uid != uid)) return;

            var participant = session.Members.First(m => m.Uid == uid);
            participant.IsSpeaking = _isSpeaking;
            participant.IsVoiceMuted = _isMuted;
            participant.IsVoiceDeafened = _isDeafened;

            var fields = BuildParticipantsFields(session.Members);
            await _firestore.UpdateVoiceSessionAsync(partyId, fields);
        }
        catch { }
    }

    private void OnAudioFrameReceived(EncodedAudioFrame frame)
    {
        bool shouldPlay;
        lock (_lock)
        {
            shouldPlay = _session.IsConnected && !_isDeafened;
        }

        if (!shouldPlay) return;

        try
        {
            var decoded = _audioEncoder.DecodeAudio(frame.EncodedAudio, frame.AudioFormat);
            if (decoded.Length == 0) return;

            var pcmBytes = new byte[decoded.Length * 2];
            Buffer.BlockCopy(decoded, 0, pcmBytes, 0, pcmBytes.Length);

            _audioDeviceService.WritePlaybackData(pcmBytes);
        }
        catch { }
    }

    // ---- WebRTC mesh ----

    private void SetupPeerConnection(string remoteUid, string displayName, string username, string avatar, VoiceParticipantState participant)
    {
        lock (_lock)
        {
            if (_peerConnections.ContainsKey(remoteUid)) return;

            var peer = new VoicePeerConnection
            {
                Uid = remoteUid,
                DisplayName = displayName,
                Username = username,
                Avatar = avatar,
                SignalDocId = GetSignalingDocId(_sessionManager.CurrentUser?.Uid ?? "", remoteUid),
                IsOfferer = string.CompareOrdinal(_sessionManager.CurrentUser?.Uid ?? "", remoteUid) < 0,
                Participant = participant
            };

            try
            {
                var pc = CreatePeerConnection(peer);
                peer.Pc = pc;
                _peerConnections[remoteUid] = peer;

                peer.SignalingCts = new CancellationTokenSource();
                _ = Task.Run(() => SignalingLoopAsync(peer), peer.SignalingCts.Token);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"WebRTC başlatılamadı ({displayName}): {ex.Message}");
            }
        }
    }

    private RTCPeerConnection CreatePeerConnection(VoicePeerConnection peer)
    {
        var config = new RTCConfiguration
        {
            iceServers = new List<RTCIceServer>
            {
                new() { urls = "stun:stun.l.google.com:19302" }
            }
        };

        var pc = new RTCPeerConnection(config);

        var track = new MediaStreamTrack(AudioCommonlyUsedFormats.OpusWebRTC, MediaStreamStatusEnum.SendRecv);
        pc.addTrack(track);

        pc.onicecandidate += (candidate) =>
        {
            lock (_lock)
            {
                if (candidate != null)
                {
                    try
                    {
                        peer.LocalCandidates.Add(candidate.toJSON());
                    }
                    catch { }
                }
            }
        };

        pc.onconnectionstatechange += (state) => OnConnectionStateChanged(peer, state);

        pc.OnAudioFrameReceived += OnAudioFrameReceived;

        return pc;
    }

    private void OnConnectionStateChanged(VoicePeerConnection peer, RTCPeerConnectionState state)
    {
        bool connected = state == RTCPeerConnectionState.connected;
        bool failed = state == RTCPeerConnectionState.failed ||
                      state == RTCPeerConnectionState.disconnected ||
                      state == RTCPeerConnectionState.closed;

        lock (_lock)
        {
            peer.IsConnected = connected;
        }

        if (connected)
        {
            peer.Participant.Quality = ConnectionQuality.Excellent;
            ParticipantStateChanged?.Invoke(this, peer.Participant);
        }
        else if (failed && _session.IsConnected && _currentPartyId != null)
        {
            peer.Participant.Quality = ConnectionQuality.Poor;
            ParticipantStateChanged?.Invoke(this, peer.Participant);

            _ = Task.Run(async () => await RestartPeerSignalingAsync(peer));
        }
    }

    private async Task RestartPeerSignalingAsync(VoicePeerConnection peer)
    {
        lock (_lock)
        {
            if (!_session.IsConnected || _currentPartyId == null) return;

            peer.SignalingCts?.Cancel();
            peer.SignalingCts?.Dispose();
            try { peer.Pc?.Dispose(); } catch { }
            peer.Pc = null;
            peer.LocalCandidates.Clear();
            peer.RemoteCandidatesApplied.Clear();
            peer.OfferWritten = false;
            peer.AnswerWritten = false;
            peer.OfferApplied = false;
            peer.AnswerApplied = false;
            peer.IsConnected = false;
        }

        try
        {
            await _firestore.DeleteVoiceSignalAsync(_currentPartyId!, peer.SignalDocId);
        }
        catch { }

        await Task.Delay(500);

        lock (_lock)
        {
            if (!_session.IsConnected || _currentPartyId == null) return;

            try
            {
                peer.Pc = CreatePeerConnection(peer);
                peer.SignalingCts = new CancellationTokenSource();
                _ = Task.Run(() => SignalingLoopAsync(peer), peer.SignalingCts.Token);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Yeniden bağlantı başlatılamadı ({peer.DisplayName}): {ex.Message}");
            }
        }
    }

    private async Task SignalingLoopAsync(VoicePeerConnection peer)
    {
        var cts = peer.SignalingCts;
        if (cts == null) return;

        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                await DoSignalingTickAsync(peer);
            }
            catch { }

            try
            {
                await Task.Delay(1500, cts.Token);
            }
            catch { break; }
        }
    }

    private async Task DoSignalingTickAsync(VoicePeerConnection peer)
    {
        var partyId = _currentPartyId;
        if (string.IsNullOrEmpty(partyId)) return;

        var signal = await _firestore.GetVoiceSignalAsync(partyId, peer.SignalDocId);

        // Publish offer if this side is the offerer and no offer written yet.
        if (peer.IsOfferer && !peer.OfferWritten && peer.Pc != null)
        {
            var offer = peer.Pc.createOffer();
            await peer.Pc.setLocalDescription(offer);

            var fields = new Dictionary<string, object>
            {
                ["partyId"] = Field("stringValue", partyId),
                ["fromUid"] = Field("stringValue", _sessionManager.CurrentUser?.Uid ?? ""),
                ["toUid"] = Field("stringValue", peer.Uid),
                ["offer"] = Field("stringValue", offer.sdp)
            };

            await _firestore.UpdateVoiceSignalAsync(partyId, peer.SignalDocId, fields);
            peer.OfferWritten = true;
        }

        // Answerer consumes the offer and produces an answer.
        if (!peer.IsOfferer && !peer.AnswerWritten && peer.Pc != null && signal?.Offer != null)
        {
            var offerInit = new RTCSessionDescriptionInit
            {
                type = RTCSdpType.offer,
                sdp = signal.Offer
            };

            peer.Pc.setRemoteDescription(offerInit);
            peer.OfferApplied = true;

            var answer = peer.Pc.createAnswer();
            await peer.Pc.setLocalDescription(answer);

            var fields = new Dictionary<string, object>
            {
                ["answer"] = Field("stringValue", answer.sdp)
            };

            await _firestore.UpdateVoiceSignalAsync(partyId, peer.SignalDocId, fields);
            peer.AnswerWritten = true;
        }

        // Offerer consumes the answer.
        if (peer.IsOfferer && !peer.AnswerApplied && signal?.Answer != null && peer.Pc != null)
        {
            var answerInit = new RTCSessionDescriptionInit
            {
                type = RTCSdpType.answer,
                sdp = signal.Answer
            };

            peer.Pc.setRemoteDescription(answerInit);
            peer.AnswerApplied = true;
        }

        // Publish new local ICE candidates.
        List<string> newLocal;
        lock (_lock)
        {
            newLocal = peer.LocalCandidates.Where(c => !peer.LocalCandidatesWritten.Contains(c)).ToList();
        }

        if (newLocal.Count > 0)
        {
            lock (_lock)
            {
                peer.LocalCandidatesWritten.AddRange(newLocal);
            }

            var fieldName = peer.IsOfferer ? "offererCandidates" : "answererCandidates";
            var fields = new Dictionary<string, object>
            {
                [fieldName] = ArrayField(peer.LocalCandidates)
            };

            await _firestore.UpdateVoiceSignalAsync(partyId, peer.SignalDocId, fields);
        }

        // Consume remote ICE candidates (only after the remote description has been applied,
        // otherwise SIPSorcery may reject them and they would never be retried).
        if (peer.Pc != null)
        {
            bool remoteDescriptionReady = peer.IsOfferer ? peer.AnswerApplied : peer.OfferApplied;
            if (remoteDescriptionReady)
            {
                var remoteCandidates = peer.IsOfferer ? signal?.AnswererCandidates : signal?.OffererCandidates;
                if (remoteCandidates != null)
                {
                    foreach (var candidateJson in remoteCandidates)
                    {
                        if (peer.RemoteCandidatesApplied.Add(candidateJson))
                        {
                            if (RTCIceCandidateInit.TryParse(candidateJson, out var init))
                            {
                                try
                                {
                                    peer.Pc.addIceCandidate(init);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
        }
    }

    // ---- Session maintenance ----

    private async Task CreateVoiceSessionDocumentAsync(string partyId)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var uid = _sessionManager.CurrentUser?.Uid;
        if (string.IsNullOrEmpty(uid)) return;

        var existing = await _firestore.GetVoiceSessionAsync(partyId);
        var members = existing?.Members.ToList() ?? new List<PartyMember>();

        if (members.All(m => m.Uid != uid))
        {
            members.Add(new PartyMember
            {
                Uid = uid,
                DisplayName = _sessionManager.Profile?.DisplayName ?? "",
                Username = _sessionManager.Profile?.Username ?? "",
                Avatar = _sessionManager.Profile?.Avatar ?? "",
                IsInVoice = true,
                IsVoiceMuted = false,
                IsVoiceDeafened = false,
                IsSpeaking = false,
                JoinedAt = DateTime.UtcNow
            });
        }

        var fields = BuildParticipantsFields(members);
        fields["partyId"] = Field("stringValue", partyId);
        fields["status"] = Field("stringValue", "active");
        fields["createdAt"] = Field("integerValue", now.ToString());

        await _firestore.UpdateVoiceSessionAsync(partyId, fields);
    }

    private async Task RemoveVoiceSessionParticipantAsync(string partyId)
    {
        var uid = _sessionManager.CurrentUser?.Uid;
        if (string.IsNullOrEmpty(uid)) return;

        try
        {
            var session = await _firestore.GetVoiceSessionAsync(partyId);
            if (session == null) return;

            var members = session.Members.Where(m => m.Uid != uid).ToList();
            if (members.Count == 0)
            {
                await _firestore.DeleteVoiceSessionAsync(partyId);
                return;
            }

            var fields = BuildParticipantsFields(members);
            await _firestore.UpdateVoiceSessionAsync(partyId, fields);
        }
        catch { }
    }

    private void StartSessionListener(string partyId)
    {
        _ = Task.Run(async () =>
        {
            while (_session.IsConnected && _currentPartyId == partyId)
            {
                try
                {
                    var session = await _firestore.GetVoiceSessionAsync(partyId);
                    if (session == null)
                    {
                        await LeaveVoiceChannelAsync();
                        break;
                    }

                    if (session.Members.All(m => m.Uid != _sessionManager.CurrentUser?.Uid))
                    {
                        await LeaveVoiceChannelAsync();
                        break;
                    }

                    lock (_lock)
                    {
                        foreach (var peer in _peerConnections.Values.ToList())
                        {
                            if (session.Members.All(m => m.Uid != peer.Uid))
                            {
                                peer.SignalingCts?.Cancel();
                                peer.SignalingCts?.Dispose();
                                try { peer.Pc?.Dispose(); } catch { }
                                _peerConnections.Remove(peer.Uid);
                                var stale = Participants.FirstOrDefault(p => p.Uid == peer.Uid);
                                if (stale != null)
                                {
                                    Participants.Remove(stale);
                                }
                            }
                        }
                    }

                    var selfUid = _sessionManager.CurrentUser?.Uid;
                    foreach (var member in session.Members)
                    {
                        if (member.Uid == selfUid) continue;

                        lock (_lock)
                        {
                            if (_peerConnections.ContainsKey(member.Uid)) continue;
                        }

                        var participantState = new VoiceParticipantState
                        {
                            Uid = member.Uid,
                            DisplayName = member.DisplayName,
                            Username = member.Username,
                            Avatar = member.Avatar,
                            IsLeader = member.IsLeader,
                            IsMuted = member.IsVoiceMuted,
                            IsDeafened = member.IsVoiceDeafened,
                            IsSpeaking = member.IsSpeaking,
                            Quality = ConnectionQuality.Excellent,
                            Ping = 0,
                            PacketLoss = 0
                        };

                        lock (_lock)
                        {
                            Participants.Add(participantState);
                        }

                        SetupPeerConnection(member.Uid, member.DisplayName, member.Username, member.Avatar, participantState);
                    }

                    // Synchronize remote participants' mute/deafen/speaking state from the session doc.
                    foreach (var member in session.Members)
                    {
                        if (member.Uid == selfUid) continue;

                        VoiceParticipantState? remote;
                        lock (_lock)
                        {
                            remote = Participants.FirstOrDefault(p => p.Uid == member.Uid);
                        }

                        if (remote != null)
                        {
                            remote.IsMuted = member.IsVoiceMuted;
                            remote.IsDeafened = member.IsVoiceDeafened;
                            remote.IsSpeaking = member.IsSpeaking;
                        }
                    }
                }
                catch { }

                await Task.Delay(3000);
            }
        });
    }

    private async Task StartReconnectWatcherAsync()
    {
        _reconnectCts = new CancellationTokenSource();

        await Task.Run(async () =>
        {
            while (!_reconnectCts.Token.IsCancellationRequested)
            {
                await Task.Delay(10000, _reconnectCts.Token);

                lock (_lock)
                {
                    if (_session.State != VoiceState.Connected) return;
                }

                var party = await _partyService.GetCurrentPartyAsync();
                if (party == null || !party.Members.Any(m => m.Uid == _sessionManager.CurrentUser?.Uid))
                {
                    await LeaveVoiceChannelAsync();
                    break;
                }
            }
        }, _reconnectCts.Token);
    }

    private void StartPingPong()
    {
        _pingCts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!_pingCts.Token.IsCancellationRequested)
            {
                try
                {
                    var startTime = DateTime.Now;

                    await Task.Delay(100, _pingCts.Token);

                    var elapsed = DateTime.Now.Subtract(startTime).TotalMilliseconds;

                    lock (_lock)
                    {
                        _connectionQuality.Ping = (int)elapsed;
                        _connectionQuality.PacketLoss = 0;
                    }
                }
                catch { break; }

                await Task.Delay(2000, _pingCts.Token);
            }
        }, _pingCts.Token);
    }

    private void StopPingPong()
    {
        _pingCts?.Cancel();
        _pingCts?.Dispose();
        _pingCts = null;
    }

    private void UpdateSelfParticipantState()
    {
        var selfUid = _sessionManager.CurrentUser?.Uid;
        if (string.IsNullOrEmpty(selfUid)) return;

        VoiceParticipantState? selfParticipant;
        lock (_lock)
        {
            selfParticipant = Participants.FirstOrDefault(p => p.Uid == selfUid);
            if (selfParticipant != null)
            {
                selfParticipant.IsMuted = _isMuted;
                selfParticipant.IsDeafened = _isDeafened;
                selfParticipant.IsSpeaking = _isSpeaking;
            }
        }

        if (selfParticipant != null)
        {
            ParticipantStateChanged?.Invoke(this, selfParticipant);
        }
    }

    private Dictionary<string, object> BuildParticipantsFields(List<PartyMember> members)
    {
        return new Dictionary<string, object>
        {
            ["participants"] = new Dictionary<string, object>
            {
                ["arrayValue"] = new Dictionary<string, object>
                {
                    ["values"] = members.Select(m => new Dictionary<string, object>
                    {
                        ["mapValue"] = new Dictionary<string, object>
                        {
                            ["fields"] = new Dictionary<string, object>
                            {
                                ["uid"] = Field("stringValue", m.Uid),
                                ["displayName"] = Field("stringValue", m.DisplayName),
                                ["username"] = Field("stringValue", m.Username),
                                ["avatar"] = Field("stringValue", m.Avatar),
                                ["isInVoice"] = Field("booleanValue", m.IsInVoice ? "true" : "false"),
                                ["isVoiceMuted"] = Field("booleanValue", m.IsVoiceMuted ? "true" : "false"),
                                ["isVoiceDeafened"] = Field("booleanValue", m.IsVoiceDeafened ? "true" : "false"),
                                ["isSpeaking"] = Field("booleanValue", m.IsSpeaking ? "true" : "false"),
                                ["joinedVoiceAt"] = Field("integerValue", m.JoinedAt.ToUnixTimeMilliseconds().ToString())
                            }
                        }
                    }).ToArray()
                }
            }
        };
    }

    private static string GetSignalingDocId(string uid1, string uid2)
    {
        var a = string.CompareOrdinal(uid1, uid2) <= 0 ? uid1 : uid2;
        var b = string.CompareOrdinal(uid1, uid2) <= 0 ? uid2 : uid1;
        return $"signal_{a}_{b}";
    }

    private static Dictionary<string, object> Field(string type, string value) =>
        new() { [type] = value };

    private static Dictionary<string, object> ArrayField(IEnumerable<string> values) =>
        new()
        {
            ["arrayValue"] = new Dictionary<string, object>
            {
                ["values"] = values.Select(v => new Dictionary<string, object>
                {
                    ["stringValue"] = v
                }).ToArray()
            }
        };

    public void Dispose()
    {
        _audioDeviceService.PcmCaptured -= OnPcmCaptured;

        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _pingCts?.Cancel();
        _pingCts?.Dispose();

        lock (_lock)
        {
            foreach (var peer in _peerConnections.Values)
            {
                peer.SignalingCts?.Cancel();
                peer.SignalingCts?.Dispose();
                try { peer.Pc?.Dispose(); } catch { }
            }
            _peerConnections.Clear();
        }

        _audioEncoder.Dispose();

        try { LeaveVoiceChannelAsync().Wait(); } catch { }
    }

    private sealed class VoicePeerConnection
    {
        public string Uid { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Avatar { get; set; } = "";
        public string SignalDocId { get; set; } = "";
        public bool IsOfferer { get; set; }
        public RTCPeerConnection? Pc { get; set; }
        public CancellationTokenSource? SignalingCts { get; set; }
        public bool OfferWritten { get; set; }
        public bool AnswerWritten { get; set; }
        public bool OfferApplied { get; set; }
        public bool AnswerApplied { get; set; }
        public bool IsConnected { get; set; }
        public List<string> LocalCandidates { get; } = new();
        public List<string> LocalCandidatesWritten { get; } = new();
        public HashSet<string> RemoteCandidatesApplied { get; } = new();
        public VoiceParticipantState Participant { get; set; } = new();
    }
}
