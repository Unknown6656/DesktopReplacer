using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.Win32;

using unknown6656;

namespace Essentials
{
    [WidgetInfo("Performance Monitor", "1.0")]
    public unsafe partial class PerformanceMonitor
    {
        private PerformanceCounter? _cpu_load;
        private PerformanceCounter? _cpu_temp0, _cpu_temp1;
        private int _ctr;


        public PerformanceMonitor() => InitializeComponent();

        public override void OnLoad()
        {
            NetworkChange.NetworkAvailabilityChanged += AvailabilityChangedCallback;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            TickInterval = 1000;
            _cpu_load = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpu_temp0 = new PerformanceCounter("Thermal Zone Information", "Temperature", @"\_TZ.TZ00");
            _cpu_temp1 = new PerformanceCounter("Thermal Zone Information", "Temperature", @"\_TZ.TZ01");
        }

        public override void OnUnload()
        {
            NetworkChange.NetworkAvailabilityChanged -= AvailabilityChangedCallback;
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;

            _cpu_load?.Dispose();
            _cpu_temp0?.Dispose();
            _cpu_temp1?.Dispose();
        }

        public override Task OnTick()
        {
            if ((_ctr = (_ctr + 1) % 20) == 0)
            {
                SystemEvents_PowerModeChanged(this, null);
                AvailabilityChangedCallback(this, null);
                // TODO : ?
            }

            GetPerformanceInfo(out PerformanceInformation ram, sizeof(PerformanceInformation));

            float cpu = _cpu_load?.NextValue() ?? 0;

            pb_cpu.Value = cpu / 100;
            pb_ram.Value = ram.AllocatedMemoryPercent;
            lb_cpu_load.Text = cpu.ToString("F1");
            lb_cpu_temp.Text = (Math.Max(_cpu_temp0?.NextValue() ?? 0, _cpu_temp1?.NextValue() ?? 0) - 273.15f).ToString("F2");
            lb_ram_load.Text = (ram.AllocatedMemoryPercent * 100).ToString("F1");
            lb_ram_gb.Text = $"{ram.AllocatedMemoryGB:F2}/{ram.TotalMemoryGB:F2}";

            return Task.CompletedTask;
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs? e)
        {
            GetSystemPowerStatus(out SYSTEM_POWER_STATUS bat);

            lb_bat_load.Text = bat.BatteryLifePercent.ToString();
            lb_bat_state.Text = (bat.BatteryFlag.HasFlag(BatteryFlag.LOW) ? "low"
                               : bat.BatteryFlag.HasFlag(BatteryFlag.HIGH) ? "high"
                               : bat.BatteryFlag.HasFlag(BatteryFlag.CRIT) ? "crit" : "unk.") + (bat.ACLineStatus == ACLineStatus.Online ? ", chg" : "");
        }

        private void AvailabilityChangedCallback(object? sender, NetworkAvailabilityEventArgs? e)
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

            GetBestInterface(0x08080808u /* 8.8.8.8 */, out int index);

            foreach (NetworkInterface n in adapters)
                if (n.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ip = n.GetIPProperties();

                    lb_eth_name.Text = n.Name;
                    lb_eth_iip.Text = ip.DhcpServerAddresses.FirstOrDefault()?.ToString() ?? "---.---.---.---";
                    lb_eth_mac.Text = string.Join(":", n.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
                    // lb_eth_eip.Text = n

                    if (ip.GetIPv4Properties()?.Index == index)
                        break;
                }
        }


        [DllImport("Iphlpapi.dll", SetLastError = true)]
        private static extern int GetBestInterface(uint ipv4, out int index);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS powerStatus);

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPerformanceInfo(out PerformanceInformation PerformanceInformation, int Size);

        [StructLayout(LayoutKind.Sequential)]
        private struct PerformanceInformation
        {
            public int Size;
            public void* CommitTotal;
            public void* CommitLimit;
            public void* CommitPeak;
            public void* PhysicalTotal;
            public void* PhysicalAvailable;
            public void* SystemCache;
            public void* KernelTotal;
            public void* KernelPaged;
            public void* KernelNonPaged;
            public void* PageSize;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;


            public float TotalMemoryGB => (long)PhysicalTotal * (long)PageSize / 1048576f / 1024f;

            public float AllocatedMemoryGB => TotalMemoryGB - PhysicalAvailableMemoryGB;

            public float AllocatedMemoryPercent => 1 - (PhysicalAvailableMemoryGB / TotalMemoryGB);

            public float PhysicalAvailableMemoryGB => (long)PhysicalAvailable * (long)PageSize / 1048576f / 1024f;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            public ACLineStatus ACLineStatus;
            public BatteryFlag BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        private enum ACLineStatus
            : byte
        {
            Offline = 0,
            Online = 1,
            Unknown = 255
        }

        [Flags]
        private enum BatteryFlag
            : byte
        {
            HIGH = 1,
            LOW = 2,
            CRIT = 4,
            CHG = 8,
            NONE = 128,
            UNKN = 255
        }
    }
}
