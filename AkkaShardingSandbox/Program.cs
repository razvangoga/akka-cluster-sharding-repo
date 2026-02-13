using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Remote.Hosting;
using AkkaShardingSandbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

const bool USE_ID_FROM_COMMAND = true;

IHost builder = new HostBuilder()
    .ConfigureAppConfiguration(c => c.AddEnvironmentVariables()
        .AddJsonFile("appsettings.json"))
    .ConfigureServices((app, services) =>
    {
        const string actorSystemName = "AkkaShardingSandboxSystem";
        const string? sandboxRoleName = "sandbox";
        const string publicHostname = "localhost";
        const int remotePort = 2550;

        services.AddAkka(actorSystemName, (builder, sp) =>
        {
            builder.ConfigureLoggers(logConfig =>
            {
                logConfig.ClearLoggers();
                logConfig.AddLoggerFactory();
            });

            builder.WithRemoting(opt =>
            {
                opt.PublicHostName = publicHostname;
                opt.Port = remotePort;
                opt.HostName = "0.0.0.0";
            });

            builder.WithShardRegion<ProcessActor>(
                typeName: nameof(ProcessActor),
                entityPropsFactory: (system, registry, resolver) => ProcessActor.PropsFactory(system),
                messageExtractor: new ProcessMessageExtractor(USE_ID_FROM_COMMAND),
                shardOptions: new ShardOptions
                {
                    RememberEntities = false,
                    Role = sandboxRoleName,
                    StateStoreMode = StateStoreMode.DData,
                });

            builder.WithClustering(new ClusterOptions
            {
                LogInfoVerbose = true,
                Roles = [sandboxRoleName],
                // RG 20250623 #1191573 - a fake seed node is needed for the unit tests to not timeout
                // Use the public hostname and remote port to reference itself as seed node
                SeedNodes = [$"akka.tcp://{actorSystemName}@{publicHostname}:{remotePort}"]
            });

            builder.AddStartup(async (system, registry) =>
            {
                await OnStartup(system, registry, sp.GetRequiredService<ILogger<ProcessActor>>());
            });
        });
    })
    .ConfigureLogging((_, builder) => { builder.AddConsole(); })
    .Build();

await builder.RunAsync();
static async Task OnStartup(ActorSystem system, IActorRegistry actorRegistry, ILogger<ProcessActor> logger)
{
    IActorRef actor = await actorRegistry.GetAsync<ProcessActor>();

    logger.LogInformation($"{nameof(USE_ID_FROM_COMMAND)} is {USE_ID_FROM_COMMAND}");

    StartResponse startResponse = await actor.Ask<StartResponse>(new StartCommand());

    logger.LogInformation($"started process {startResponse.Id}");

    for (int i = 1; i <= 5; i++)
    {
        HitResponse response = await actor.Ask<HitResponse>(new HitCommand(startResponse.Id));
        logger.LogInformation($"hit {i} on process {startResponse.Id} => {response.HitCount}");
    }
}
