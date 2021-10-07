using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HostedSvc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // CreateHostBuilder(args).Build().Run();

            IHost host = CreateHostBuilder(args).Build();

            /// IServiceScope scope = host.Services.CreateScope();
            /// IServiceProvider services = scope.ServiceProvider;
            /// IWebHostEnvironment environment = services.GetRequiredService<IWebHostEnvironment>();

            IConfigurationRoot configRoot = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: false)
                .AddCommandLine(args)
                .Build();

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<MyHostedService>();
                    services.AddHostedService<MyBackgroundService>();
                })
            ;
    }
}
