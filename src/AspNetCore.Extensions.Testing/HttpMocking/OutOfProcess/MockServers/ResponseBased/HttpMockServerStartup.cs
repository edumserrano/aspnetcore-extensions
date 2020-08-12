﻿using AspNetCore.Extensions.Testing.HttpMocking.OutOfProcess.MockServers.ResponseBased.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Extensions.Testing.HttpMocking.OutOfProcess.MockServers.ResponseBased
{
    public class HttpMockServerStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton<DefaultResponseMiddleware>()
                .AddSingleton<ResponseMocksMiddleware>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app
                .UseResponseMocks()
                .RunDefaultResponse();
        }
    }
}