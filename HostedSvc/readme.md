# Background tasks with hosted services in ASP.NET Core

In ASP.NET Core, background tasks can be implemented as hosted services. A hosted service is a class with background task logic that implements the IHostedService interface.

## IHostedService interface
The IHostedService interface defines two methods for objects that are managed by the host:
* StartAsync(CancellationToken)
* StopAsync(CancellationToken)

### StartAsync
StartAsync contains the logic to start the background task. StartAsync is called before:

* The app's request processing pipeline is configured.
* The server is started and IApplicationLifetime.ApplicationStarted is triggered.

The default behavior can be changed so that the hosted service's StartAsync runs after the app's pipeline has been configured and ApplicationStarted is called. To change the default behavior, add the hosted service after calling ConfigureWebHostDefaults:

``` csharp
public class Program
{
    public static void Main(string[] args) {
        CreateHostBuilder(args).Build().Run();
    }
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => {
                webBuilder.UseStartup<Startup>();
            })
            .ConfigureServices(services => {
                services.AddHostedService<MyHostedService>();
            });
}
```

### StopAsync
StopAsync(CancellationToken) is triggered when the host is performing a graceful shutdown. StopAsync contains the logic to end the background task. Implement IDisposable and finalizers (destructors) to dispose of any unmanaged resources.

The cancellation token has a default five second timeout to indicate that the shutdown process should no longer be graceful. When cancellation is requested on the token:

* Any remaining background operations that the app is performing should be aborted.
* Any methods called in StopAsync should return promptly.

However, tasks aren't abandoned after cancellation is requested—the caller awaits all tasks to complete.

If the app shuts down unexpectedly (for example, the app's process fails), StopAsync might not be called. Therefore, any methods called or operations conducted in StopAsync might not occur.

To extend the default five second shutdown timeout, set:

* ShutdownTimeout when using Generic Host. 
* Shutdown timeout host configuration setting when using Web Host. 

The hosted service is activated once at app startup and gracefully shut down at app shutdown. If an error is thrown during background task execution, Dispose should be called even if StopAsync isn't called.

### BackgroundService base class
BackgroundService is a base class for implementing a long running IHostedService.

ExecuteAsync(CancellationToken) is called to run the background service. The implementation returns a Task that represents the entire lifetime of the background service. No further services are started until ExecuteAsync becomes asynchronous, such as by calling await. Avoid performing long, blocking initialization work in ExecuteAsync. The host blocks in StopAsync(CancellationToken) waiting for ExecuteAsync to complete.

The cancellation token is triggered when IHostedService.StopAsync is called. Your implementation of ExecuteAsync should finish promptly when the cancellation token is fired in order to gracefully shut down the service. Otherwise, the service ungracefully shuts down at the shutdown timeout. For more information, see the IHostedService interface section.

StartAsync should be limited to short running tasks because hosted services are run sequentially, and no further services are started until StartAsync runs to completion. Long running tasks should be placed in ExecuteAsync. For more information, see the source to BackgroundService.

``` csharp
public class TimedHostedService : IHostedService, IDisposable
{
    private int executionCount = 0;
    private readonly ILogger<TimedHostedService> _logger;
    private Timer _timer;

    public TimedHostedService(ILogger<TimedHostedService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");

        _timer = new Timer(DoWork, null, TimeSpan.Zero, 
            TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        var count = Interlocked.Increment(ref executionCount);

        _logger.LogInformation(
            "Timed Hosted Service is working. Count: {Count}", count);
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service is stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
``` 

The Timer doesn't wait for previous executions of DoWork to finish, so the approach shown might not be suitable for every scenario. Interlocked.Increment is used to increment the execution counter as an atomic operation, which ensures that multiple threads don't update executionCount concurrently.

# Consuming a scoped service in a background task
To use scoped services within a BackgroundService, create a scope. No scope is created for a hosted service by default.

The scoped background task service contains the background task's logic. In the following example:

* The service is asynchronous. The DoWork method returns a Task. For demonstration purposes, a delay of ten seconds is awaited in the DoWork method.
* An ILogger is injected into the service.


``` csharp
internal interface IScopedProcessingService
{
    Task DoWork(CancellationToken stoppingToken);
}

internal class ScopedProcessingService : IScopedProcessingService
{
    private int executionCount = 0;
    private readonly ILogger _logger;
    
    public ScopedProcessingService(ILogger<ScopedProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task DoWork(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            executionCount++;

            _logger.LogInformation(
                "Scoped Processing Service is working. Count: {Count}", executionCount);

            await Task.Delay(10000, stoppingToken);
        }
    }
}
```

The hosted service creates a scope to resolve the scoped background task service to call its DoWork method. DoWork returns a Task, which is awaited in ExecuteAsync:

``` csharp
public class ConsumeScopedServiceHostedService : BackgroundService
{
    private readonly ILogger<ConsumeScopedServiceHostedService> _logger;

    public ConsumeScopedServiceHostedService(IServiceProvider services, 
        ILogger<ConsumeScopedServiceHostedService> logger)
    {
        Services = services;
        _logger = logger;
    }

    public IServiceProvider Services { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Consume Scoped Service Hosted Service running.");

        await DoWork(stoppingToken);
    }

    private async Task DoWork(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Consume Scoped Service Hosted Service is working.");

        using (var scope = Services.CreateScope())
        {
            var scopedProcessingService = 
                scope.ServiceProvider
                    .GetRequiredService<IScopedProcessingService>();

            await scopedProcessingService.DoWork(stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Consume Scoped Service Hosted Service is stopping.");

        await base.StopAsync(stoppingToken);
    }
}
```

The services are registered in IHostBuilder.ConfigureServices (Program.cs). The hosted service is registered with the AddHostedService extension method:


``` csharp
services.AddHostedService<ConsumeScopedServiceHostedService>();
services.AddScoped<IScopedProcessingService, ScopedProcessingService>();
```

# Queued background tasks

Please refer to the reference section...

# References
* https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-5.0&tabs=visual-studio