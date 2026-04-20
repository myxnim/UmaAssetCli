using SQLite;

namespace UmaAssetCli.Models;

[Table("a")]
public sealed class ManifestEntry
{
    [Column("i"), PrimaryKey]
    public int Id { get; set; }

    [Column("n"), NotNull]
    public string Name { get; set; } = string.Empty;

    [Column("d")]
    public string Dependencies { get; set; } = string.Empty;

    [Column("g"), NotNull]
    public AssetBundleGroup Group { get; set; }

    [Column("l"), NotNull]
    public long Length { get; set; }

    [Column("c"), NotNull]
    public long Checksum { get; set; }

    [Column("h"), NotNull]
    public string HashName { get; set; } = string.Empty;

    [Column("m"), NotNull]
    public string Manifest { get; set; } = string.Empty;

    [Column("k"), NotNull]
    public ManifestEntryKind Kind { get; set; }

    [Column("s"), NotNull]
    public byte State { get; set; }

    [Column("p"), NotNull]
    public int Priority { get; set; }

    [Column("e"), NotNull]
    public long EncryptionKey { get; set; }

    public string BaseName
    {
        get
        {
            var lastSlash = Name.LastIndexOf('/');
            return Name[(lastSlash + 1)..];
        }
    }
}
