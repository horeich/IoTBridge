// Copyright (c) HOREICH GmbH, all rights reserved

using System;

namespace Horeich.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when a client is requesting a resource that
    /// doesn't exist yet.
    /// </summary>
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException() : base()
        {
        }

        public ResourceNotFoundException(string message) : base(message)
        {
        }

        public ResourceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
