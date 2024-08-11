// Copyright (c) HOREICH GmbH, all rights reserved

using System;

namespace Horeich.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when the mapping from the database does
    /// not fit the telemetry sent from the device
    /// </summary>
    public class MappingMismatchException : Exception
    {
        public MappingMismatchException() : base()
        {
        }

        public MappingMismatchException(string message) : base(message)
        {
        }

        public MappingMismatchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
