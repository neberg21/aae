namespace Module.AI.Chat.Jobs;

public class ChannelDashboard
{
    private readonly Dictionary<string, int> _dashboard = new();

    public void Decrement<T>()
    {
        var name = Name<T>();

        if (!_dashboard.TryGetValue(name, out _))
        {
            _dashboard[name] = 0;
        }

        _dashboard[name]--;
    }

    public void Increment<T>()
    {
        var name = Name<T>();

        if (!_dashboard.TryGetValue(name, out _))
        {
            _dashboard[name] = 0;
        }

        _dashboard[name]++;
    }

    private static string Name<T>()
    {
        return typeof(T).Name;
    }
}