#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public class GameDebugWindow : EditorWindow
{
    private enum DebugCategory
    {
        Buff,
        Skill,
        Inventory,
        Enemy,
        Map
    }

    private DebugCategory selectedCategory;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Game Debug")]
    public static void Open()
    {
        GameDebugWindow window = GetWindow<GameDebugWindow>();
        window.titleContent = new GUIContent("Game Debug");
        window.minSize = new Vector2(450f, 350f);
        window.Show();
    }

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.Space(10);

        scrollPosition =
            EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedCategory)
        {
            case DebugCategory.Buff:
                DrawBuffDebug();
                break;

            case DebugCategory.Skill:
                DrawSkillDebug();
                break;

            case DebugCategory.Inventory:
                DrawInventoryDebug();
                break;

            case DebugCategory.Enemy:
                DrawEnemyDebug();
                break;

            case DebugCategory.Map:
                DrawMapDebug();
                break;
        }

        EditorGUILayout.EndScrollView();

        if (Application.isPlaying)
            Repaint();
    }

    private void DrawToolbar()
    {
        selectedCategory = (DebugCategory)GUILayout.Toolbar(
            (int)selectedCategory,
            new[]
            {
                "Buff",
                "Skill",
                "Inventory",
                "Enemy",
                "Map"
            }
        );
    }

    private void DrawBuffDebug()
    {
        EditorGUILayout.LabelField(
            "Buff Debug",
            EditorStyles.boldLabel
        );

        BuffManager buffManager =
            Object.FindAnyObjectByType<BuffManager>();

        if (buffManager == null)
        {
            EditorGUILayout.HelpBox(
                "현재 씬에서 BuffManager를 찾을 수 없습니다.",
                MessageType.Warning
            );

            return;
        }

        EditorGUILayout.ObjectField(
            "Buff Manager",
            buffManager,
            typeof(BuffManager),
            true
        );

        EditorGUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        if (GUILayout.Button(
            "전체 버프 클리어",
            GUILayout.Height(30)))
        {
            buffManager.ClearAllBuffs();
        }

        if (GUILayout.Button(
            "일반 버프만 클리어",
            GUILayout.Height(30)))
        {
            buffManager.ClearNormalBuffs();
        }

        if (GUILayout.Button(
            "무한 버프만 클리어",
            GUILayout.Height(30)))
        {
            buffManager.ClearInfiniteBuffs();
        }

        EditorGUI.EndDisabledGroup();

        DrawPlayModeHelpBox();
    }

    private void DrawSkillDebug()
    {
        EditorGUILayout.LabelField(
            "Skill Debug",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "스킬 테스트 기능을 여기에 추가합니다.",
            MessageType.Info
        );
    }

    private void DrawInventoryDebug()
    {
        EditorGUILayout.LabelField(
            "Inventory Debug",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "인벤토리 테스트 기능을 여기에 추가합니다.",
            MessageType.Info
        );
    }

    private void DrawEnemyDebug()
    {
        EditorGUILayout.LabelField(
            "Enemy Debug",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "적 테스트 기능을 여기에 추가합니다.",
            MessageType.Info
        );
    }

    private void DrawMapDebug()
    {
        EditorGUILayout.LabelField(
            "Map Debug",
            EditorStyles.boldLabel
        );

        TilemapRadialSequenceController mapController =
            Object.FindAnyObjectByType<
                TilemapRadialSequenceController
            >();

        if (mapController == null)
        {
            EditorGUILayout.HelpBox(
                "현재 씬에서 TilemapRadialSequenceController를 찾을 수 없습니다.",
                MessageType.Warning
            );

            return;
        }

        EditorGUILayout.ObjectField(
            "Map Controller",
            mapController,
            typeof(TilemapRadialSequenceController),
            true
        );

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField(
            "현재 맵 인덱스",
            mapController.CurrentMapIndex.ToString()
        );

        EditorGUILayout.LabelField(
            "전환 실행 중",
            mapController.IsPlaying ? "Yes" : "No"
        );

        EditorGUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(
            !Application.isPlaying ||
            mapController.IsPlaying
        );

        if (GUILayout.Button(
            "다음 맵 재생",
            GUILayout.Height(40)))
        {
            mapController.PlayNext();
        }

        EditorGUI.EndDisabledGroup();

        DrawPlayModeHelpBox();
    }

    private void DrawPlayModeHelpBox()
    {
        if (Application.isPlaying)
            return;

        EditorGUILayout.HelpBox(
            "플레이 모드에서만 실행할 수 있습니다.",
            MessageType.Info
        );
    }
}

#endif