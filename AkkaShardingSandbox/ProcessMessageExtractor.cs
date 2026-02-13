using Akka.Cluster.Sharding;

namespace AkkaShardingSandbox;

public class ProcessMessageExtractor(bool useIdFromStartCommand) : HashCodeMessageExtractor(50)
{
    public override string? EntityId(object message)
    {
        return message switch
        {
            StartCommand startCommand => useIdFromStartCommand
                ? startCommand.Id
                : Guid.NewGuid().ToString("N"),
            HitCommand processMessage => processMessage.Id,
            _ => throw new InvalidOperationException($"Unsupported message type: {message.GetType().Name}")
        };
    }
}

public record StartCommand
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
}

public record StartResponse(string Id);
public record HitCommand(string Id);
public record HitResponse(string Id, int HitCount);
