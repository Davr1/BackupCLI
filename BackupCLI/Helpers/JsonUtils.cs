using System.Text.Json;
using System.Text.Json.Serialization;
using BackupCLI.Backup;

namespace BackupCLI.Helpers;

public static class JsonUtils
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new BackupJobJsonConverter(), new BackupJobJsonListConverter(), new CronConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static T LoadFile<T>(string path, JsonSerializerOptions? options = null)
    {
        if (JsonSerializer.Deserialize<T>(File.ReadAllText(path), options) is not { } json)
            throw new JsonException("Input file is not a valid JSON file.");

        return json;
    }

    public static bool TryLoadFile<T>(string path, out T? output, JsonSerializerOptions? options = null)
    {
        try
        {
            output = LoadFile<T>(path, options);
            return true;
        }
        catch (Exception e)
        {
            output = default;
            Program.Logger.Error(e);
            return false;
        }
    }

    public static bool TryWriteFile<T>(string path, T? input, JsonSerializerOptions? options = null)
    {
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(input, options));
            return true;
        }
        catch (Exception e)
        {
            Program.Logger.Error(e);
            return false;
        }
    }
}

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
