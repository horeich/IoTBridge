// Copyright (c) HOREICH GmbH, All rights reserved

using System;
using System.Threading;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Horeich.Services.Runtime;
using Microsoft.ApplicationInsights.NLogTarget;
using NLog.Config;
using NLog;
using NLog.Web;

namespace Horeich.Services.Diagnostics
{
    public interface ILogger
    {
        // The following 4 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Error(Exception exception);
        LogLevel LogLevel { get; set; }
        String ApplicationName { get; set; }

        // The following 4 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)
    }

    public class Logger : ILogger
    { 
        private readonly NLog.Logger _logger;
        private string _processId;
        private readonly string _name;
        public LogLevel LogLevel { get; set; }
        public String ApplicationName { get; set; }

        public Logger(string processId, string name, LogLevel logLevel)
        {
            this._processId = processId;
            this._name = name;
            this.LogLevel = logLevel;
            ApplicationName = "";

            _logger = NLog.LogManager.Setup()
                .LoadConfigurationFromFile(GetLogConfigFileName(), false) // give NLog the right file to load for the environment
                .GetCurrentClassLogger();

            ReadOnlyCollection<NLog.Targets.Target> targets = NLog.LogManager.Configuration.AllTargets;
            foreach (var target in targets)
            {
                Info($"Logging to {target}");
            }
            _logger.Factory.Flush(TimeSpan.FromSeconds(1000));
        }

        public static string GetLogConfigFileName()
        {
            // Check the environment and load the file accordingly
            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            
            // Set default
            if (String.IsNullOrEmpty(env))
            {
                return "nlog.development.config";
            }
            if (env.Equals("Release", System.StringComparison.InvariantCultureIgnoreCase))
            {
                return "nlog.release.config";
            }
            else if (env.Equals(value: "Staging", System.StringComparison.InvariantCultureIgnoreCase))
            {
                return "nlog.debug.config";
            }
            else // fall back to debug settings by default
            {
                return "nlog.development.config";
            }
        }

        // The following 4 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)
        public void Debug(string message)
        {
            if (this.LogLevel > LogLevel.Debug) return;
            Log(message, LogLevel.Debug, null);
        }

        public void Info(string message)
        {
            if (this.LogLevel > LogLevel.Info) return;
            Log(message, LogLevel.Info, null);
        }

        public void Warn(string message)
        {
            if (this.LogLevel > LogLevel.Warn) return;
            Log(message, LogLevel.Warn, null);
        }

        public void Error(string message)
        {
            if (this.LogLevel > LogLevel.Error) return;
            Log(message, LogLevel.Error, null);
        }

        public void Error(Exception exception)
        {
            Log("", LogLevel.Error, exception);
        }

        private void Log(string message, LogLevel logLevel, Exception exception)
        {
            var logEventInfo = new LogEventInfo(GetLogLevel(logLevel), _name, message); //, $"{formatter(state, exception)}");
            if (exception != null)
            {
                logEventInfo.Exception = exception;
            }

            _logger
                // These properties are predefined in nlog.config
                // .WithProperty("UserId", userContext.Id)
                // .WithProperty("Time", DateTime.Now)
                .WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                .WithProperty("ThreadId", Thread.CurrentThread.ManagedThreadId)
                .WithProperty("ProcessId", ApplicationName + "." + _processId)
                // .WithProperty("Channel", userContext.AuthenticatedUserId) 
                // .WithProperty("UserAgent", requestContext.UserAgent)
                // .WithProperty("CorrelationId", requestContext.CorrelationId)
                // .WithProperty("EventId", eventId.Id)
                .Log(logEventInfo);
        }

        internal NLog.LogLevel GetLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return NLog.LogLevel.Trace;
                case LogLevel.Debug:
                    return NLog.LogLevel.Debug;
                case LogLevel.Info:
                    return NLog.LogLevel.Info;
                case LogLevel.Warn:
                    return NLog.LogLevel.Warn;
                case LogLevel.Error:
                    return NLog.LogLevel.Error;
                case LogLevel.Critical:
                    return NLog.LogLevel.Fatal;
            }
            return NLog.LogLevel.Info;
        }
    }
}
