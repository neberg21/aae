using System.Threading.Channels;
using Module.AI.Persistence;

namespace Module.AI.Chat.Jobs;

public class ExecuteVisionChannel : ExecuteJobChannel<Vision>
{
    public ExecuteVisionChannel(ChannelDashboard dashboard) : base(dashboard)
    {
    }
}

public class ExecuteRecruitmentChannel : ExecuteJobChannel<Recruitment>
{
    public ExecuteRecruitmentChannel(ChannelDashboard dashboard) : base(dashboard)
    {
    }
}

public class ExecuteOnboardingChannel : ExecuteJobChannel<Onboarding>
{
    public ExecuteOnboardingChannel(ChannelDashboard dashboard) : base(dashboard)
    {
    }
}

public abstract class ExecuteJobChannel<T>
{
    private readonly ChannelDashboard _dashboard;
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    protected ExecuteJobChannel(ChannelDashboard dashboard)
    {
        _dashboard = dashboard;
    }

    public void TryWrite(T item)
    {
        _dashboard.Increment<T>();
        _channel.Writer.TryWrite(item);
    }

    public async Task<T> ReadAsync(CancellationToken cancellationToken)
    {
        _dashboard.Decrement<T>();
        return await _channel.Reader.ReadAsync(cancellationToken);
    }

    public async Task<bool> WaitToReadAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.WaitToReadAsync(cancellationToken);
    }
}