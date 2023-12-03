using System.Text.Json;

namespace BackupCLI.Helpers;

public static class JsonManipulator
{
    public static TValue LoadFile<TValue>(string path, JsonSerializerOptions options) where TValue : ValidJson
    {
        if (JsonSerializer.Deserialize<TValue>(File.ReadAllText(path), options) is not { } json)
            throw new JsonException("Input file is not a valid JSON file.");

        json.Validate();

        return json;
    }

    public static bool TryLoadFile<TValue>(string path, JsonSerializerOptions options, out TValue? output) where TValue : ValidJson
    {
        output = default;

        try
        {
            output = LoadFile<TValue>(path, options);
            return true;
        }
        catch (Exception e)
        {
            Program.Logger.Error(e);
            return false;
        }
    }
}