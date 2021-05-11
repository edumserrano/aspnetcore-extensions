﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DotNet.Sdk.Extensions.Polly.Http.CircuitBreaker;
using DotNet.Sdk.Extensions.Polly.Http.Fallback.FallbackHttpResponseMessages;
using DotNet.Sdk.Extensions.Polly.Policies;
using DotNet.Sdk.Extensions.Testing.HttpMocking.HttpMessageHandlers;
using DotNet.Sdk.Extensions.Tests.Polly.Http.Auxiliary;
using Polly.CircuitBreaker;
using Shouldly;

namespace DotNet.Sdk.Extensions.Tests.Polly.Http.CircuitBreaker.Auxiliary
{
    public class CircuitBreakerPolicyExecutor : IAsyncDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly CircuitBreakerOptions _circuitBreakerOptions;
        private readonly TestHttpMessageHandler _testHttpMessageHandler;
        private readonly string _resetRequestPath;

        public CircuitBreakerPolicyExecutor(
            HttpClient httpClient,
            CircuitBreakerOptions circuitBreakerOptions,
            TestHttpMessageHandler testHttpMessageHandler)
        {
            _httpClient = httpClient;
            _circuitBreakerOptions = circuitBreakerOptions;
            _testHttpMessageHandler = testHttpMessageHandler;
            _resetRequestPath = HandleResetRequest();
        }
        
        public async Task TriggerFromExceptionAsync(Exception exception)
        {
            var requestPath = $"/circuit-breaker/exception/{exception.GetType().Name}";
            _testHttpMessageHandler.HandleException(requestPath, exception);
            await TriggerCircuitBreakerFromExceptionAsync(requestPath);
        }

        public async Task TriggerFromTransientHttpStatusCodeAsync(HttpStatusCode httpStatusCode)
        {
            var handledRequestPath = _testHttpMessageHandler.HandleTransientHttpStatusCode(
                requestPath: "/circuit-breaker/transient-http-status-code",
                responseHttpStatusCode: httpStatusCode);
            await TriggerCircuitBreakerFromTransientStatusCodeAsync(handledRequestPath, httpStatusCode);
        }

        public async Task WaitForResetAsync()
        {
            // wait for the duration of break so that the circuit goes into half open state
            await Task.Delay(TimeSpan.FromSeconds(_circuitBreakerOptions.DurationOfBreakInSecs));
            // successful response will move the circuit breaker into closed state
            var response = await _httpClient.GetAsync(_resetRequestPath);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            // make sure we transition to a new sampling window or else requests would still fall
            // in the previous sampling window where the circuit state had already been open and closed.
            await Task.Delay(TimeSpan.FromSeconds(_circuitBreakerOptions.SamplingDurationInSecs));
        }
        
        /// <remarks>
        /// The circuit breaker policy added is a wrapped policy which joins an
        /// <see cref="AsyncCircuitBreakerPolicy{TResult}"/> and a <see cref="CircuitBreakerCheckerAsyncPolicy{T}"/>.
        /// 
        /// The <see cref="CircuitBreakerCheckerAsyncPolicy{T}"/> will check if the circuit is open/isolated and
        /// if so it will return <see cref="CircuitBrokenHttpResponseMessage"/> which is an http response message
        /// with 500 status code and some extra properties.
        /// </remarks>
        public async Task ShouldBeOpenAsync(string requestPath)
        {
            var response = await _httpClient.GetAsync(requestPath);
            var circuitBrokenHttpResponseMessage = response as CircuitBrokenHttpResponseMessage;
            circuitBrokenHttpResponseMessage.ShouldNotBeNull();
        }

        private string HandleResetRequest()
        {
            var handledRequestPath = "/circuit-breaker/reset";
            _testHttpMessageHandler.MockHttpResponse(builder =>
            {
                builder
                    .Where(httpRequestMessage => httpRequestMessage.RequestUri!.ToString().Contains(handledRequestPath))
                    .RespondWith(new HttpResponseMessage(HttpStatusCode.OK));
            });
            return handledRequestPath;
        }

        private async Task TriggerCircuitBreakerFromTransientStatusCodeAsync(
            string requestPath,
            HttpStatusCode httpStatusCode)
        {
            for (var i = 0; i < _circuitBreakerOptions.MinimumThroughput; i++)
            {
                var response = await _httpClient.GetAsync(requestPath);
                // the circuit should be closed during this loop which means it will be returning the 
                // expected status code. Once the circuit is open it starts failing fast by returning
                // a CircuitBrokenHttpResponseMessage instance whose status code is 500
                response.StatusCode.ShouldBe(httpStatusCode); 
            }
        }

        private async Task TriggerCircuitBreakerFromExceptionAsync(string requestPath)
        {
            for (var i = 0; i < _circuitBreakerOptions.MinimumThroughput; i++)
            {
                // not only asserts the exception is expected but also avoids the exception
                // being propagated in order to open the circuit after the CircuitBreakerOptions.MinimumThroughput
                // number of requests
                await Should.ThrowAsync<Exception>(() => _httpClient.GetAsync(requestPath));
            }
        }

        public async ValueTask DisposeAsync()
        {
            await WaitForResetAsync();
        }
    }
}