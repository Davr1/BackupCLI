using System.Text.Json;

namespace BackupCLI.Helpers;

public static class JsonManipulator
{
    public static TValue LoadFile<TValue>(string path, JsonSerializerOptions options)
    {
        if (JsonSerializer.Deserialize<TValue>(File.ReadAllText(path), options) is not { } json)
            throw new JsonException("Input file is not a valid JSON file.");

        return json;
    }

    public static bool TryLoadFile<TValue>(string path, JsonSerializerOptions options, out TValue? output)
    {
        try
        {
            output = LoadFile<TValue>(path, options);
            return true;
        }
        catch (Exception e)
        {
            output = default;
            Program.Logger.Error(e);
            return false;
        }
    }
}