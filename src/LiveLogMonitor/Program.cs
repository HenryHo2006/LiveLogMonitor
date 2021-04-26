using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using CommandLine;

namespace LiveLogMonitor
{
    internal static class Program
    {
        // Log Level
        private enum LogLevel { Debug, Info, Warning, Error, Fatal }

        // Log Item (duplicate define to remove dependency)
        private struct LogItem
        {
            public int ID { get; set; }
            public DateTimeOffset TimeStamp { get; set; }
            public LogLevel Level { get; set; }
            public string Application { get; set; }
            public string LoggerName { get; set; }
            public string Message { get; set; }
        }

        private class Options
        {
            [Option('m', "max", Required = false, Default = false, HelpText = "Set screen size to max.")]
            public bool Max { get; set; }
            [Option('t', "top", Required = false, Default = true, HelpText = "Always on top.")]
            public bool Top { get; set; }
            [Value(0, MetaName = "pipename", Required = false, HelpText = "Pipe name [host\\port]")]
            public string PipeName { get; set; }
        }

        private static async Task Main(string[] args)
        {
            string host = ".", named_pipe = null;

            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                if (!string.IsNullOrEmpty(o.PipeName))
                {
                    string[] strs = o.PipeName.Split('/', '\\', ':');
                    if (strs.Length == 2)
                    {
                        host = strs[0];
                        named_pipe = strs[1];
                    }
                }

                if (o.Top)
                {
                    IntPtr hWnd = NativeMethods.GetConsoleWindow();
                    if (hWnd != IntPtr.Zero)
                        NativeMethods.SetWindowPos(hWnd, new IntPtr(NativeMethods.HWND_TOPMOST), 0, 0, 0, 0,
                            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE);
                }

                if (o.Max)
                {
                    Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        NativeMethods.ShowWindow(Process.GetCurrentProcess().MainWindowHandle, ShowWindowMode.SW_MAXIMIZE);
                }
            }).WithNotParsed(errs =>
            {
                //Console.WriteLine(errs);
                Console.WriteLine("without pipename, the tool will enumerate local host named pipes, connect first of which name start of log***)");
                Environment.Exit(-1);
            });

            if(string.IsNullOrEmpty(named_pipe))
                named_pipe = EnumerateLocalNamedPipes();

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(named_pipe))
            {
                Console.WriteLine("not found any named pipe start with log_*");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(0);
            }

            Console.OutputEncoding = Encoding.Unicode;
            Console.Title = $@"Living Log Monitor 【host:{host} pipe:{named_pipe}】";

            var pipeClient = new NamedPipeClientStream(host, named_pipe, PipeDirection.InOut);
            Console.WriteLine("If you disconnect and then connect again, you need to wait for the first log");
            Console.Write($@"Connecting to host:{host} pipe:{named_pipe} ...");
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
                    if (log.ID == 0 && log.TimeStamp == DateTimeOffset.MinValue)
                        continue;
                    if (log.Level == LogLevel.Warning)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else if (log.Level == LogLevel.Error || log.Level == LogLevel.Fatal)
                        Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{log.ID,-6:0} [{log.TimeStamp.ToLocalTime():yy-MM-dd HH:mm:ss.fff}] {log.Level,7} {log.Message}");
                    if (log.Level >= LogLevel.Warning)
                        Console.ResetColor();
                }
                catch (Exception ex)
                {
                    pipeClient.Close();

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

        //private static void printUsage()
        //{
        //    Console.WriteLine("not found any named pipe start with log_*");
        //    Console.WriteLine();
        //    Console.WriteLine("Usage:");
        //    Console.WriteLine();
        //    Console.WriteLine("LiveLogMonitor [host/named_pipe] [top:[on/off]]");
        //    Console.WriteLine("without parameters, the tool will enumerate local host named pipes, connect first of which name start of log***)");
        //    Console.WriteLine();
        //}

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
            int bytes = stream.Read(buffer);
            if (bytes == 0)     // monitor app exit / crash
                return new LogItem();

            int total_len = buffer[0] * 256 + buffer[1];
            int app_len = buffer[2];
            int log_name_len = buffer[3];
            int msg_len = total_len - app_len - log_name_len - (4 + 8 + 1 + 4);

            return new LogItem
            {
                ID = BitConverter.ToInt32(buffer.Slice(4, 4)),
                TimeStamp = DateTimeOffset.FromUnixTimeMilliseconds(BitConverter.ToInt64(buffer.Slice(8, 8))),
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

        /// <summary>
        /// GetConsoleWindow
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        /// <summary>
        /// SetWindowPos
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="hWndInsertAfter"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="uFlags"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            int uFlags);

        public const int HWND_TOPMOST = -1;
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOSIZE = 0x0001;

    }

}
