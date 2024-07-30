// Copyright (c) HOREICH GmbH, all rights reserved

using System;

namespace Horeich.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when a client attempts to create a resource
    /// which would conflict with an existing one, for instance using the same
    /// identifier. The client should change the identifier or assume the
    /// resource has already been created.
    /// </summary>
    public class StorageAdapterException : Exception
    {
        public StorageAdapterException() : base()
        {
        }

        public StorageAdapterException(string message) : base(message)
        {
        }

        public StorageAdapterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
