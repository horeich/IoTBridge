// Copyright (c) Horeich UG (andreas.reichle@horeich.de)

using System;

namespace Horeich.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when the device identity is not recognized
    /// </summary>
    public class DeviceIdentityException : Exception
    {
        public DeviceIdentityException() : base()
        {
        }

        public DeviceIdentityException(string message) : base(message)
        {
        }

        public DeviceIdentityException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
