using System.IO;

// 저장소를 분리해 세이브 슬롯/클라우드 저장소로 교체할 수 있습니다.
public interface IMergeBoxUpgradeSaveStore
{
    string Read();
    void Write(string json);
}

public sealed class MergeBoxUpgradeFileStore : IMergeBoxUpgradeSaveStore
{
    private readonly string path;
    public MergeBoxUpgradeFileStore(string path) => this.path = path;
    public string Read() => File.Exists(path) ? File.ReadAllText(path) : null;
    public void Write(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, json);
        if (File.Exists(path)) File.Replace(temporary, path, null);
        else File.Move(temporary, path);
    }
}
