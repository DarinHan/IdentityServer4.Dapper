using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using NLog.Web;
using System.Linq;

namespace IdentityServer.Dapper.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Onesmart统一授权验证服务";

            var logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                logger.Debug("init main");
                var seed = args.Contains("--seed");
                if (seed)
                {
                    args = args.Except(new[] { "--seed" }).ToArray();
                }
                var host = BuildWebHost(args);

                if (seed)
                {
                    SeedData.EnsureSeedData(host.Services);
                }

                host.Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var builder = WebHost.CreateDefaultBuilder(args)
                 .UseContentRoot(Directory.GetCurrentDirectory())
                 .ConfigureAppConfiguration((hostingContext, config) =>
                 {
                     var env = hostingContext.HostingEnvironment;
                     config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                           .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                     config.AddEnvironmentVariables();
                 })
                 .ConfigureLogging(logging =>
                 {
                     logging.ClearProviders();
                     logging.AddConsole();
                     logging.SetMinimumLevel(LogLevel.Trace);
                 })
                 .UseNLog()  // NLog: setup NLog for Dependency injection
                 .UseStartup<Startup>();
            var configbuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var configuration = configbuilder.Build();

            builder.UseUrls(configuration["HostUrl"]);

            return builder.Build();

        }
    }
}
