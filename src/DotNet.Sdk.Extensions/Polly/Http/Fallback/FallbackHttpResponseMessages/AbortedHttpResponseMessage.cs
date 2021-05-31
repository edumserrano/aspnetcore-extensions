﻿using System;
using System.Net;
using System.Net.Http;

namespace DotNet.Sdk.Extensions.Polly.Http.Fallback.FallbackHttpResponseMessages
{
    public class AbortedHttpResponseMessage : HttpResponseMessage
    {
        public AbortedHttpResponseMessage(Exception exception)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}