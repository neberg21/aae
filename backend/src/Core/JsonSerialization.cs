using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace System.Text.Json;

public static class JsonSerialization
{
    public static JsonSerializerOptions ConfigureJsonSerialization(this JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
        return options;
    }

    public static JsonElement ToJsonElement<T>(this T obj) =>
        JsonSerializer.SerializeToElement(obj, new JsonSerializerOptions().ConfigureJsonSerialization());
}