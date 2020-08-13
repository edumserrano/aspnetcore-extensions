﻿using System;

namespace DotNet.Sdk.Extensions.Testing.HttpMocking.WebHostBuilders.ResponseMocking
{
    internal class HttpResponseMessageMockDescriptorBuilderException : Exception
    {
        public HttpResponseMessageMockDescriptorBuilderException(string message)
            : base(message)
        {
        }

        public HttpResponseMessageMockDescriptorBuilderException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}