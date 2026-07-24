using System.Threading.Channels;

namespace Module.AI.Chat.Jobs;

public class ExecuteRecruitingChannel
{
    private readonly Channel<Recruitment> _channel = Channel.CreateUnbounded<Recruitment>();
    
    public void TryWrite(Recruitment response)
    {
        _channel.Writer.TryWrite(response);
    }

    public async Task<Recruitment> ReadAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }

    public async Task<bool> WaitToReadAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.WaitToReadAsync(cancellationToken);
    }
}