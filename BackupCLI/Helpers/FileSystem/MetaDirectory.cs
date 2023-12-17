using System.Text.Json;
using System.Text.Json.Serialization;
using BackupCLI.Helpers.Json;

namespace BackupCLI.Helpers.FileSystem;

/// <summary>
/// Directory associated with a single json metadata file located inside
/// </summary>
/// <typeparam name="TJson">Type that can be serialized or deserialized into a physical file</typeparam>
public abstract class MetaDirectory<TJson> where TJson : class
{
    public DirectoryInfo Folder { get; }
    public virtual string MetadataFileName { get; } = "metadata.json";
    public FileInfo MetadataFile => new(Path.Join(Folder.FullName, MetadataFileName));
    protected List<DirectoryInfo> Subdirectories => [..Folder.GetDirectories()];
    protected virtual JsonSerializerOptions Options { get; set; } = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public TJson Json { get; set; } = null!;

    protected MetaDirectory(DirectoryInfo folder, TJson? @default = default)
    {
        Folder = folder;
        Folder.Create();

        LoadMetadata(@default);
    }

    /// <summary>
    /// Triggered after parsing the json file
    /// </summary>
    protected virtual void OnLoad(TJson json) { }

    /// <summary>
    /// Reads the json file from disk and parses it into <see cref="TJson"/>
    /// </summary>
    /// <param name="default">Fallback value</param>
    public void LoadMetadata(TJson? @default = default)
    {
        Json = @default!;

        if (!MetadataFile.Exists)
        {
            SaveMetadata(Json);
        }
        else if (JsonUtils.TryLoadFile(MetadataFile.FullName, out TJson? output, Options))
        {
            Json = output!;
        }

        OnLoad(Json);
    }

    /// <summary>
    /// Writes the json file to disk
    /// </summary>
    /// <param name="obj">Optional argument - if null, uses the <see cref="Json"/> property instead</param>
    public void SaveMetadata(TJson? obj = null)
    {
        JsonUtils.TryWriteFile(MetadataFile.FullName, obj ?? Json, Options);
    }
}
