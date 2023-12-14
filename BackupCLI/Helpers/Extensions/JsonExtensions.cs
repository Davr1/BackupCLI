using System.Text.Json;

namespace BackupCLI.Helpers.Extensions;

/// <summary>
/// Provides extension methods for <see cref="JsonElement"/> to allow for fallback values and case insensitive property names in a custom parser.
/// </summary>
public static class JsonExtensions {
    public static T? DeserializeOrDefault<T>(this JsonElement element, string propertyName, T? @default = default, JsonSerializerOptions? options = null)
    {
        var prop = element.EnumerateObject().FirstOrDefault(prop =>
            string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase));

        return prop.Value.DeserializeOrDefault(@default, options);
    }

    public static T? DeserializeOrDefault<T>(this JsonElement element, T? @default = default, JsonSerializerOptions? options = null)
        => element.ValueKind == JsonValueKind.Undefined ? @default : element.Deserialize<T>(options);
}
