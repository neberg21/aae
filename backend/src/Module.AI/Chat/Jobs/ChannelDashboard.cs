namespace Module.AI.Chat.Jobs;

public class ChannelDashboard
{
    private readonly Dictionary<string, int> _dashboard = new();

    public void Decrement<T>()
    {
        _dashboard[typeof(T).Name]--;
    }

    public void Increment<T>()
    {
        _dashboard[typeof(T).Name]++;
    }
}