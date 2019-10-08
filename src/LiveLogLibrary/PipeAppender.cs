using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using log4net.Appender;
using log4net.Core;

namespace LiveLogLibrary
{
    /// <summary>
    /// Pipe Addpender for log item transfer
    /// </summary>
    public class PipeAppender : AppenderSkeleton // BufferingAppenderSkeleton
    {
        // BufferingAppenderSkeleton cache some log without display !

        private const int BUFFER_LEN = 4096;

        private readonly PipeStream _PipeStream;
        //private readonly ManualResetEvent _ConnectBrokenEvent;
        private readonly ManualResetEventAsync _ConnectBrokenEvent;
        private readonly byte[] _buffer;
        private int _id;

        public PipeAppender(PipeStream pipe_stream)
        {
            _PipeStream = pipe_stream;
            //_ConnectBrokenEvent = new ManualResetEvent(false);
            _ConnectBrokenEvent = new ManualResetEventAsync(false);
            _buffer = new byte[BUFFER_LEN];
            _id = 0;
        }

        protected override void Append(LoggingEvent loggingEvent)
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
            }
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
    }
}
