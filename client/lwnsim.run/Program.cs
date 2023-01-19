// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.CompilerServices;
using lwnsim.Configuration;
using lwnsim.Devices;
using lwnsim.Devices.Factory;
using lwnsim.Devices.Interfaces;
using lwnsim.Devices.Milesight;
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


        var devices = Assembly.GetExecutingAssembly().DefinedTypes
            .Where(t => t.IsAssignableTo(typeof(ISimuDevice)) && !t.IsInterface && !t.IsAbstract ).ToArray();
        
        foreach (var device in devices)
        {
            services.AddTransient(typeof(ISimuDevice), device);
        }
        // services.AddTransient<ISimuDevice, SensativeStrip>();
        // services.AddTransient<ISimuDevice, Em300th>();
    })
    .Build();

app.Run();