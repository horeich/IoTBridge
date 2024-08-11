// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Horeich.Services.Runtime
{
    /// <summary>Simple helper capturing uptime information</summary>
    public class DeviceUptime
    {
        /// <summary>When the service started</summary>
        public DateTime _start = DateTime.UtcNow;
        public void Reset()
        {
            _start = DateTime.UtcNow;
        }
        /// <summary>How long the service has been running</summary>
        public TimeSpan OnlineInterval => DateTime.UtcNow.Subtract(_start);

        /// <summary>A randomly generated ID used to identify the process in the logs</summary>
        public string ProcessId { get; } = "WebService." + Guid.NewGuid();
    }
}