// Copyright (c) HOREICH GmbH, all rights reserved

namespace Horeich.Services.Diagnostics
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 10,
        Info = 20,
        Warn = 30,
        Error = 40,
        Critical = 60,
        None = 80,
    }
    
    public interface ILogConfig
    {  
        LogLevel LogLevel { get; }
        LogLevel RemoteLogLevel { get; }
        string InstrumentationKey { get; }
        int EventId { get; }
    }

    public class LogConfig : ILogConfig
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Trace;
        public LogLevel RemoteLogLevel { get; set; }
        public string InstrumentationKey { get; set; }
        public int EventId { get; set; } = 0;
    }
}
