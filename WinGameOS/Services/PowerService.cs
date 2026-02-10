using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinGameOS.Helpers;
using WinGameOS.Models;

namespace WinGameOS.Services
{
    /// <summary>
    /// Manages power plans, battery status, and performance mode switching.
    /// </summary>
    public class PowerService
    {
        public PowerService()
        {
            LoggingService.Instance.Info("Power service initialized.");
        }

        /// <summary>
        /// Gets the current battery status.
        /// </summary>
        public (int Percent, bool IsCharging, TimeSpan TimeRemaining) GetBatteryStatus()
        {
            try
            {
                NativeApi.GetSystemPowerStatus(out var status);
                int percent = status.BatteryLifePercent;
                if (percent > 100) percent = 100;
                bool isCharging = status.ACLineStatus == 1;
                var timeRemaining = status.BatteryLifeTime >= 0
                    ? TimeSpan.FromSeconds(status.BatteryLifeTime)
                    : TimeSpan.Zero;

                return (percent, isCharging, timeRemaining);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to get battery status", ex);
                return (0, false, TimeSpan.Zero);
            }
        }

        /// <summary>
        /// Gets the current active power plan name.
        /// </summary>
        public PerformanceMode GetCurrentPerformanceMode()
        {
            try
            {
                NativeApi.PowerGetActiveScheme(IntPtr.Zero, out IntPtr activeGuidPtr);
                if (activeGuidPtr != IntPtr.Zero)
                {
                    Guid activeGuid = Marshal.PtrToStructure<Guid>(activeGuidPtr);
                    Marshal.FreeHGlobal(activeGuidPtr);

                    if (activeGuid == NativeApi.GUID_POWER_SAVER)
                        return PerformanceMode.Eco;
                    if (activeGuid == NativeApi.GUID_BALANCED)
                        return PerformanceMode.Balanced;
                    if (activeGuid == NativeApi.GUID_HIGH_PERFORMANCE)
                        return PerformanceMode.Performance;
                    if (activeGuid == NativeApi.GUID_ULTIMATE_PERF)
                        return PerformanceMode.Turbo;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to get power plan", ex);
            }
            return PerformanceMode.Balanced;
        }

        /// <summary>
        /// Sets the power plan (performance mode).
        /// </summary>
        public (bool Success, string Message) SetPerformanceMode(PerformanceMode mode)
        {
            try
            {
                Guid planGuid = mode switch
                {
                    PerformanceMode.Eco => NativeApi.GUID_POWER_SAVER,
                    PerformanceMode.Balanced => NativeApi.GUID_BALANCED,
                    PerformanceMode.Performance => NativeApi.GUID_HIGH_PERFORMANCE,
                    PerformanceMode.Turbo => NativeApi.GUID_ULTIMATE_PERF,
                    _ => NativeApi.GUID_BALANCED
                };

                uint result = NativeApi.PowerSetActiveScheme(IntPtr.Zero, ref planGuid);
                if (result == 0)
                {
                    string msg = $"Performance mode set to {mode}";
                    LoggingService.Instance.Info(msg);
                    return (true, msg);
                }
                else
                {
                    // If Ultimate Performance is not available, try High Performance
                    if (mode == PerformanceMode.Turbo)
                    {
                        planGuid = NativeApi.GUID_HIGH_PERFORMANCE;
                        result = NativeApi.PowerSetActiveScheme(IntPtr.Zero, ref planGuid);
                        if (result == 0)
                        {
                            string msg = "Turbo mode not available, using High Performance instead";
                            LoggingService.Instance.Warning(msg);
                            return (true, msg);
                        }
                    }

                    string errMsg = $"Failed to set performance mode to {mode} (error code: {result})";
                    LoggingService.Instance.Error(errMsg);
                    return (false, errMsg);
                }
            }
            catch (Exception ex)
            {
                string msg = $"Error setting performance mode: {ex.Message}";
                LoggingService.Instance.Error(msg, ex);
                return (false, msg);
            }
        }

        /// <summary>
        /// Gets system performance data snapshot.
        /// </summary>
        public PerformanceSnapshot GetPerformanceSnapshot()
        {
            var snapshot = new PerformanceSnapshot();

            try
            {
                // Battery
                var (percent, isCharging, timeRemaining) = GetBatteryStatus();
                snapshot.BatteryPercent = percent;
                snapshot.IsCharging = isCharging;
                snapshot.BatteryTimeRemaining = timeRemaining;

                // CPU usage via WMI
                using (var cpuSearcher = new System.Management.ManagementObjectSearcher(
                    "SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'"))
                {
                    foreach (var obj in cpuSearcher.Get())
                    {
                        snapshot.CpuUsage = Convert.ToSingle(obj["PercentProcessorTime"]);
                        break;
                    }
                }

                // RAM
                using (var ramSearcher = new System.Management.ManagementObjectSearcher(
                    "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in ramSearcher.Get())
                    {
                        float totalKB = Convert.ToSingle(obj["TotalVisibleMemorySize"]);
                        float freeKB = Convert.ToSingle(obj["FreePhysicalMemory"]);
                        snapshot.RamTotalGB = totalKB / 1048576f;
                        snapshot.RamUsedGB = (totalKB - freeKB) / 1048576f;
                        break;
                    }
                }

                // CPU Temperature via WMI (may not be available on all systems)
                try
                {
                    using var tempSearcher = new System.Management.ManagementObjectSearcher(
                        "root\\WMI",
                        "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
                    foreach (var obj in tempSearcher.Get())
                    {
                        // Temperature in tenths of Kelvin
                        float tempK = Convert.ToSingle(obj["CurrentTemperature"]);
                        snapshot.CpuTemperature = (tempK / 10f) - 273.15f;
                        break;
                    }
                }
                catch
                {
                    snapshot.CpuTemperature = -1; // Not available
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to get performance snapshot", ex);
            }

            return snapshot;
        }

        /// <summary>
        /// Put the system to sleep.
        /// </summary>
        public void Sleep()
        {
            try
            {
                LoggingService.Instance.Info("System going to sleep...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = "powrprof.dll,SetSuspendState 0,1,0",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to put system to sleep", ex);
            }
        }

        /// <summary>
        /// Shutdown the system.
        /// </summary>
        public void Shutdown()
        {
            try
            {
                LoggingService.Instance.Info("System shutting down...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/s /t 5",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to shutdown", ex);
            }
        }

        /// <summary>
        /// Restart the system.
        /// </summary>
        public void Restart()
        {
            try
            {
                LoggingService.Instance.Info("System restarting...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/r /t 5",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to restart", ex);
            }
        }
    }
}
