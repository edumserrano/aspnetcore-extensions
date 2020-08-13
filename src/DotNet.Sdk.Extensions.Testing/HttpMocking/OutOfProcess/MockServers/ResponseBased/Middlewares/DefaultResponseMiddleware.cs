﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DotNet.Sdk.Extensions.Testing.HttpMocking.OutOfProcess.MockServers.ResponseBased.Middlewares
{
    public static class DefaultResponseMiddlewareExtensions
    {
        public static IApplicationBuilder RunDefaultResponse(this IApplicationBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder.UseMiddleware<DefaultResponseMiddleware>();
        }
    }

    public class DefaultResponseMiddleware : IMiddleware
    {
        public Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
            //TODO send internal server error/404 with some body message
            return Task.CompletedTask;
        }
    }
}
