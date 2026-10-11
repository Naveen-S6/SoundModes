using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace SoundModes.Services;

/// <summary>
/// Master volume of the default playback device via Windows Core Audio (NAudio).
/// Follows volume changes made elsewhere and rebinds when the default device changes.
/// Must be created on the UI thread; <see cref="Changed"/> is always raised on that thread.
/// </summary>
public sealed class VolumeService : IMMNotificationClient, IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly SynchronizationContext _ui;
    private MMDevice? _device;

    /// <summary>Raised with the current volume (0-100), or null when no playback device is available.</summary>
    public event Action<int?>? Changed;

    /// <summary>Binds to the current default device and starts listening for device changes.</summary>
    public VolumeService()
    {
        _ui = SynchronizationContext.Current ?? throw new InvalidOperationException("Create VolumeService on the UI thread.");
        _enumerator.RegisterEndpointNotificationCallback(this);
        Bind();
    }

    /// <summary>Current volume 0-100, or null when no playback device is available.</summary>
    public int? Volume => _device == null ? null : (int)Math.Round(_device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);

    /// <summary>Sets master volume, clamped to 0-100. No-op without a device.</summary>
    public void SetVolume(int percent)
    {
        if (_device == null) return;
        try { _device.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(percent, 0, 100) / 100f; }
        catch (COMException ex) { Log.Warn("Setting volume failed", ex); }
    }

    private void Bind()
    {
        Unbind();
        try
        {
            _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _device.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
        }
        catch (COMException ex)
        {
            Log.Warn("No default playback device", ex);
            _device = null;
        }
        Changed?.Invoke(Volume);
    }

    private void Unbind()
    {
        if (_device == null) return;
        _device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
        _device.Dispose();
        _device = null;
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data) =>
        _ui.Post(_ => Changed?.Invoke((int)Math.Round(data.MasterVolume * 100)), null);

    // Core Audio callbacks arrive on a COM thread; rebind on the UI thread.
    void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Render && role == Role.Multimedia) _ui.Post(_ => Bind(), null);
    }

    void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState) { }
    void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId) { }
    void IMMNotificationClient.OnDeviceRemoved(string deviceId) { }
    void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    /// <summary>Stops listening and releases the device.</summary>
    public void Dispose()
    {
        _enumerator.UnregisterEndpointNotificationCallback(this);
        Unbind();
        _enumerator.Dispose();
    }
}
