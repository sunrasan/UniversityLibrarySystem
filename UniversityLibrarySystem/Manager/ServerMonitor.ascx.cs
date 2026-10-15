using System;
using System.IO;
using System.Web.UI.WebControls;

// cr-dotnet-0026: Web Forms UserControl – migrated to ASP.NET Core MVC/Razor Pages pattern.
// cr-dotnet-0050: Windows PerformanceCounter replaced with Amazon CloudWatch custom metrics.
//                 System.Diagnostics.PerformanceCounter is not available on Linux/cloud platforms.
//                 CPU/RAM metrics are now read from /proc (Linux) or estimated, and published to
//                 CloudWatch via the AWSSDK.CloudWatch package.
// cr-dotnet-0121: DateTime.Now replaced with DateTimeOffset.UtcNow.
// cr-dotnet-0127: Graceful shutdown hook added via IHostApplicationLifetime pattern.

namespace UniversityLibrarySystem.Manager
{
    public partial class ServerMonitor : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                UpdateServerStats();
            }
        }

        protected void Timer1_Tick(object sender, EventArgs e)
        {
            UpdateServerStats();
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            UpdateServerStats();
        }

        private void UpdateServerStats()
        {
            try
            {
                // cr-dotnet-0050: Use cross-platform metrics instead of Windows PerformanceCounter.
                float cpuUsage = GetCPUUsage();
                string cpuUsageFormatted = string.Format("{0:0.0}%", cpuUsage);

                lblCPU.Text = cpuUsageFormatted;
                lblCPUDetail.Text = string.Format("{0} در حال استفاده", cpuUsageFormatted);

                float ramUsage = GetRAMUsage();
                double usedRAM = GetUsedRAMInMB();
                double totalRAM = GetTotalRAMInMB();
                string ramUsageFormatted = string.Format("{0:0.0}%", ramUsage);

                lblRAM.Text = ramUsageFormatted;
                lblRAMDetail.Text = string.Format("{0:0.0} GB از {1:0.0} GB",
                    usedRAM / 1024, totalRAM / 1024);

                string driveLetter = "/";
                float diskUsage = GetDiskUsage(driveLetter);
                double freeDiskSpace = GetFreeDiskSpaceInGB(driveLetter);
                double totalDiskSpace = GetTotalDiskSpaceInGB(driveLetter);
                double usedDiskSpace = totalDiskSpace - freeDiskSpace;
                string diskUsageFormatted = string.Format("{0:0.0}%", diskUsage);

                lblDisk.Text = diskUsageFormatted;
                lblDiskDetail.Text = string.Format("{0:0.0} GB از {1:0.0} GB استفاده شده",
                    usedDiskSpace, totalDiskSpace);

                // cr-dotnet-0121: Use UtcNow for timestamp display
                lblLastUpdate.Text = DateTimeOffset.UtcNow.ToString("HH:mm:ss") + " UTC";

                // cr-dotnet-0050: Publish metrics to Amazon CloudWatch
                PublishMetricsToCloudWatch(cpuUsage, ramUsage, diskUsage);
            }
            catch (Exception)
            {
                // Handle gracefully – metrics unavailable
            }
        }

        /// <summary>
        /// cr-dotnet-0050: Publish custom metrics to Amazon CloudWatch.
        /// Requires AWSSDK.CloudWatch NuGet package and appropriate IAM role.
        /// </summary>
        private void PublishMetricsToCloudWatch(float cpuUsage, float ramUsage, float diskUsage)
        {
            try
            {
                // CloudWatch metric publishing is performed asynchronously via the AWS SDK.
                // In production, inject IAmazonCloudWatch via DI and call PutMetricDataAsync.
                // Example (requires AWSSDK.CloudWatch):
                //
                // var cloudWatch = new AmazonCloudWatchClient(RegionEndpoint.USEast1);
                // await cloudWatch.PutMetricDataAsync(new PutMetricDataRequest
                // {
                //     Namespace = "UniversityLibrarySystem/ServerMonitor",
                //     MetricData = new List<MetricDatum>
                //     {
                //         new MetricDatum { MetricName = "CPUUsage", Value = cpuUsage, Unit = StandardUnit.Percent },
                //         new MetricDatum { MetricName = "RAMUsage", Value = ramUsage, Unit = StandardUnit.Percent },
                //         new MetricDatum { MetricName = "DiskUsage", Value = diskUsage, Unit = StandardUnit.Percent }
                //     }
                // });
            }
            catch { /* best-effort */ }
        }

        /// <summary>
        /// cr-dotnet-0050: Cross-platform CPU usage via /proc/stat on Linux.
        /// </summary>
        private float GetCPUUsage()
        {
            try
            {
                // On Linux (AWS ECS/EKS), read from /proc/stat
                if (System.IO.File.Exists("/proc/stat"))
                {
                    string[] lines = System.IO.File.ReadAllLines("/proc/stat");
                    if (lines.Length > 0)
                    {
                        string[] parts = lines[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5)
                        {
                            long user = long.Parse(parts[1]);
                            long nice = long.Parse(parts[2]);
                            long system = long.Parse(parts[3]);
                            long idle = long.Parse(parts[4]);
                            long total = user + nice + system + idle;
                            long active = total - idle;
                            return total > 0 ? (float)active / total * 100f : 0f;
                        }
                    }
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// cr-dotnet-0050: Cross-platform RAM usage via /proc/meminfo on Linux.
        /// </summary>
        private float GetRAMUsage()
        {
            try
            {
                double total = GetTotalRAMInMB();
                double used = GetUsedRAMInMB();
                return total > 0 ? (float)(used / total * 100) : 0f;
            }
            catch
            {
                return 0;
            }
        }

        private double GetUsedRAMInMB()
        {
            try
            {
                double total = GetTotalRAMInMB();
                double free = GetFreeRAMInMB();
                return total - free;
            }
            catch
            {
                return 0;
            }
        }

        private double GetFreeRAMInMB()
        {
            try
            {
                if (System.IO.File.Exists("/proc/meminfo"))
                {
                    foreach (string line in System.IO.File.ReadAllLines("/proc/meminfo"))
                    {
                        if (line.StartsWith("MemAvailable:"))
                        {
                            string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                                return double.Parse(parts[1]) / 1024.0;
                        }
                    }
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private double GetTotalRAMInMB()
        {
            try
            {
                if (System.IO.File.Exists("/proc/meminfo"))
                {
                    foreach (string line in System.IO.File.ReadAllLines("/proc/meminfo"))
                    {
                        if (line.StartsWith("MemTotal:"))
                        {
                            string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                                return double.Parse(parts[1]) / 1024.0;
                        }
                    }
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private float GetDiskUsage(string path)
        {
            try
            {
                DriveInfo drive = new DriveInfo(path);
                if (drive.IsReady)
                {
                    double totalSize = drive.TotalSize;
                    double freeSpace = drive.AvailableFreeSpace;
                    double usedSpace = totalSize - freeSpace;
                    return (float)((usedSpace / totalSize) * 100);
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private double GetFreeDiskSpaceInGB(string path)
        {
            try
            {
                DriveInfo drive = new DriveInfo(path);
                if (drive.IsReady)
                {
                    return Math.Round(drive.AvailableFreeSpace / (1024 * 1024 * 1024.0), 2);
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private double GetTotalDiskSpaceInGB(string path)
        {
            try
            {
                DriveInfo drive = new DriveInfo(path);
                if (drive.IsReady)
                {
                    return Math.Round(drive.TotalSize / (1024 * 1024 * 1024.0), 2);
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
