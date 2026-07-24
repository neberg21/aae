using System.Threading.Channels;
using Module.AI.Persistence;

namespace Module.AI.Chat.Jobs;

public class ExecuteVisionChannel : ExecuteJobChannel<Vision>;

public class ExecuteRecruitmentChannel : ExecuteJobChannel<Recruitment>;

public class ExecuteOnboardingChannel : ExecuteJobChannel<Agent>;

public abstract class ExecuteJobChannel<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public void TryWrite(T item)
    {
        _channel.Writer.TryWrite(item);
    }

    public async Task<T> ReadAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }

    public async Task<bool> WaitToReadAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.WaitToReadAsync(cancellationToken);
    }
}