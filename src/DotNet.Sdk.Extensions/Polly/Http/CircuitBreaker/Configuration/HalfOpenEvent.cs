﻿namespace DotNet.Sdk.Extensions.Polly.Http.CircuitBreaker.Configuration
{
    public class HalfOpenEvent
    {
        internal HalfOpenEvent(string httpClientName, CircuitBreakerOptions circuitBreakerOptions)
        {
            HttpClientName = httpClientName;
            CircuitBreakerOptions = circuitBreakerOptions;
        }

        public string HttpClientName { get; }

        public CircuitBreakerOptions CircuitBreakerOptions { get; }
    }
}