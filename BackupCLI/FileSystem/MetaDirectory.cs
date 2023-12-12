using System.Text.Json;
using System.Text.Json.Serialization;
using BackupCLI.Helpers;

namespace BackupCLI.FileSystem;

public abstract class MetaDirectory<TJson> where TJson : class
{
    public DirectoryInfo Folder { get; }
    public string MetadataFileName;
    public FileInfo MetadataFile => new(Path.Join(Folder.FullName, MetadataFileName));
    protected List<DirectoryInfo> Subdirectories => Folder.GetDirectories().ToList();
    protected virtual JsonSerializerOptions Options { get; set; } = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public TJson Json { get; set; } = null!;

    protected MetaDirectory(DirectoryInfo folder, string metadataFileName, TJson? @default = default)
    {
        Folder = folder;
        Folder.Create();

        MetadataFileName = metadataFileName;

        LoadMetadata(@default);
    }

    protected virtual void OnLoad(TJson json) { }

    public void LoadMetadata(TJson? @default = default)
    {
        if (!MetadataFile.Exists)
        {
            Json = @default!;
            SaveMetadata(Json);
        }
        else if (JsonUtils.TryLoadFile(MetadataFile.FullName, out TJson? output, Options))
        {
            Json = output!;
        }

        OnLoad(Json);
    }

    public void SaveMetadata(TJson? obj = null)
    {
        JsonUtils.TryWriteFile(MetadataFile.FullName, obj ?? Json, Options);
    }
}
