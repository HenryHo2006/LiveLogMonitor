using System;
using System.IO.Pipes;

using System.Text;

namespace LiveLogMonitor
{
    // Log Level
    internal enum LogLevel { Debug, Info, Warning, Error, Fatal }

    // Log Item
    // No expcetion filed，we recommend use Log.Error(exception_object) to convert exception object to stack trace info.
    internal struct LogItem
    {
        public int ID { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public LogLevel Level { get; set; }
        public string Application { get; set; }
        public string LoggerName { get; set; }
        public string Message { get; set; }

        // .net standard 2.1
        //internal void PutToStream(PipeStream stream, Span<byte> buffer)
        //{
        //    // encode: total_len(2byte), application len(1byte), logger name(1byte), field sequence...
        //    BitConverter.TryWriteBytes(buffer.Slice(4), ID);
        //    BitConverter.TryWriteBytes(buffer.Slice(8), TimeStamp.ToUnixTimeMilliseconds());
        //    buffer[16] = (byte)Level;
        //    int app_len = Encoding.Unicode.GetBytes(Application, buffer.Slice(17));
        //    int log_name_len = Encoding.Unicode.GetBytes(LoggerName, buffer.Slice(17 + app_len));
        //    int msg_len = Encoding.Unicode.GetBytes(Message, buffer.Slice(17 + app_len + log_name_len));

        //    int total_len = 17 + app_len + log_name_len + msg_len;
        //    if (total_len > ushort.MaxValue) throw new Exception($"log message large than {ushort.MaxValue} bytes");
        //    ushort u_len = (ushort)total_len;
        //    buffer[0] = (byte)(u_len / 255);
        //    buffer[1] = (byte)(u_len & 255);
        //    buffer[2] = (byte)app_len;
        //    buffer[3] = (byte)log_name_len;

        //    stream.Write(buffer.Slice(0, total_len));
        //}
        // .net standard 2.0
        internal unsafe void PutToStream(PipeStream stream, byte[] buffer)
        {
            fixed (byte* pBuf = buffer)
            {
                fixed (char* pApp = Application, pLogName = LoggerName, pMsg = Message)
                {
                    // encode: total_len(2byte), application len(1byte), logger name(1byte), field sequence...
                    //BitConverter.TryWriteBytes(buffer.Slice(4), ID);
                    *(int*)(pBuf + 4) = ID;
                    //BitConverter.TryWriteBytes(buffer.Slice(8), TimeStamp.ToUnixTimeMilliseconds());
                    *(long*)(pBuf + 8) = TimeStamp.ToUnixTimeMilliseconds();
                    buffer[16] = (byte)Level;
                    int app_len = Encoding.Unicode.GetBytes(pApp, Application.Length, pBuf + 17, buffer.Length - 17);
                    int log_name_len = Encoding.Unicode.GetBytes(pLogName, LoggerName.Length, pBuf + 17 + app_len, buffer.Length - 17 - app_len);
                    int msg_len = Encoding.Unicode.GetBytes(pMsg, Message.Length, pBuf + 17 + app_len + log_name_len, buffer.Length - 17 - app_len - log_name_len);

                    int total_len = 17 + app_len + log_name_len + msg_len;
                    if (total_len > ushort.MaxValue)
                        throw new Exception($"log message large than {ushort.MaxValue} bytes");
                    ushort u_len = (ushort)total_len;
                    buffer[0] = (byte)(u_len / 256);
                    buffer[1] = (byte)(u_len & 255);
                    buffer[2] = (byte)app_len;
                    buffer[3] = (byte)log_name_len;

                    stream.Write(buffer, 0, total_len);
                }
            }
        }

        // .net standard 2.1
        //public static LogItem ReadFromStream(PipeStream stream, Span<byte> buffer)
        //{
        //    stream.Read(buffer);
        //    int total_len = buffer[0] * 256 + buffer[1];
        //    int app_len = buffer[2];
        //    int log_name_len = buffer[3];
        //    int msg_len = total_len - app_len - log_name_len - (4 + 8 + 1 + 4);

        //    return new LogItem
        //    {
        //        ID = BitConverter.ToInt32(buffer.Slice(4, 4)),
        //        TimeStamp = DateTimeOffset.FromFileTime(BitConverter.ToInt64(buffer.Slice(8, 8))),
        //        Level = (LogLevel)buffer[16],
        //        Application = Encoding.Unicode.GetString(buffer.Slice(17, app_len)),
        //        LoggerName = Encoding.Unicode.GetString(buffer.Slice(17 + app_len, log_name_len)),
        //        Message = Encoding.Unicode.GetString(buffer.Slice(17 + app_len + log_name_len, msg_len))
        //    };
        //}
    }

}
