// 개별 텍스트와 여러 표시 요소를 가진 UI 그룹이 같은 씬 중개기를 사용합니다.
public interface ISceneLocalizedUI
{
    void RefreshLanguage();
    void ApplyLanguage(language selected, LocalizedTextTable table);
}
