using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using log4net;

using LiveLogLibrary;

namespace sample_netframework
{
    static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Log.Info("Application sample_netframework Start");  // initial log4net, do not remove

            var pipe = CreatePipeWithSecurity(); //Utils.CreatePipe();
            var exit = new CancellationTokenSource();
            var task = Utils.WaitConnectAndBrokenAsync(pipe, exit.Token);

            Application.Run(new Form1());

            try
            {
                exit.Cancel();
                await task;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Create Pipe
        /// </summary>
        /// <param name="pipe_name"></param>
        /// <returns></returns>
        public static NamedPipeServerStream CreatePipeWithSecurity(string pipe_name = null)
        {
            const int PIPE_SIZE = 4096;

            var repository = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository(
                Assembly.GetExecutingAssembly());
            if (!repository.Configured)
                throw new Exception("Log4Net repository is not configured, maybe missed file log4net.config");

            if (string.IsNullOrEmpty(pipe_name))
                pipe_name = $"log_{Process.GetCurrentProcess().Id}";

            PipeSecurity pipeSa = new PipeSecurity();
            pipeSa.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                PipeAccessRights.ReadWrite, AccessControlType.Allow));

            NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipe_name, PipeDirection.InOut,
                1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, PIPE_SIZE, PIPE_SIZE, pipeSa);

            return pipeServer;
        }

    }
}
