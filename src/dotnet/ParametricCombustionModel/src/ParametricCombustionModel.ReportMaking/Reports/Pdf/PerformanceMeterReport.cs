using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Models;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Resources;
using ParametricCombustionModel.Telemetry;
using ParametricCombustionModel.Telemetry.Instruments;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

public class PerformanceMeterReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    private readonly PerformanceMeter _performanceMeter;

    public PerformanceMeterReport(ReportContextDto reportContext) : base(reportContext.OptimizationResult)
    {
        _performanceMeter = reportContext.Meter
                            ?? throw new ArgumentNullException(nameof(reportContext.Meter),
                                                              "Performance meter cannot be null for PerformanceMeterReport.");
    }

    public Queue<IPdfOperation> Transform()
    {
        var operations = new Queue<IPdfOperation>();

        operations.Enqueue(new PrintTextOperation(
                               PerformanceMeterReportResources.Header,
                               TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new LineBreakOperation());

        // System Information Section
        AddSystemInfo(operations);

        // Total Execution Time Section
        AddTotalExecutionTime(operations);

        // Garbage Collection Section
        AddGarbageCollectionInfo(operations);

        // Detailed Performance Metrics Section
        AddDetailedPerformanceMetrics(operations);

        return operations;
    }

    private static string GetProcessorName()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return key?.GetValue("ProcessorNameString")?.ToString() ?? "Unknown Processor";
            }
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var cpuInfo = File.ReadAllText("/proc/cpuinfo");
                var lines = cpuInfo.Split('\n');
                var modelLine = lines.FirstOrDefault(line => line.StartsWith("model name"));
                if (modelLine != null)
                {
                    var colonIndex = modelLine.IndexOf(':');
                    if (colonIndex >= 0 && colonIndex < modelLine.Length - 1)
                        return modelLine.Substring(colonIndex + 1).Trim();
                }
            }
            
            return "Unknown Processor";
        }
        catch
        {
            return "Unknown Processor";
        }
    }

    private static string GetMemoryInfo()
    {
        try
        {
            var memoryType = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
            var memoryFrequency = GetMemoryFrequency();
            
            var totalPhysicalMemoryBytes = GetTotalPhysicalMemory();
            
            if (totalPhysicalMemoryBytes > 0)
            {
                var memoryGB = Math.Round(totalPhysicalMemoryBytes / (1024.0 * 1024.0 * 1024.0), 2);
                return $"{memoryGB}|{memoryType}|{memoryFrequency}";
            }
            
            var gcMemory = GC.GetTotalMemory(false);
            var gcMemoryGB = Math.Round(gcMemory / (1024.0 * 1024.0 * 1024.0), 2);
            return $"{gcMemoryGB} (GC)|{memoryType}|{memoryFrequency}";
        }
        catch
        {
            return "Unknown|Unknown|Unknown";
        }
    }

    private static long GetTotalPhysicalMemory()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsPhysicalMemory();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxPhysicalMemory();
            }
            
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private static long GetWindowsPhysicalMemory()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "computersystem get TotalPhysicalMemory /value",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd();
            process?.WaitForExit();
            
            if (!string.IsNullOrEmpty(output))
            {
                var lines = output.Split('\n');
                var memoryLine = lines.FirstOrDefault(line => line.StartsWith("TotalPhysicalMemory="));
                if (memoryLine != null)
                {
                    var memoryStr = memoryLine.Split('=')[1].Trim();
                    if (long.TryParse(memoryStr, out var totalPhysicalMemory))
                    {
                        return totalPhysicalMemory;
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

    private static long GetLinuxPhysicalMemory()
    {
        try
        {
            var memInfo = File.ReadAllText("/proc/meminfo");
            var lines = memInfo.Split('\n');
            var memTotalLine = lines.FirstOrDefault(line => line.StartsWith("MemTotal:"));
            if (memTotalLine != null)
            {
                var parts = memTotalLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && long.TryParse(parts[1], out var memoryKB))
                {
                    return memoryKB * 1024;
                }
            }
            
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string GetProcessorFrequency()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsProcessorFrequency();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxProcessorFrequency();
            }
            
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetWindowsProcessorFrequency()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "cpu get CurrentClockSpeed /value",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd();
            process?.WaitForExit();
            
            if (!string.IsNullOrEmpty(output))
            {
                var lines = output.Split('\n');
                var clockLine = lines.FirstOrDefault(line => line.StartsWith("CurrentClockSpeed="));
                if (clockLine != null)
                {
                    var clockStr = clockLine.Split('=')[1].Trim();
                    if (double.TryParse(clockStr, out var clockMHz))
                    {
                        var clockGHz = Math.Round(clockMHz / 1000.0, 2);
                        return $"{clockGHz} GHz";
                    }
                }
            }
            
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetLinuxProcessorFrequency()
    {
        try
        {
            var cpuInfo = File.ReadAllText("/proc/cpuinfo");
            var lines = cpuInfo.Split('\n');
            var freqLine = lines.FirstOrDefault(line => line.StartsWith("cpu MHz"));
            if (freqLine != null)
            {
                var colonIndex = freqLine.IndexOf(':');
                if (colonIndex >= 0 && colonIndex < freqLine.Length - 1)
                {
                    var freqStr = freqLine.Substring(colonIndex + 1).Trim();
                    if (double.TryParse(freqStr, out var freqMHz))
                    {
                        var freqGHz = Math.Round(freqMHz / 1000.0, 2);
                        return $"{freqGHz} GHz";
                    }
                }
            }
            
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetMemoryFrequency()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsMemoryFrequency();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxMemoryFrequency();
            }
            
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetWindowsMemoryFrequency()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "memorychip get Speed /value",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd();
            process?.WaitForExit();
            
            if (!string.IsNullOrEmpty(output))
            {
                var lines = output.Split('\n');
                var speedLine = lines.FirstOrDefault(line => line.StartsWith("Speed=") && !line.Contains("Speed=0"));
                if (speedLine != null)
                {
                    var speedStr = speedLine.Split('=')[1].Trim();
                    if (int.TryParse(speedStr, out var speedMHz))
                    {
                        return $"DDR-{speedMHz}";
                    }
                }
            }
            
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetLinuxMemoryFrequency()
    {
        try
        {
            var dmidecodeInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dmidecode",
                Arguments = "-t memory",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(dmidecodeInfo);
            var output = process?.StandardOutput.ReadToEnd();
            process?.WaitForExit();
            
            if (!string.IsNullOrEmpty(output))
            {
                var lines = output.Split('\n');
                var speedLine = lines.FirstOrDefault(line => line.Trim().StartsWith("Speed:") && !line.Contains("Unknown"));
                if (speedLine != null)
                {
                    var speedPart = speedLine.Split(':')[1].Trim();
                    return speedPart;
                }
            }
            
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private void AddSystemInfo(Queue<IPdfOperation> operations)
    {
        operations.Enqueue(new PrintTextOperation(
                               PerformanceMeterReportResources.SystemInfo,
                               TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(PerformanceMeterReportResources.OperatingSystem,
                                           $"{Environment.OSVersion} ({RuntimeInformation.OSDescription})"),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        var processorName = GetProcessorName().Replace("-Core", $"-Core, {Environment.ProcessorCount}-Thread");
        processorName += $" (Base Clock Frequency {GetProcessorFrequency()})";
        operations.Enqueue(new PrintTextOperation(
                               string.Format(PerformanceMeterReportResources.ProcessorName,
                                           processorName),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        var memoryInfo = GetMemoryInfo().Split('|');
        if (memoryInfo.Length >= 3)
        {
            operations.Enqueue(new PrintTextOperation(
                                   string.Format(PerformanceMeterReportResources.MemoryDetailedInfo,
                                               memoryInfo[0], memoryInfo[1], memoryInfo[2]),
                                   TextStyle.None));
        }
        else
        {
            operations.Enqueue(new PrintTextOperation(
                                   string.Format(PerformanceMeterReportResources.MemoryInfo,
                                               memoryInfo[0], memoryInfo.Length > 1 ? memoryInfo[1] : "Unknown"),
                                   TextStyle.None));
        }
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(PerformanceMeterReportResources.MachineName,
                                           Environment.MachineName),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(PerformanceMeterReportResources.RuntimeDescription,
                                           $".NET {Environment.Version} ({RuntimeInformation.FrameworkDescription})"),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(PerformanceMeterReportResources.RuntimeArchitecture,
                                           $"{RuntimeInformation.OSArchitecture} / {RuntimeInformation.ProcessArchitecture}"),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new LineBreakOperation());
    }

    private void AddTotalExecutionTime(Queue<IPdfOperation> operations)
    {
        if (_performanceMeter.TotalExecutionTimeMeasurer != null)
        {
            operations.Enqueue(new PrintTextOperation(
                                   PerformanceMeterReportResources.TotalExecutionTime,
                                   TextStyle.Bold));
            operations.Enqueue(new LineBreakOperation());

            var formattedTime = TimeSpan.FromMilliseconds(_performanceMeter.TotalExecutionTimeMeasurer.ExecutionTime).ToString(@"hh\:mm\:ss\.fff");
            operations.Enqueue(new PrintTextOperation(
                                   string.Format(PerformanceMeterReportResources.TotalExecutionTimeFormatted,
                                               formattedTime),
                                   TextStyle.None));
            operations.Enqueue(new LineBreakOperation());
            operations.Enqueue(new LineBreakOperation());
        }
    }

    private void AddGarbageCollectionInfo(Queue<IPdfOperation> operations)
    {
        operations.Enqueue(new PrintTextOperation(
                               PerformanceMeterReportResources.GarbageCollectionInfo,
                               TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(PerformanceMeterReportResources.TotalMemory,
                                           GCMeasurer.TotalMemory),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(PerformanceMeterReportResources.PauseTimePercentage,
                                           GCMeasurer.PauseTimePercentage),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(PerformanceMeterReportResources.Generation0Collections,
                                           GCMeasurer.GetCollectionCount(0)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(PerformanceMeterReportResources.Generation1Collections,
                                           GCMeasurer.GetCollectionCount(1)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(
                               string.Format(PerformanceMeterReportResources.Generation2Collections,
                                           GCMeasurer.GetCollectionCount(2)),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new LineBreakOperation());
    }

    private void AddDetailedPerformanceMetrics(Queue<IPdfOperation> operations)
    {
        if (_performanceMeter.ExecutionFrames.Count > 0)
        {
            operations.Enqueue(new PrintTextOperation(
                                   PerformanceMeterReportResources.DetailedPerformanceMetrics,
                                   TextStyle.Bold));
            operations.Enqueue(new LineBreakOperation());

            foreach (var executionFrame in _performanceMeter.ExecutionFrames)
            {
                operations.Enqueue(new PrintTextOperation(
                                       string.Format(PerformanceMeterReportResources.ExecutionFrameHeader,
                                                     executionFrame.Path),
                                       TextStyle.Italic));
                operations.Enqueue(new LineBreakOperation());

                operations.Enqueue(new PrintTextOperation(
                                       string.Format(PerformanceMeterReportResources.CallsCount,
                                                     executionFrame.CallsCount),
                                       TextStyle.None));
                operations.Enqueue(new LineBreakOperation());

                operations.Enqueue(new PrintTextOperation(
                                       string.Format(PerformanceMeterReportResources.MaxExecutionTime,
                                                     executionFrame.MaxExecutionTime),
                                       TextStyle.None));
                operations.Enqueue(new LineBreakOperation());

                operations.Enqueue(new PrintTextOperation(
                                       string.Format(PerformanceMeterReportResources.MinExecutionTime,
                                                     executionFrame.MinExecutionTime),
                                       TextStyle.None));
                operations.Enqueue(new LineBreakOperation());

                operations.Enqueue(new PrintTextOperation(
                                       string.Format(PerformanceMeterReportResources.MeanExecutionTime,
                                                     executionFrame.MeanExecutionTime),
                                       TextStyle.None));
                operations.Enqueue(new LineBreakOperation());

                operations.Enqueue(new PrintTextOperation(
                                       string.Format(PerformanceMeterReportResources.StdDevExecutionTime,
                                                     executionFrame.StdDevExecutionTime),
                                       TextStyle.None));
                operations.Enqueue(new LineBreakOperation());
                operations.Enqueue(new LineBreakOperation());
            }
        }
    }
}
