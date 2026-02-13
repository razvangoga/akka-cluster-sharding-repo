using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Persistence;
using Microsoft.Extensions.Logging;

namespace AkkaShardingSandbox;

public class ProcessActor : ReceivePersistentActor
{
    private ProcessState _state;
    private readonly ILogger<ProcessActor> _logger;

    public ProcessActor(ProcessState state, ILogger<ProcessActor> logger)
    {
        _state = state;
        _logger = logger;

        Become(Uninitialized);
    }

    public void Uninitialized()
    {
        Command<StartCommand>(c =>
        {
            Become(Started);
            _logger.LogInformation($"START in {Self.Path}");
            Sender.Tell(new StartResponse(PersistenceId));
        });

        Command<HitCommand>(c =>
        {
            _logger.LogWarning($"uninitialized HIT in {Self.Path}");
            Sender.Tell(new HitResponse(_state.Id, -1));
        });
    }

    public void Started()
    {
        Command<StartCommand>(c =>
        {
            _logger.LogInformation($"ALREADY START in {Self.Path}");
            Sender.Tell(new Status.Failure(new InvalidOperationException("cant's start what is already started")));
        });

        Command<HitCommand>(c =>
        {
            _logger.LogInformation($"HIT in {Self.Path}");
            _state.Hits++;
            Sender.Tell(new HitResponse(_state.Id, _state.Hits));
        });
    }

    public override string PersistenceId => _state.Id;

    public static Func<string, Props> PropsFactory(ActorSystem system)
    {
        DependencyResolver resolver = DependencyResolver.For(system);
        return entityId => resolver.Props<ProcessActor>(new ProcessState(entityId));
    }
}
