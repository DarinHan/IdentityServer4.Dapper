using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using IdentityServer4.Dapper.Extensions;
using AutoMapper.Configuration;

namespace IdentityServer.Dapper.Test
{
    class Startup
    {
        //private readonly IConfiguration _config;
        //private readonly IHostingEnvironment _env;

        //public Startup(IConfiguration config, IHostingEnvironment env)
        //{
        //    _config = config;
        //    _env = env;
        //}

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            //支持跨域
            services.AddCors(options =>
            {
                // this defines a CORS policy called "default"
                options.AddPolicy("default", policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            // configure identity server with in-memory stores, keys, clients and scopes
            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddMySQLProvider(option =>
                {
                    option.ConnectionString = "server=10.40.0.190;uid=changyin.han;pwd=fjfhhan07;database=identityserver4;SslMode=None;";
                })
                .AddConfigurationStore()
                .AddOperationalStore(option =>
                {
                    option.EnableTokenCleanup = true;
                    option.TokenCleanupInterval = 10;
                });

            return services.BuildServiceProvider(validateScopes: true);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("default");

            app.UseIdentityServer();
        }
    }
}
