using SQLite;

namespace UmaAsset.Game.Services;

public sealed class MasterDataDatabase : IDisposable
{
    private static bool sqliteInitialized;
    private readonly TemporaryStagedFile stagedMasterFile;

    public MasterDataDatabase(string umaDir)
    {
        stagedMasterFile = GameFileStager.StageMetaFile(Path.Combine(umaDir, "master", "master.mdb"));
    }

    public IReadOnlyDictionary<int, int> GetSkillIconIds(IEnumerable<int> skillIds)
    {
        var ids = skillIds
            .Distinct()
            .OrderBy(static id => id)
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<int, int>();
        }

        using var connection = OpenConnection();
        var parameterNames = ids.Select(static (_, index) => $"?{index + 1}").ToArray();
        var command = $"SELECT id, icon_id FROM skill_data WHERE id IN ({string.Join(", ", parameterNames)})";
        var parameters = ids.Cast<object>().ToArray();

        return connection.Query<SkillIconRow>(command, parameters)
            .ToDictionary(static row => row.Id, static row => row.IconId);
    }

    public IReadOnlyList<MasterSkillRecord> GetAllSkills()
    {
        using var connection = OpenConnection();
        return connection.Query<MasterSkillRecord>(
            """
            SELECT sd.id AS SkillId,
                   sd.icon_id AS IconId,
                   td.text AS NameJa
            FROM skill_data sd
            LEFT JOIN text_data td
              ON td.category = 47
             AND td."index" = sd.id
            ORDER BY sd.id
            """);
    }

    private SQLiteConnection OpenConnection()
    {
        if (!sqliteInitialized)
        {
            SQLitePCL.Batteries_V2.Init();
            sqliteInitialized = true;
        }

        return new SQLiteConnection(stagedMasterFile.Path, SQLiteOpenFlags.ReadOnly);
    }

    public void Dispose()
    {
        stagedMasterFile.Dispose();
    }

    private sealed class SkillIconRow
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("icon_id")]
        public int IconId { get; set; }
    }
}

public sealed class MasterSkillRecord
{
    public int SkillId { get; set; }

    public int IconId { get; set; }

    public string? NameJa { get; set; }
}
