﻿using System;
using System.Net.Http;
using DotNet.Sdk.Extensions.Polly.Http.CircuitBreaker;
using DotNet.Sdk.Extensions.Polly.Http.Fallback;
using DotNet.Sdk.Extensions.Polly.Http.Resilience.Events;
using DotNet.Sdk.Extensions.Polly.Http.Retry;
using DotNet.Sdk.Extensions.Polly.Http.Timeout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace DotNet.Sdk.Extensions.Polly.Http.Resilience.Extensions
{
    public static class ResiliencePoliciesHttpClientBuilderExtensions
    {
        private class BlankHttpMessageHandler : DelegatingHandler { }

        public static IHttpClientBuilder AddResiliencePolicies(
            this IHttpClientBuilder httpClientBuilder,
            string optionsName)
        {
            Func<IServiceProvider, IResiliencePoliciesEventHandler> eventHandlerFactory = _ => new DefaultResiliencePoliciesEventHandler();
            return httpClientBuilder.AddResiliencePoliciesCore(
                optionsName: optionsName,
                configureOptions: null,
                eventHandlerFactory: eventHandlerFactory);
        }

        public static IHttpClientBuilder AddResiliencePolicies(
            this IHttpClientBuilder httpClientBuilder,
            Action<ResilienceOptions> configureOptions)
        {
            Func<IServiceProvider, IResiliencePoliciesEventHandler> eventHandlerFactory = _ => new DefaultResiliencePoliciesEventHandler();
            return httpClientBuilder.AddResiliencePoliciesCore(
                optionsName: null,
                configureOptions: configureOptions,
                eventHandlerFactory: eventHandlerFactory);
        }

        public static IHttpClientBuilder AddResiliencePolicies<TPolicyEventHandler>(
            this IHttpClientBuilder httpClientBuilder,
            string optionsName)
            where TPolicyEventHandler : class, IResiliencePoliciesEventHandler
        {
            httpClientBuilder.Services.TryAddSingleton<TPolicyEventHandler>();
            Func<IServiceProvider, IResiliencePoliciesEventHandler> eventHandlerFactory = provider => provider.GetRequiredService<TPolicyEventHandler>();
            return httpClientBuilder.AddResiliencePoliciesCore(
                optionsName: optionsName,
                configureOptions: null,
                eventHandlerFactory: eventHandlerFactory);
        }

        public static IHttpClientBuilder AddResiliencePolicies<TPolicyEventHandler>(
            this IHttpClientBuilder httpClientBuilder,
            Action<ResilienceOptions> configureOptions)
            where TPolicyEventHandler : class, IResiliencePoliciesEventHandler
        {
            httpClientBuilder.Services.TryAddSingleton<TPolicyEventHandler>();
            Func<IServiceProvider, IResiliencePoliciesEventHandler> eventHandlerFactory = provider => provider.GetRequiredService<TPolicyEventHandler>();
            return httpClientBuilder.AddResiliencePoliciesCore(
                optionsName: null,
                configureOptions: configureOptions,
                eventHandlerFactory: eventHandlerFactory);
        }

        public static IHttpClientBuilder AddResiliencePolicies(
            this IHttpClientBuilder httpClientBuilder,
            string optionsName,
            Func<IServiceProvider, IResiliencePoliciesEventHandler> eventHandlerFactory)
        {
            return httpClientBuilder.AddResiliencePoliciesCore(
                optionsName: optionsName,
                configureOptions: null,
                eventHandlerFactory: eventHandlerFactory);
        }

        public static IHttpClientBuilder AddResiliencePolicies(
            this IHttpClientBuilder httpClientBuilder,
            Action<ResilienceOptions> configureOptions,
            Func<IServiceProvider, IResiliencePoliciesEventHandler> eventHandlerFactory)
        {
            return httpClientBuilder.AddResiliencePoliciesCore(
                optionsName: null,
                configureOptions: configureOptions,
                eventHandlerFactory: eventHandlerFactory);
        }

        private static IHttpClientBuilder AddResiliencePoliciesCore(
            this IHttpClientBuilder httpClientBuilder,
            string? optionsName,
            Action<ResilienceOptions>? configureOptions,
            Func<IServiceProvider, IResiliencePoliciesEventHandler> eventHandlerFactory)
        {
            var httpClientName = httpClientBuilder.Name;
            optionsName ??= $"{httpClientName}_resilience_{Guid.NewGuid()}";
            configureOptions ??= _ => { };
            httpClientBuilder.Services
                .AddSingleton<IValidateOptions<ResilienceOptions>, ResilienceOptionsValidation>()
                .AddHttpClientResilienceOptions(optionsName)
                .ValidateDataAnnotations()
                .Configure(configureOptions);

            // here can NOT reuse the other extension methods to add the policies
            // like for instance RetryPolicyHttpClientBuilderExtensions.AddRetryPolicy
            // because those extension methods add the TPolicyEventHandler and the options
            // to the ServiceCollection plus options validation.
            // This would lead to duplicated registrations and incorrect behavior when
            // validating options (multiple validations and multiple error messages when validations fail).
            return httpClientBuilder
                .AddResilienceFallbackPolicy(optionsName, eventHandlerFactory)
                .AddResilienceRetryPolicy(optionsName, eventHandlerFactory)
                .AddResilienceCircuitBreakerPolicy(optionsName, eventHandlerFactory)
                .AddResilienceTimeoutPolicy(optionsName, eventHandlerFactory);
        }

        private static IHttpClientBuilder AddResilienceFallbackPolicy(
            this IHttpClientBuilder httpClientBuilder,
            string optionsName,
            Func<IServiceProvider, IResiliencePoliciesEventHandler> eventHandlerFactory)
        {
            return httpClientBuilder.AddHttpMessageHandler(provider =>
            {
                var resilienceOptions = provider.GetHttpClientResilienceOptions(optionsName);
                if (!resilienceOptions.EnableFallbackPolicy)
                {
                    return new BlankHttpMessageHandler();
                }

                var policyEventHandler = eventHandlerFactory(provider);
                var retryPolicy = FallbackPolicyFactory.CreateFallbackPolicy(
                    httpClientBuilder.Name,
                    policyEventHandler);
                return new PolicyHttpMessageHandler(retryPolicy);
            });
        }

        private static IHttpClientBuilder AddResilienceRetryPolicy(
            this IHttpClientBuilder httpClientBuilder,
            string optionsName, Func<IServiceProvider,
                IResiliencePoliciesEventHandler> eventHandlerFactory)
        {
            return httpClientBuilder.AddHttpMessageHandler(provider =>
            {
                var resilienceOptions = provider.GetHttpClientResilienceOptions(optionsName);
                if (!resilienceOptions.EnableRetryPolicy)
                {
                    return new BlankHttpMessageHandler();
                }

                var policyEventHandler = eventHandlerFactory(provider);
                var retryPolicy = RetryPolicyFactory.CreateRetryPolicy(
                    httpClientBuilder.Name,
                    resilienceOptions.Retry,
                    policyEventHandler);
                return new PolicyHttpMessageHandler(retryPolicy);
            });
        }

        private static IHttpClientBuilder AddResilienceCircuitBreakerPolicy(
            this IHttpClientBuilder httpClientBuilder,
            string optionsName,
            Func<IServiceProvider, IResiliencePoliciesEventHandler> eventHandlerFactory)
        {
            return httpClientBuilder.AddHttpMessageHandler(provider =>
            {
                var resilienceOptions = provider.GetHttpClientResilienceOptions(optionsName);
                if (!resilienceOptions.EnableCircuitBreakerPolicy)
                {
                    return new BlankHttpMessageHandler();
                }

                var policyEventHandler = eventHandlerFactory(provider);
                var retryPolicy = CircuitBreakerPolicyFactory.CreateCircuitBreakerPolicy(
                    httpClientBuilder.Name,
                    resilienceOptions.CircuitBreaker,
                    policyEventHandler);
                return new PolicyHttpMessageHandler(retryPolicy);
            });
        }

        private static IHttpClientBuilder AddResilienceTimeoutPolicy(
            this IHttpClientBuilder httpClientBuilder,
            string optionsName,
            Func<IServiceProvider, IResiliencePoliciesEventHandler> eventHandlerFactory)
        {
            return httpClientBuilder.AddHttpMessageHandler(provider =>
            {
                var resilienceOptions = provider.GetHttpClientResilienceOptions(optionsName);
                if (!resilienceOptions.EnableTimeoutPolicy)
                {
                    return new BlankHttpMessageHandler();
                }

                var policyEventHandler = eventHandlerFactory(provider);
                var retryPolicy = TimeoutPolicyFactory.CreateTimeoutPolicy(
                    httpClientBuilder.Name,
                    resilienceOptions.Timeout,
                    policyEventHandler);
                return new PolicyHttpMessageHandler(retryPolicy);
            });
        }
    }
}
