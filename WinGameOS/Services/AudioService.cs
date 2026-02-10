using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;

namespace WinGameOS.Services
{
    /// <summary>
    /// Manages system audio: volume, mute, device selection.
    /// Uses Windows Core Audio API via NAudio.
    /// </summary>
    public class AudioService : IDisposable
    {
        private MMDeviceEnumerator? _enumerator;

        public AudioService()
        {
            try
            {
                _enumerator = new MMDeviceEnumerator();
                LoggingService.Instance.Info("Audio service initialized.");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to initialize audio service", ex);
            }
        }

        /// <summary>
        /// Gets the default audio playback device.
        /// </summary>
        private MMDevice? GetDefaultDevice()
        {
            try
            {
                return _enumerator?.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the current master volume (0-100).
        /// </summary>
        public int GetVolume()
        {
            try
            {
                var device = GetDefaultDevice();
                if (device != null)
                    return (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to get volume", ex);
            }
            return 0;
        }

        /// <summary>
        /// Sets the master volume (0-100).
        /// </summary>
        public bool SetVolume(int level)
        {
            level = Math.Clamp(level, 0, 100);
            try
            {
                var device = GetDefaultDevice();
                if (device != null)
                {
                    device.AudioEndpointVolume.MasterVolumeLevelScalar = level / 100f;
                    LoggingService.Instance.Info($"Volume set to {level}%");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to set volume", ex);
            }
            return false;
        }

        /// <summary>
        /// Gets mute state.
        /// </summary>
        public bool IsMuted()
        {
            try
            {
                var device = GetDefaultDevice();
                return device?.AudioEndpointVolume.Mute ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Toggles mute or sets explicit mute state.
        /// </summary>
        public bool SetMute(bool mute)
        {
            try
            {
                var device = GetDefaultDevice();
                if (device != null)
                {
                    device.AudioEndpointVolume.Mute = mute;
                    LoggingService.Instance.Info(mute ? "Audio muted" : "Audio unmuted");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to set mute", ex);
            }
            return false;
        }

        /// <summary>
        /// Toggles mute on/off.
        /// </summary>
        public bool ToggleMute()
        {
            return SetMute(!IsMuted());
        }

        /// <summary>
        /// Lists available audio output devices.
        /// </summary>
        public List<Models.AudioDeviceInfo> GetOutputDevices()
        {
            var devices = new List<Models.AudioDeviceInfo>();
            try
            {
                if (_enumerator == null) return devices;

                var defaultDevice = GetDefaultDevice();
                string defaultId = defaultDevice?.ID ?? "";

                var collection = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                foreach (var device in collection)
                {
                    devices.Add(new Models.AudioDeviceInfo
                    {
                        Id = device.ID,
                        Name = device.FriendlyName,
                        IsDefault = device.ID == defaultId,
                        IsActive = device.State == DeviceState.Active
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to enumerate audio devices", ex);
            }
            return devices;
        }

        public void Dispose()
        {
            _enumerator?.Dispose();
            _enumerator = null;
        }
    }
}
