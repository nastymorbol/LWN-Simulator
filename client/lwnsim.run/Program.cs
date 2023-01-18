// See https://aka.ms/new-console-template for more information

using lwnsim.Configuration;
using lwnsim.Devices;
using lwnsim.Devices.Factory;
using lwnsim.Devices.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


var app = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(host =>
    {
    })
    .ConfigureLogging(options =>
    {
        options.ClearProviders();
        options.AddSimpleConsole(builder =>
        {
            builder.SingleLine = true;
        });
    })
    .ConfigureServices((host, services) =>
    {
        services.AddTransient<Jurassic.ScriptEngine>(p => new Jurassic.ScriptEngine());
        services.AddHostedService<LwnSimulatorClient>();
        services.AddSingleton<LwnConnectionService>();
        services.Configure<LwnConnection>(host.Configuration.GetSection("LwnConnection"));
        services.AddHttpClient(nameof(LwnConnectionService), (provider, client) =>
        {
            client.BaseAddress = provider.GetRequiredService<IOptions<LwnConnection>>().Value.ApiUrl;
        });
        
        services.AddSingleton<SimDeviceFactory>();
        services.AddSingleton<ISimuDevice, SensativeStrip>();
    })
    .Build();

app.Run();