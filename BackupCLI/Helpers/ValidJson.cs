using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace BackupCLI.Helpers;

/// <summary>
/// Provides non-null type checking and default value support for deserialization using the built-in JSON library. 
/// </summary>
public class ValidJson
{
    /// <summary>
    /// Default properties assigned in the constructor.
    /// </summary>
    private Dictionary<string, object?> DefaultProps { get; }

    private object? PropOrDefault(PropertyInfo prop) => prop.GetValue(this) ?? DefaultProps[prop.Name];

    /// <summary>
    /// Recursively check all properties for null values.
    /// </summary>
    /// <exception cref="JsonException">Thrown when a property is null</exception>
    public virtual void Validate()
    {
        PropertyInfo[] props = GetType().GetProperties();
        MethodInfo validate = GetType().GetMethod("Validate")!;

        foreach (var prop in props.Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null))
            prop.SetValue(this, PropOrDefault(prop) ?? throw new JsonException($"{prop.Name} cannot be null."));

        foreach (var prop in props.Where(p => typeof(ValidJson).IsAssignableFrom(p.PropertyType)))
            validate.Invoke(prop.GetValue(this), null);
    }

    protected ValidJson()
    {
        DefaultProps = GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(this));
    }
}

/// <summary>
/// Simple wrapper for json lists/arrays that checks all of its items for null and validates them.
/// </summary>
public class JsonList<TValue>(List<TValue> items) : ValidJson where TValue : ValidJson
{
    public List<TValue> Items { get; set; } = items;

    /// <exception cref="JsonException">Thrown when an item is null or not valid</exception>
    public override void Validate() => Items.ForEach(item =>
    {
        if (item is null) throw new JsonException("List item cannot be null.");
        item.Validate();
    });
}

/// <summary>
/// Used in the <see cref="JsonSerializerOptions"/> configuration object.
/// </summary>
public class JsonListConverter<TValue> : JsonConverter<JsonList<TValue>> where TValue : ValidJson
{
    public override JsonList<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        List<TValue> items = JsonSerializer.Deserialize<List<TValue>>(doc.RootElement.GetRawText(), options);
        return new JsonList<TValue>(items);
    }

    public override void Write(Utf8JsonWriter writer, JsonList<TValue> value, JsonSerializerOptions options)
        => throw new NotImplementedException();
}