using System.Text.Json;
using System.Text.Json.Serialization;
using BackupCLI.Helpers.Json.Converters;

namespace BackupCLI.Helpers.Json;

public static class JsonUtils
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new BackupJobConverter(), new BackupJobListConverter(), new CronConverter() },
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
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
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
