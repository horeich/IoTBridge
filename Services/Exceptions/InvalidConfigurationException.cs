// Copyright (c) HOREICH GmbH, all rights reserved

using System;

namespace Horeich.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when the service is configured incorrectly.
    /// In order to recover, the service owner should fix the configuration
    /// and re-deploy the service.
    /// </summary>
    public class InvalidConfigurationException : Exception
    {
        public InvalidConfigurationException() : base()
        {
        }

        public InvalidConfigurationException(string message) : base(message)
        {
        }

        public InvalidConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
