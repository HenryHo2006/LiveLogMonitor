using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using log4net;
using log4net.Config;

using LiveLogMonitor;

namespace sample_netcore
{
    internal class Program
    {
        private static ILog Log = LogManager.GetLogger(typeof(Program));

        static async Task Main(string[] args)
        {
            var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));
            if (!repository.Configured)
                throw new Exception("Log4Net repository is not configured, maybe missed file log4net.config");

            var exit = new CancellationTokenSource();
            var pipe = Utils.CreatePipe();
            var task = Utils.WaitConnectAndBrokenAsync(pipe, exit.Token);

            Random rand = new Random();
            long counter = 0;
            long last_counter = 0;
            Stopwatch sw = Stopwatch.StartNew();
            TimeSpan ts_last = TimeSpan.Zero;
            while (true)
            {
                Log.Debug("jls:DC100372A TOP 外　层 lot:200111A sn:1已成功保存到数据库");
                continue;
                //var msg = Console.ReadLine();
                //if (msg == "break") break;
                var i = rand.Next();
                int level = i % 5;
                string str = $"random number: {i}";
                switch (level)
                {
                    case 0:
                        Log.Debug(str);
                        break;
                    case 1:
                        Log.Info(str);
                        break;
                    case 2:
                        Log.Warn(str);
                        break;
                    case 3:
                        Log.Error(str);
                        break;
                    case 4:
                        Log.Fatal(str);
                        break;
                }
                counter++;

                if ((sw.Elapsed - ts_last) > new TimeSpan(0, 0, 1))
                {
                    Console.WriteLine($"do {counter - last_counter:###,###} loops, you can start run LiveLogMonitor");
                    last_counter = counter;
                    ts_last = sw.Elapsed;
                }

                //Thread.Sleep(1000);
                if (i == int.MaxValue) break;
            }

            exit.Cancel();
            await task;
        }
    }
}
