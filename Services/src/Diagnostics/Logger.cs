// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Reflection;

using Horeich.SensingSolutions.Services.Runtime;
using Microsoft.ApplicationInsights.NLogTarget;
using NLog.Config;


namespace Horeich.SensingSolutions.Services.Diagnostics
{
    public interface ILogger
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

    public class Logger : ILogger
    {
        // private readonly string _processId;
        // private readonly LogLevel _defaultLogLevel;
        // private readonly LogLevel _remoteLogLevel;
        // private readonly NLog.Logger _insightsLog; 
        private readonly ILocalLogger _localLogger;
        private readonly IInsightsLogger _insightLogger;
        public Logger(string processId, ILogConfig logConfig)
        {
            _insightLogger = new InsightsLogger(processId, logConfig.RemoteLogLevel, logConfig.InstrumentationKey);
            _localLogger = new LocalLogger(processId, logConfig.DefaultLogLevel);
        }

        // The following 4 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)
        public void Debug(string message, Action context)
        {
            _localLogger.Debug(message, context);
            _insightLogger.Debug(message, context);
        }

        public void Info(string message, Action context)
        {
            _localLogger.Info(message, context);
            _insightLogger.Info(message, context);
        }

        public void Warn(string message, Action context)
        {
            _localLogger.Warn(message, context);
            _insightLogger.Warn(message, context);
        }

        public void Error(string message, Action context)
        {
            _localLogger.Error(message, context);
            _insightLogger.Error(message, context);
        }

        // The following 4 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)
        public void Debug(string message, Func<object> context)
        {
            _localLogger.Debug(message, context);
            _insightLogger.Debug(message, context);
        }

        public void Info(string message, Func<object> context)
        {
            _localLogger.Info(message, context);
            _insightLogger.Info(message, context);
        }

        public void Warn(string message, Func<object> context)
        {
            _localLogger.Warn(message, context);
            _insightLogger.Warn(message, context);
        }

        public void Error(string message, Func<object> context)
        {
            _localLogger.Error(message, context);
            _insightLogger.Error(message, context);
        }

        // /// <summary>
        // /// Log the message and information about the context, cleaning up
        // /// and shortening the class name and method name (e.g. removing
        // /// symbols specific to .NET internal implementation)
        // /// </summary>
        // private void Write(string level, MethodInfo context, string text)
        // {
        //     // Extract the Class Name from the context
        //     var classname = "";
        //     if (context.DeclaringType != null)
        //     {
        //         classname = context.DeclaringType.FullName;
        //     }
        //     classname = classname.Split(new[] { '+' }, 2).First();
        //     classname = classname.Split('.').LastOrDefault();

        //     // Extract the Method Name from the context
        //     var methodname = context.Name;
        //     methodname = methodname.Split(new[] { '>' }, 2).First();
        //     methodname = methodname.Split(new[] { '<' }, 2).Last();

        //     var time = DateTimeOffset.UtcNow.ToString("u");
        //     Console.WriteLine($"[{this.processId}][{time}][{level}][{classname}:{methodname}] {text}");
        // }

        // private void WriteToJson(string level, MethodInfo context, string text)
        // {
            
        // }
    }
}
