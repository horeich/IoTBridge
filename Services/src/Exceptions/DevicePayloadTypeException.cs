// Copyright (c) Horeich UG (andreas.reichle@horeich.de)

using System;

namespace Horeich.SensingSolutions.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when the device payload is invalid
    /// </summary>
    public class DevicePayloadTypeException : Exception
    {
        public DevicePayloadTypeException() : base()
        {
        }

        public DevicePayloadTypeException(string message) : base(message)
        {
        }

        public DevicePayloadTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
