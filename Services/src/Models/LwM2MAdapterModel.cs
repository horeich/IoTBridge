
// Copyright (c) Horeich UG

using System;
using Microsoft.Azure.DigitalTwins.Parser;

namespace Horeich.Services.Models
{
    public class LwM2MAdapterModel
    {
        public String Identity { get; set; }
        public String LwM2MObject { get; set; }
        public DTEntityKind DataType { get; set; }

    }
}