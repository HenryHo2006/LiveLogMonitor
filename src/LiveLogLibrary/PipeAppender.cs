using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using log4net.Appender;
using log4net.Core;

namespace LiveLogMonitor
{
    /// <summary>
    /// Pipe Addpender for log item transfer
    /// </summary>
    internal class PipeAppender : AppenderSkeleton, IDisposable // BufferingAppenderSkeleton
    {
        // BufferingAppenderSkeleton cache some log without display !

        private const int BUFFER_LEN = 4096;

        private readonly PipeStream _PipeStream;
        //private readonly ManualResetEvent _ConnectBrokenEvent;
        private readonly ManualResetEventAsync _ConnectBrokenEvent;
        private readonly byte[] _buffer;
        private int _id;
        private readonly ProdConsTasks<LoggingEvent> _ProdConsTasks;

        public PipeAppender(PipeStream pipe_stream)
        {
            _PipeStream = pipe_stream;
            //_ConnectBrokenEvent = new ManualResetEvent(false);
            _ConnectBrokenEvent = new ManualResetEventAsync(false);
            _ProdConsTasks = new ProdConsTasks<LoggingEvent>("cached prcoess log thread")
            {
                Process = ProcessLog
            };
            _ProdConsTasks.Start();

            _buffer = new byte[BUFFER_LEN];
            _id = 0;
        }

        private void ProcessLog(LoggingEvent loggingEvent, int task_id, CancellationToken cancellation_token)
        {
            try
            {
                //string msg = loggingEvent.RenderedMessage;
                var log_item = new LogItem
                {
                    ID = _id++,
                    TimeStamp = loggingEvent.TimeStamp,
                    Level = convert(loggingEvent.Level),
                    Application = loggingEvent.Domain,
                    LoggerName = loggingEvent.LoggerName,
                    Message = loggingEvent.RenderedMessage
                };
                log_item.PutToStream(_PipeStream, _buffer);
            }
            catch (IOException) // pipeline connection broken
            {
                _ConnectBrokenEvent.Set();
                _ProdConsTasks.Abort();
            }
            catch(Exception ex)
            {
                // log系统自身异常，需要被诊断解决的
                try
                {
                    var log_item = new LogItem
                    {
                        ID = _id++,
                        TimeStamp = loggingEvent.TimeStamp,
                        Level = LogLevel.Error,
                        Application = "LiveLogLibrary.dll",
                        LoggerName = "PipeAppender",
                        Message = $"管道日志系统产生异常：{ex.Message}"
                    };
                    log_item.PutToStream(_PipeStream, _buffer);
                }
                catch
                {
                    // 还是出现问题再忽略
                }
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            _ProdConsTasks.AddMaterial(loggingEvent);
        }

        public async Task WaitConnectBrokenAsync(CancellationToken cancellation_token)
        {
            //await _ConnectBrokenEvent.AsTask();
            await _ConnectBrokenEvent.WaitAsync(cancellation_token);
        }

        private static LogLevel convert(Level level)
        {
            if (level == Level.Verbose || level == Level.Debug)
                return LogLevel.Debug;
            if (level == Level.Alert || level == Level.Warn)
                return LogLevel.Warning;
            if (level == Level.Error)
                return LogLevel.Error;
            if (level == Level.Critical || level == Level.Emergency || level == Level.Fatal)
                return LogLevel.Fatal;
            return LogLevel.Info;
        }

        #region Dispose Pattern

        private bool _disposed;

        /// <summary>
        /// Implement IDisposable. 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    _ProdConsTasks.Dispose();
                    // _PipeStream is just a reference, its lift cycle is not managed by PipeAppender
                    // _PipeStream.Dispose();
                }
                // Free your own state (unmanaged objects).
                // Set large fields to null.
                _disposed = true;
            }
        }

        #endregion

    }
}
