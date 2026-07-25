namespace Module.AI.Chat.Jobs;

public class ChannelDashboard : Dictionary<string, int>
{
    public void Decrement<T>()
    {
        var name = Name<T>();

        if (!TryGetValue(name, out _))
        {
            this[name] = 0;
        }

        this[name]--;
    }

    public void Increment<T>()
    {
        var name = Name<T>();

        if (!TryGetValue(name, out _))
        {
            this[name] = 0;
        }

        this[name]++;
    }

    private static string Name<T>()
    {
        return typeof(T).Name;
    }
}