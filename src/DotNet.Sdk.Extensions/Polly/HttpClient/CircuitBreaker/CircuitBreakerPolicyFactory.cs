﻿using System;
using System.Threading.Tasks;
using DotNet.Sdk.Extensions.Polly.Policies;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace DotNet.Sdk.Extensions.Polly.HttpClient.CircuitBreaker
{
    internal static class CircuitBreakerPolicyFactory
    {
        public static IsPolicy CreateCircuitBreakerPolicy(
            CircuitBreakerOptions options,
            ICircuitBreakerPolicyConfiguration policyConfiguration)
        {
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .Or<TaskCanceledException>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: options.FailureThreshold,
                    samplingDuration: TimeSpan.FromSeconds(options.SamplingDurationInSecs),
                    minimumThroughput: options.MinimumThroughput,
                    durationOfBreak: TimeSpan.FromSeconds(options.DurationOfBreakInSecs),
                    onBreak: async (lastOutcome, previousState, breakDuration, context) =>
                    {
                        await policyConfiguration.OnBreakAsync(options, lastOutcome, previousState, breakDuration, context);
                    },
                    onReset: async context =>
                    {
                        await policyConfiguration.OnResetAsync(options, context);
                    },
                    onHalfOpen: async () =>
                    {
                        await policyConfiguration.OnHalfOpenAsync(options);
                    });
            var circuitBreakerCheckerPolicy = new CircuitBreakerCheckerAsyncPolicy(circuitBreakerPolicy);
            var finalPolicy = Policy.WrapAsync(circuitBreakerCheckerPolicy, circuitBreakerPolicy);
            return finalPolicy;
        }
    }
}
