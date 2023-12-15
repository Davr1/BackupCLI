using System.Text.Json;
using System.Text.Json.Serialization;
using Quartz;

namespace BackupCLI.Helpers.Json.Converters;

/// <summary>
/// Custom parser for <see cref="CronExpression"/> that converts standard cron expressions to quartz format.
/// </summary>
public class CronConverter : JsonConverter<CronExpression>
{
    public override CronExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Cron expression must be a string");

        string[] parts = reader.GetString()!.Split(' ');

        if (parts.Length is < 5 or > 7)
            throw new JsonException("Invalid cron expression");

        // standard cron expression are incompatible with the quartz format, so we need to convert them
        if (parts.Length == 5) parts = ["0", ..parts];

        // day of week and day of month are mutually exclusive
        if (parts[3].Contains('*') && parts[5] != "?") parts[3] = "?";
        else if (parts[5].Contains('*') && parts[3] != "?") parts[5] = "?";

        return new CronExpression(string.Join(' ', parts));
    }

    public override void Write(Utf8JsonWriter writer, CronExpression value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.CronExpressionString);
}
