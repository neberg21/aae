using System.Threading.Channels;

namespace Module.AI.Chat.Jobs;

public class ExecuteVisionChannel
{
    private readonly Channel<Vision> _channel = Channel.CreateUnbounded<Vision>();

    public void TryWrite(Vision vision)
    {
        _channel.Writer.TryWrite(vision);
    }

    public async Task<Vision> ReadAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }

    public async Task<bool> WaitToReadAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.WaitToReadAsync(cancellationToken);
    }
}