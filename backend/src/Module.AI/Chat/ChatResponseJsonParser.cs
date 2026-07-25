using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Module.AI.Chat;

public static partial class ChatResponseJsonParser
{
    [GeneratedRegex("```(?:\\s*json)?\\s*(?<json>[\\s\\S]*?)\\s*```", RegexOptions.IgnoreCase)]
    private static partial Regex JsonCodeFenceRegex();

    public static bool TryDeserialize<T>(string? content, [NotNullWhen(true)] out T? response)
    {
        response = default;
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var options = new JsonSerializerOptions().ConfigureJsonSerialization();
        var candidates = GetCandidates(content);
        foreach (var candidate in candidates)
        {
            try
            {
                response = JsonSerializer.Deserialize<T>(candidate, options);
                if (response is not null)
                    return true;
            }
            catch (JsonException)
            {
            }
        }

        return false;
    }

    private static IEnumerable<string> GetCandidates(string content)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        if (TryAdd(content.Trim(), seen, out var trimmed))
            yield return trimmed;

        foreach (Match match in JsonCodeFenceRegex().Matches(content))
        {
            if (TryAdd(match.Groups["json"].Value.Trim(), seen, out var fenced))
                yield return fenced;
        }

        foreach (var extracted in ExtractBalancedJson(content))
        {
            if (TryAdd(extracted, seen, out var balanced))
                yield return balanced;
        }
    }

    private static bool TryAdd(string value, ISet<string> seen, [NotNullWhen(true)] out string? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!seen.Add(value))
            return false;

        result = value;
        return true;
    }

    private static IEnumerable<string> ExtractBalancedJson(string content)
    {
        for (var start = 0; start < content.Length; start++)
        {
            var startChar = content[start];
            if (startChar is not ('{' or '['))
                continue;

            var stack = new Stack<char>();
            stack.Push(startChar == '{' ? '}' : ']');

            var inString = false;
            var isEscaped = false;

            for (var index = start + 1; index < content.Length; index++)
            {
                var current = content[index];

                if (inString)
                {
                    if (isEscaped)
                    {
                        isEscaped = false;
                        continue;
                    }

                    if (current == '\\')
                    {
                        isEscaped = true;
                        continue;
                    }

                    if (current == '"')
                        inString = false;

                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                    continue;
                }

                if (current == '{')
                {
                    stack.Push('}');
                    continue;
                }

                if (current == '[')
                {
                    stack.Push(']');
                    continue;
                }

                if (current is not ('}' or ']'))
                    continue;

                if (stack.Count == 0 || stack.Peek() != current)
                    break;

                stack.Pop();
                if (stack.Count != 0)
                    continue;

                var candidate = content[start..(index + 1)].Trim();
                if (!string.IsNullOrWhiteSpace(candidate))
                    yield return candidate;
                start = index;
                break;
            }
        }
    }
}
