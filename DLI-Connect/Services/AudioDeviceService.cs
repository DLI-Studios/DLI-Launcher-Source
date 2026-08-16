using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DLI.Connect.Models;
using DLI.Connect.Services.Interfaces;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DLI.Connect.Services;

public class AudioDeviceService : IAudioDeviceService
{
    private const int SampleRate = 48000;

    private readonly object _lock = new();
    private MMDevice? _selectedInputDevice;
    private MMDevice? _selectedOutputDevice;
    private WaveInEvent? _waveIn;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _waveProvider;
    private readonly List<AudioDevice> _inputDevices = new();
    private readonly List<AudioDevice> _outputDevices = new();
    private bool _isMuted;
    private float _inputVolume = 1.0f;
    private float _outputVolume = 1.0f;

    public event EventHandler<string>? DeviceChanged;
    public event Action<byte[]>? PcmCaptured;

    public List<AudioDevice> AvailableInputDevices => _inputDevices;
    public List<AudioDevice> AvailableOutputDevices => _outputDevices;

    public bool IsInitialized { get; private set; }
    public string? SelectedInputDeviceId => _selectedInputDevice?.ID;
    public string? SelectedOutputDeviceId => _selectedOutputDevice?.ID;

    public async Task InitializeAsync()
    {
        try
        {
            await RefreshDevicesAsync();

            var audioDeviceManager = new MMDeviceEnumerator();
            var defaultInput = audioDeviceManager.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            var defaultOutput = audioDeviceManager.GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications);

            if (defaultInput != null)
            {
                _selectedInputDevice = defaultInput;
            }

            if (defaultOutput != null)
            {
                _selectedOutputDevice = defaultOutput;
            }

            IsInitialized = true;
        }
        catch
        {
            IsInitialized = false;
        }
    }

    public void Dispose()
    {
        StopCaptureAsync().Wait();
        StopPlaybackAsync().Wait();

        _waveIn?.Dispose();
        _waveOut?.Dispose();

        _waveIn = null;
        _waveOut = null;
        _waveProvider = null;
    }

    public async Task RefreshDevicesAsync()
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                var audioDeviceManager = new MMDeviceEnumerator();
                var allDevices = audioDeviceManager.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);

                _inputDevices.Clear();
                _outputDevices.Clear();

                foreach (var device in allDevices)
                {
                    if (device.State == DeviceState.Active)
                    {
                        _inputDevices.Add(new AudioDevice
                        {
                            Id = device.ID,
                            Name = device.FriendlyName,
                            Type = DeviceType.Input
                        });

                        _outputDevices.Add(new AudioDevice
                        {
                            Id = device.ID,
                            Name = device.FriendlyName,
                            Type = DeviceType.Output
                        });
                    }
                }

                _inputDevices.Insert(0, new AudioDevice
                {
                    Id = "",
                    Name = "Varsayılan",
                    Type = DeviceType.Input
                });

                _outputDevices.Insert(0, new AudioDevice
                {
                    Id = "",
                    Name = "Varsayılan",
                    Type = DeviceType.Output
                });
            }
        });
    }

    public async Task SetInputDeviceAsync(string deviceId)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                var audioDeviceManager = new MMDeviceEnumerator();

                if (string.IsNullOrEmpty(deviceId))
                {
                    _selectedInputDevice = audioDeviceManager.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                }
                else
                {
                    var devices = audioDeviceManager.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                    _selectedInputDevice = devices.FirstOrDefault(d => d.ID == deviceId);
                }

                DeviceChanged?.Invoke(this, "input");
            }
        });
    }

    public async Task SetOutputDeviceAsync(string deviceId)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                var audioDeviceManager = new MMDeviceEnumerator();

                if (string.IsNullOrEmpty(deviceId))
                {
                    _selectedOutputDevice = audioDeviceManager.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                }
                else
                {
                    var devices = audioDeviceManager.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    _selectedOutputDevice = devices.FirstOrDefault(d => d.ID == deviceId);
                }

                DeviceChanged?.Invoke(this, "output");
            }
        });
    }

    public async Task StartCaptureAsync(VoiceSettings settings)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                StopCaptureAsync().Wait();

                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(SampleRate, 1),
                    DeviceNumber = GetDeviceIndex(_selectedInputDevice, DataFlow.Capture),
                    BufferMilliseconds = 20
                };

                _waveIn.DataAvailable += (_, e) =>
                {
                    if (_isMuted) return;

                    var processedData = ApplyAudioProcessing(e.Buffer, e.BytesRecorded, settings);
                    PcmCaptured?.Invoke(processedData);
                };

                _waveIn.StartRecording();
            }
        });
    }

    public async Task StopCaptureAsync()
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    _waveIn?.StopRecording();
                    _waveIn?.Dispose();
                    _waveIn = null;
                }
                catch { }
            }
        });
    }

    public async Task StartPlaybackAsync(VoiceSettings settings)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                StopPlaybackAsync().Wait();

                _waveProvider = new BufferedWaveProvider(new WaveFormat(SampleRate, 1))
                {
                    DiscardOnBufferOverflow = true
                };

                _waveOut = new WaveOutEvent
                {
                    DeviceNumber = GetDeviceIndex(_selectedOutputDevice, DataFlow.Render)
                };

                _waveOut.Volume = _outputVolume;

                _waveOut.Init(_waveProvider);
                _waveOut.Play();
            }
        });
    }

    public async Task StopPlaybackAsync()
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    _waveOut?.Stop();
                    _waveOut?.Dispose();
                    _waveProvider?.ClearBuffer();
                    _waveOut = null;
                    _waveProvider = null;
                }
                catch { }
            }
        });
    }

    public void WritePlaybackData(byte[] pcm)
    {
        BufferedWaveProvider? provider;
        lock (_lock)
        {
            provider = _waveProvider;
        }

        if (provider == null || pcm.Length == 0) return;

        provider.AddSamples(pcm, 0, pcm.Length);
    }

    public void ClearPlaybackBuffer()
    {
        lock (_lock)
        {
            try
            {
                _waveProvider?.ClearBuffer();
            }
            catch { }
        }
    }

    public async Task ToggleMuteAsync(bool muted)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                _isMuted = muted;
            }
        });
    }

    public async Task SetInputVolumeAsync(float volume)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                _inputVolume = Math.Clamp(volume, 0f, 1f);
            }
        });
    }

    public async Task SetOutputVolumeAsync(float volume)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                _outputVolume = Math.Clamp(volume, 0f, 1f);
                if (_waveOut != null)
                {
                    _waveOut.Volume = _outputVolume;
                }
            }
        });
    }

    private int GetDeviceIndex(MMDevice? device, DataFlow dataFlow)
    {
        if (device == null) return -1;

        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active);

        int index = 0;
        foreach (var d in devices)
        {
            if (d.ID == device.ID) return index;
            index++;
        }

        return -1;
    }

    private byte[] ApplyAudioProcessing(byte[] data, int length, VoiceSettings settings)
    {
        if (!settings.NoiseSuppression && !settings.EchoCancellation && !settings.AutomaticGainControl)
        {
            var processedData = new byte[length];
            Array.Copy(data, processedData, length);
            return processedData;
        }

        var samples = new float[length / 2];
        for (int i = 0; i < length / 2; i++)
        {
            samples[i] = BitConverter.ToInt16(data, i * 2) / 32768f;
        }

        if (settings.AutomaticGainControl)
        {
            float max = 0f;
            foreach (var s in samples)
            {
                if (Math.Abs(s) > max) max = Math.Abs(s);
            }

            if (max > 0.01f && max < 1.0f)
            {
                float gain = 1.0f / max;
                gain = Math.Min(gain, 10f);
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] *= gain;
                }
            }
        }

        if (settings.NoiseSuppression)
        {
            var threshold = 0.02f;
            for (int i = 0; i < samples.Length; i++)
            {
                if (Math.Abs(samples[i]) < threshold)
                {
                    samples[i] *= 0.1f;
                }
            }
        }

        var finalData = new byte[length];
        for (int i = 0; i < length / 2; i++)
        {
            var clipped = Math.Max(-1f, Math.Min(1f, samples[i]));
            var bytes = BitConverter.GetBytes((short)(clipped * 32767));
            Array.Copy(bytes, 0, finalData, i * 2, 2);
        }

        return finalData;
    }
}
