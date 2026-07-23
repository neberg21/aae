using System.Threading.Channels;

namespace Module.AI.Chat.Jobs;

public class ExecuteRecruitingChannel
{
    private readonly Channel<RecruitingResponse> _channel = Channel.CreateUnbounded<RecruitingResponse>();
    
    public void TryWrite(RecruitingResponse response)
    {
        _channel.Writer.TryWrite(response);
    }

    public async Task<RecruitingResponse> ReadAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }

    public async Task<bool> WaitToReadAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.WaitToReadAsync(cancellationToken);
    }
}