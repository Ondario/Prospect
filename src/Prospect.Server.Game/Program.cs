using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prospect.Server.Game.Config;
using Serilog;
using Serilog.Events;

namespace Prospect.Server.Game;

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
            Log.Information("Starting game server");

            var host = CreateHostBuilder(args).Build();
            var server = host.Services.GetRequiredService<GameServer>();
            var settings = host.Services.GetRequiredService<IOptions<GameServerSettings>>().Value;

            // Start the server using configured settings
            await server.Start(settings.Host, settings.Port);

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Game server terminated unexpectedly");
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
                services.Configure<GameServerSettings>(hostContext.Configuration.GetSection("GameServerSettings"));
                services.Configure<ApiServerSettings>(hostContext.Configuration.GetSection("ApiServerSettings"));
                services.Configure<SignalRSettings>(hostContext.Configuration.GetSection("SignalRSettings"));
                services.AddSingleton<GameServer>();
            });
}