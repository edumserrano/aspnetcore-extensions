﻿using System;
using DotNet.Sdk.Extensions.Polly.Http.Retry.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace DotNet.Sdk.Extensions.Polly.Http.Retry.Extensions
{
    public static class RetryPolicyHttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddRetryPolicy(
            this IHttpClientBuilder httpClientBuilder,
            string optionsName)
        {
            return httpClientBuilder.AddRetryPolicyCore<DefaultRetryPolicyConfiguration>(
                optionsName: optionsName,
                configureOptions: null);
        }

        public static IHttpClientBuilder AddRetryPolicy(
            this IHttpClientBuilder httpClientBuilder,
            Action<RetryOptions> configureOptions)
        {
            return httpClientBuilder.AddRetryPolicyCore<DefaultRetryPolicyConfiguration>(
                optionsName: null,
                configureOptions: configureOptions);
        }

        public static IHttpClientBuilder AddRetryPolicy(
            this IHttpClientBuilder httpClientBuilder,
            string optionsName,
            Action<RetryOptions> configureOptions)
        {
            return httpClientBuilder.AddRetryPolicyCore<DefaultRetryPolicyConfiguration>(
                optionsName: optionsName,
                configureOptions: configureOptions);
        }

        public static IHttpClientBuilder AddRetryPolicy<TPolicyConfiguration>(
            this IHttpClientBuilder httpClientBuilder,
            string optionsName)
            where TPolicyConfiguration : class, IRetryPolicyConfiguration
        {
            return httpClientBuilder.AddRetryPolicyCore<TPolicyConfiguration>(
                optionsName: optionsName,
                configureOptions: null);
        }

        public static IHttpClientBuilder AddRetryPolicy<TPolicyConfiguration>(
            this IHttpClientBuilder httpClientBuilder,
            Action<RetryOptions> configureOptions)
            where TPolicyConfiguration : class, IRetryPolicyConfiguration
        {
            return httpClientBuilder.AddRetryPolicyCore<TPolicyConfiguration>(
                optionsName: null,
                configureOptions: configureOptions);
        }

        public static IHttpClientBuilder AddRetryPolicy<TPolicyConfiguration>(
            this IHttpClientBuilder httpClientBuilder,
            string optionsName,
            Action<RetryOptions> configureOptions)
            where TPolicyConfiguration : class, IRetryPolicyConfiguration
        {
            return httpClientBuilder.AddRetryPolicyCore<TPolicyConfiguration>(
                optionsName: optionsName,
                configureOptions: configureOptions);
        }

        private static IHttpClientBuilder AddRetryPolicyCore<TPolicyConfiguration>(
            this IHttpClientBuilder httpClientBuilder,
            string? optionsName,
            Action<RetryOptions>? configureOptions)
            where TPolicyConfiguration : class, IRetryPolicyConfiguration
        {
            var httpClientName = httpClientBuilder.Name;
            optionsName ??= $"{httpClientName}_retry_{Guid.NewGuid()}";
            configureOptions ??= _ => { };
            httpClientBuilder.Services
                .AddSingleton<TPolicyConfiguration>()
                .AddHttpClientRetryOptions(optionsName)
                .Configure(configureOptions);

            return httpClientBuilder.AddHttpMessageHandler(provider =>
            {
                var policyConfiguration = provider.GetRequiredService<TPolicyConfiguration>();
                var retryOptions = provider.GetHttpClientRetryOptions(optionsName);
                var retryPolicy = RetryPolicyFactory.CreateRetryPolicy(httpClientName, retryOptions, policyConfiguration);
                return new PolicyHttpMessageHandler(retryPolicy);
            });
        }
    }
}
