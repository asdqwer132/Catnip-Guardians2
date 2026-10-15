using System.IO;

public static class GameSceneRoutes
{
    public const string Core = "CoreScene";
    public const string Main = "MainScene";
    public const string General = "GameGeneralScene";
    public const string Battle = "GameBattleScene";
    public const string Item = "GameItemScene";
    public const string Upgrade = "UpgradeScene";
    public const string Story = "StoryScene";

    public static string Resolve(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        name = Path.GetFileNameWithoutExtension(name.Trim());
        // 이전 버튼/튜토리얼 데이터의 씬 이름도 새 진입점으로 보냅니다.
        if (name == "GameScene") return General;
        if (name == "Main") return Main;
        return name;
    }
}
