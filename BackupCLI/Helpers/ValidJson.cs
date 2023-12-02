using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace BackupCLI.Helpers;

/// <summary>
/// Provides non-null type checking and default value support for deserialization using the built-in JSON library. 
/// </summary>
public class ValidJson
{
    private Dictionary<string, object?> DefaultProps { get; }
    private object? PropOrDefault(PropertyInfo prop) => prop.GetValue(this) ?? DefaultProps[prop.Name];

    public void Validate()
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
