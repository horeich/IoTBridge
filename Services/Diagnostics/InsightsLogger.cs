// Copyright (c) Horeich UG. All rights reserved.

using System;
using System.Linq;
using System.Reflection;

using Microsoft.ApplicationInsights.NLogTarget;
using NLog.Config;


namespace Horeich.SensingSolutions.Services.Diagnostics
{
    public interface IInsightsLogger
    {
        // The following 4 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)
        void Debug(string message, Action context);
        void Info(string message, Action context);
        void Warn(string message, Action context);
        void Error(string message, Action context);

        // The following 4 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)
         void Debug(string message, Func<object> context);
         void Info(string message, Func<object> context);
         void Warn(string message, Func<object> context);
         void Error(string message, Func<object> context);
    }

    public class InsightsLogger : IInsightsLogger
    {
        private readonly string _processId;
        private readonly LogLevel _loggingLevel;
        private readonly NLog.Logger _logger;
        public InsightsLogger(string processId, LogLevel logLevel, string instrumentationKey)
        {
            _processId = processId;
            _loggingLevel = logLevel;
            NLog.LogLevel nlogLevel = ConvertToNLogLevel(logLevel);

            var nLogConfig = new LoggingConfiguration();
            ApplicationInsightsTarget logTarget = new ApplicationInsightsTarget();
            logTarget.InstrumentationKey = instrumentationKey;

            // Initialize with highest logging level
            LoggingRule rule = new LoggingRule("*", nlogLevel, logTarget);
            nLogConfig.LoggingRules.Add(rule);

            NLog.LogManager.Configuration = nLogConfig;
            _logger = NLog.LogManager.GetLogger("CoAPServer");
        }

        private NLog.LogLevel ConvertToNLogLevel(LogLevel level)
        {
            NLog.LogLevel nLogLevel;
            switch (level)
            {
            case LogLevel.Debug:
                nLogLevel = NLog.LogLevel.Debug;
                break;
            case LogLevel.Info:
                nLogLevel = NLog.LogLevel.Info;
                break;
            case LogLevel.Warn:
                nLogLevel = NLog.LogLevel.Warn;
                break;
            case LogLevel.Error:
                nLogLevel = NLog.LogLevel.Error;
                break;
            case LogLevel.None:
                nLogLevel = NLog.LogLevel.Off;
                break;
            default:
                nLogLevel = NLog.LogLevel.Debug;
                break;
            }
            return nLogLevel;
        }

      // The following 4 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)
        public void Debug(string message, Action context)
        {
            if (_loggingLevel > LogLevel.Debug) return;
            this.Write(NLog.LogLevel.Debug, context.GetMethodInfo(), message);
        }

        public void Info(string message, Action context)
        {
            if (_loggingLevel > LogLevel.Info) return;
            this.Write(NLog.LogLevel.Info, context.GetMethodInfo(), message);
        }

        public void Warn(string message, Action context)
        {
            if (_loggingLevel > LogLevel.Warn) return;
            this.Write(NLog.LogLevel.Warn, context.GetMethodInfo(), message);
        }

        public void Error(string message, Action context)
        {
            if (_loggingLevel > LogLevel.Error) return;
            this.Write(NLog.LogLevel.Error, context.GetMethodInfo(), message);
        }

        // The following 4 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)
        public void Debug(string message, Func<object> context)
        {
            if (_loggingLevel > LogLevel.Debug) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write(NLog.LogLevel.Debug, context.GetMethodInfo(), message);
        }

        public void Info(string message, Func<object> context)
        {
            if (_loggingLevel > LogLevel.Info) return;
           
            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write(NLog.LogLevel.Info, context.GetMethodInfo(), message);
        }

        public void Warn(string message, Func<object> context)
        {
            if (_loggingLevel > LogLevel.Warn) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write(NLog.LogLevel.Warn, context.GetMethodInfo(), message);
        }

        public void Error(string message, Func<object> context)
        {
            if (_loggingLevel > LogLevel.Error) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(context.Invoke());

            this.Write(NLog.LogLevel.Error, context.GetMethodInfo(), message);
        }

        /// <summary>
        /// Log the message and information about the context, cleaning up
        /// and shortening the class name and method name (e.g. removing
        /// symbols specific to .NET internal implementation)
        /// </summary>
        private void Write(NLog.LogLevel level, MethodInfo context, string text)
        {
            // Extract the Class Name from the context
            var classname = "";
            if (context.DeclaringType != null)
            {
                classname = context.DeclaringType.FullName;
            }
            classname = classname.Split(new[] { '+' }, 2).First();
            classname = classname.Split('.').LastOrDefault();

            // Extract the Method Name from the context
            var methodname = context.Name;
            methodname = methodname.Split(new[] { '>' }, 2).First();
            methodname = methodname.Split(new[] { '<' }, 2).Last();

            var time = DateTimeOffset.UtcNow.ToString("u");
            _logger.Log(level, $"[{_processId}][{time}][{level}][{classname}:{methodname}] {text}");
        }
    }
}
