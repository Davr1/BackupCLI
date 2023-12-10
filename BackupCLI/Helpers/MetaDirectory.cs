using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackupCLI.Helpers;

public abstract class MetaDirectory<TJson> where TJson : class
{
    public DirectoryInfo Folder { get; }
    protected string MetadataFileName;
    protected string MetadataFilePath => Path.Join(Folder.FullName, MetadataFileName);
    protected List<DirectoryInfo> Subdirectories => Folder.GetDirectories().ToList();
    protected virtual JsonSerializerOptions Options { get; set; } = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected MetaDirectory(DirectoryInfo folder, string metadataFileName = "meta.json", TJson? @default = default)
    {
        Folder = folder;
        Folder.Create();

        MetadataFileName = metadataFileName;

        LoadMetadata(@default);
    }

    protected abstract void SetProperties(TJson? json);

    public void LoadMetadata(TJson? @default = default)
    {
        if (!File.Exists(MetadataFilePath))
        {
            SetProperties(@default);
            SaveMetadata(@default);
        }
        else
        {
            JsonUtils.TryLoadFile(MetadataFilePath, out TJson? output, Options);
            SetProperties(output);
        }
    }

    public void SaveMetadata(TJson obj)
    {
        JsonUtils.TryWriteFile(MetadataFilePath, obj, Options);
    }
}