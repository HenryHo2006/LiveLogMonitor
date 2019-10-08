﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using LiveLogLibrary;

namespace LiveLogMonitor
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            string host = null, named_pipe = null;
            if (args.Length == 0)
            {
                host = ".";
                named_pipe = EnumerateLocalNamedPipes();
            }
            else
            {
                string[] strs = args[0].Split('/', '\\', ':');
                if (strs.Length == 2)
                {
                    host = strs[0];
                    named_pipe = strs[1];
                }
            }

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(named_pipe))
            {
                printUsage();
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(0);
            }

            Console.Title = $@"Living Log Monitor 【host:{host} pipe:{named_pipe}】";
            Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                NativeMethods.ShowWindow(Process.GetCurrentProcess().MainWindowHandle, ShowWindowMode.SW_MAXIMIZE);

            var pipeClient = new NamedPipeClientStream(host, named_pipe, PipeDirection.InOut);
            Console.Write($@"Connecting to host:{host} pipe:{named_pipe}...");
            await pipeClient.ConnectAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success");
            Console.ResetColor();

            byte[] buffer = new byte[4096];
            while (true)
            {
                try
                {
                    if (!pipeClient.IsConnected) throw new Exception("pipe is broken");

                    var log = ReadFromStream(pipeClient, buffer);
                    if (log.Level == LogLevel.Warning)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else if (log.Level == LogLevel.Error || log.Level == LogLevel.Fatal)
                        Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{log.ID,-8:0} [{log.TimeStamp:yy-MM-dd hh:mm:ss.fff}] {log.Level,8} {log.Message}");
                    if (log.Level >= LogLevel.Warning)
                        Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                    Environment.Exit(-1);
                    return;
                }
            }
        }

        private static void printUsage()
        {
            Console.WriteLine("not found any named pipe start with log_*");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("LiveLogMonitor [host/named_pipe]");
            Console.WriteLine("without parameters, the tool will enumerate local host named pipes, connect first of which name start of log***)");
            Console.WriteLine();
        }

        private static string EnumerateLocalNamedPipes()
        {
            // enumerate local named pipes:
            // powershell command : [System.IO.Directory]::GetFiles("\\.\\pipe\\")
            var listOfPipes = Directory.GetFiles(@"\\.\pipe\");
            var first = listOfPipes.FirstOrDefault(pipe => pipe.ToLower().StartsWith(@"\\.\pipe\log_"));
            if (first == null) return null;
            return first.Substring(9);
        }

        private static LogItem ReadFromStream(PipeStream stream, Span<byte> buffer)
        {
            stream.Read(buffer);
            int total_len = buffer[0] * 256 + buffer[1];
            int app_len = buffer[2];
            int log_name_len = buffer[3];
            int msg_len = total_len - app_len - log_name_len - (4 + 8 + 1 + 4);

            return new LogItem
            {
                ID = BitConverter.ToInt32(buffer.Slice(4, 4)),
                TimeStamp = DateTimeOffset.FromFileTime(BitConverter.ToInt64(buffer.Slice(8, 8))),
                Level = (LogLevel)buffer[16],
                Application = Encoding.Unicode.GetString(buffer.Slice(17, app_len)),
                LoggerName = Encoding.Unicode.GetString(buffer.Slice(17 + app_len, log_name_len)),
                Message = Encoding.Unicode.GetString(buffer.Slice(17 + app_len + log_name_len, msg_len))
            };
        }

    }

    /// <summary>
    /// 窗口模式
    /// </summary>
    public enum ShowWindowMode
    {
        /// <summary>
        /// 隐藏
        /// </summary>
        SW_HIDE = 0,
        /// <summary>
        /// 最大化
        /// </summary>
        SW_MAXIMIZE = 3,
        /// <summary>
        /// 最小化
        /// </summary>
        SW_MINIMIZE = 6,
        /// <summary>
        /// 恢复
        /// </summary>
        SW_RESTORE = 9
    }

    /// <summary>
    /// Win32 API
    /// </summary>
    public static class NativeMethods
    {
        /// <summary>
        /// 设置窗口模式
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="cmdShow"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowMode cmdShow);
    }

}
