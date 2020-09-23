using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using IdentityServer4.Dapper.Extensions.MySql;
using IdentityServer4.Dapper.Extensions.PostgreSQL;
using IdentityServer4.Dapper.Extensions;

namespace IdentityServer4.Dapper.Host
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
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
            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddPostgreSQLProvider(option =>
                {
                    //option.ConnectionString = "server=.;uid=darinhan;pwd=darinhan;database=identityserver4;SslMode=None;";
                    option.ConnectionString = "Host=localhost;Port=32676;Username=postgresadmin;Password=admin123;Database=postgresdb;Minimum Pool Size=5;Search Path=identityserver";
                })
                .AddConfigurationStore()
                .AddOperationalStore(option =>
                {
                    option.EnableTokenCleanup = true;
                    option.TokenCleanupInterval = 10;
                });

            services.AddMvc(options => {
                options.EnableEndpointRouting = false;
            });

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.EnvironmentName.Equals("Development"))
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseCors("default");

            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                //spa.UseProxyToSpaDevelopmentServer("http://localhost:7012");
                //if (env.IsDevelopment())
                //{
                spa.UseReactDevelopmentServer(npmScript: "start");
                //}
            });
        }
    }
}
