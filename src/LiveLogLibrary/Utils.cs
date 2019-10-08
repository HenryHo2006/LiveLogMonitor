using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using log4net;
using log4net.Core;

namespace LiveLogLibrary
{
    /// <summary>
    /// Utility class
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Create Pipe
        /// </summary>
        /// <param name="pipe_name"></param>
        /// <returns></returns>
        public static NamedPipeServerStream CreatePipe(string pipe_name = null)
        {
            var repository = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository(
                Assembly.GetExecutingAssembly());
            if (!repository.Configured)
                throw new Exception("Log4Net repository is not configured, maybe missed file log4net.config");

            if (string.IsNullOrEmpty(pipe_name))
                pipe_name = $"log_{Process.GetCurrentProcess().Id}";

            NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipe_name, PipeDirection.InOut,
                1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            return pipeServer;
        }

        /// <summary>
        /// Wait Connection and broken, loop forever
        /// </summary>
        /// <remarks>
        /// Application must call this fuction to live log monitor 
        /// </remarks>
        /// <param name="pipe_server"></param>
        /// <param name="cancellation_token"></param>
        /// <returns></returns>
        public static async Task WaitConnectAndBrokenAsync(NamedPipeServerStream pipe_server, CancellationToken cancellation_token)
        {
            var repository = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository(
                Assembly.GetExecutingAssembly());

            try
            {
                while (true)
                {
                    // Wait for a client to connect
                    await pipe_server.WaitForConnectionAsync(cancellation_token);

                    var pipe_appender = new PipeAppender(pipe_server) { Threshold = Level.All };
                    repository.Root.AddAppender(pipe_appender);

                    // Wait for client connect broken
                    await pipe_appender.WaitConnectBrokenAsync(cancellation_token);

                    pipe_appender.Threshold = Level.Off;
                    await Task.Delay(10, cancellation_token);   // clear exist log items
                    pipe_appender.Close();
                    repository.Root.RemoveAppender(pipe_appender);

                    pipe_server.Disconnect();
                }
            }
            // Catch the IOException that is raised if the pipe is broken or disconnected.
            //catch (IOException ex)
            //{
            //    Console.WriteLine("ERROR: {0}", ex.Message);
            //}
            catch (OperationCanceledException)
            {
            }
            finally
            {
                pipe_server.Close();
            }

        }
    }
}