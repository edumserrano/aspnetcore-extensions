﻿using DotNet.Sdk.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNet.Sdk.Extensions.Tests.Options.ValidateEagerly.DataAnnotations
{
    public class StartupMyOptions2ValidateEargerly
    {
        private readonly IConfiguration _configuration;

        public StartupMyOptions2ValidateEargerly(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services                                                                               
                .AddOptions<MyOptions2>()
                .Bind(_configuration)
                .ValidateDataAnnotations()
                .ValidateEagerly();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", async context =>
                    {
                        var myOptions = context.RequestServices.GetRequiredService<MyOptions2>();
                        await context.Response.WriteAsync(myOptions.SomeOption);
                    });
                });
        }
    }
}
