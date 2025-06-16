using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Prospect.TestClient;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Starting test client");

            var host = CreateHostBuilder(args).Build();
            var client = host.Services.GetRequiredService<TestClient>();

            // Connect to the server
            await client.Connect("127.0.0.1", 7777);

            // Keep the program running
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Test client terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<TestClient>();
            });
} 