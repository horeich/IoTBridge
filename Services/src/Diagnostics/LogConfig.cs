// Copyright (c) Horeich UG (andreas.reichle@horeich.com)

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Horeich.SensingSolutions.Services.Diagnostics
{
    public enum LogLevel
    {
        Debug = 10,
        Info = 20,
        Warn = 30,
        Error = 40,
        None = 60,
    }
    public interface ILogConfig
    {  
        LogLevel DefaultLogLevel { get; }
        LogLevel RemoteLogLevel { get; }
        string InstrumentationKey { get; }
    }

    public class LogConfig : ILogConfig
    {
        public LogLevel DefaultLogLevel { get; set; }
        public LogLevel RemoteLogLevel { get; set; }
        public string InstrumentationKey { get; set; }
    }
}
