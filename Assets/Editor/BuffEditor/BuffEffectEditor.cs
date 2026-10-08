#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// BuffEffect를 선택한 상태에서 여러 Modifier를 만들고 바로 설정하는 에디터.
/// 기존 BuffModifier[]와 SO 참조 직렬화 방식은 변경하지 않는다.
/// </summary>
[CustomEditor(typeof(BuffEffect))]
public sealed class BuffEffectEditor : Editor
{
    private ReorderableList modifierList;
    private readonly Dictionary<BuffModifier, Editor> editors = new Dictionary<BuffModifier, Editor>();

    private void OnEnable()
    {
        modifierList = new ReorderableList(serializedObject, serializedObject.FindProperty("modifiers"),
            true, true, true, true);
        modifierList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "모디파이어 목록 (드래그로 순서 변경)");
        modifierList.elementHeight = EditorGUIUtility.singleLineHeight * 2 + 7;
        modifierList.drawElementCallback = DrawElement;
        modifierList.onAddDropdownCallback = ShowAddMenu;
        modifierList.onRemoveCallback = RemoveModifier;
    }

    private void OnDisable()
    {
        foreach (Editor cachedEditor in editors.Values)
        {
            if (cachedEditor != null)
                DestroyImmediate(cachedEditor);
        }
        editors.Clear();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "modifiers");
        EditorGUILayout.Space(8);
        modifierList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();

        SerializedProperty array = serializedObject.FindProperty("modifiers");
        if (array.arraySize == 0)
        {
            EditorGUILayout.HelpBox("우측 + 버튼으로 새 Float 모디파이어를 이 버프 안에 만들거나 기존 에셋을 연결할 수 있습니다.", MessageType.Info);
            return;
        }

        int index = modifierList.index;
        if (index < 0 || index >= array.arraySize)
        {
            EditorGUILayout.HelpBox("설정할 모디파이어를 목록에서 선택하세요.", MessageType.None);
            return;
        }

        BuffModifier modifier = array.GetArrayElementAtIndex(index).objectReferenceValue as BuffModifier;
        if (modifier == null)
        {
            EditorGUILayout.HelpBox("비어 있는 항목입니다. 위의 Object 필드에 기존 모디파이어 에셋을 연결하세요.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("선택한 모디파이어 " + (index + 1), EditorStyles.boldLabel);

        bool embedded = IsEmbeddedInCurrentBuff(modifier);
        if (!embedded)
        {
            EditorGUILayout.HelpBox("외부 모디파이어 에셋입니다. 여기서 수정하면 이 에셋을 참조하는 다른 버프에도 변경사항이 적용됩니다.", MessageType.Warning);
            if (GUILayout.Button("이 버프 전용으로 복제하고 연결하기"))
            {
                CreateEmbeddedModifier(modifier);
                EditorGUILayout.EndVertical();
                return;
            }
        }
        else
        {
            EditorGUILayout.LabelField("이 버프에 포함된 모디파이어", EditorStyles.miniLabel);
            if (GUILayout.Button("선택한 설정 복제해서 추가하기"))
            {
                CreateEmbeddedModifier(modifier, true);
                EditorGUILayout.EndVertical();
                return;
            }
        }

        Editor modifierEditor;
        if (!editors.TryGetValue(modifier, out modifierEditor) || modifierEditor == null)
        {
            modifierEditor = CreateEditor(modifier);
            editors[modifier] = modifierEditor;
        }
        if (modifierEditor != null)
            modifierEditor.OnInspectorGUI();

        EditorGUILayout.EndVertical();
    }

    private void DrawElement(Rect rect, int index, bool active, bool focused)
    {
        SerializedProperty array = modifierList.serializedProperty;
        if (index >= array.arraySize)
            return;

        SerializedProperty element = array.GetArrayElementAtIndex(index);
        BuffModifier modifier = element.objectReferenceValue as BuffModifier;
        float line = EditorGUIUtility.singleLineHeight;
        rect.y += 2;
        Rect objRect = new Rect(rect.x, rect.y, rect.width, line);
        EditorGUI.PropertyField(objRect, element, new GUIContent("#" + (index + 1)), true);

        Rect summaryRect = new Rect(rect.x + 14, rect.y + line + 2, rect.width - 14, line);
        EditorGUI.LabelField(summaryRect, BuffModifierEditorGUI.Summary(modifier), EditorStyles.miniLabel);
    }

    private void ShowAddMenu(Rect buttonRect, ReorderableList list)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("새 Float 필드 모디파이어 (버프에 내장)"), false,
            () => CreateEmbeddedModifier(null));
        menu.AddItem(new GUIContent("기존 모디파이어 에셋 연결"), false, AddReferenceSlot);
        menu.DropDown(buttonRect);
    }

    private void AddReferenceSlot()
    {
        serializedObject.Update();
        SerializedProperty array = serializedObject.FindProperty("modifiers");
        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        array.GetArrayElementAtIndex(index).objectReferenceValue = null;
        serializedObject.ApplyModifiedProperties();
        modifierList.index = index;
    }

    private bool IsEmbeddedInCurrentBuff(BuffModifier modifier)
    {
        if (modifier == null || !AssetDatabase.IsSubAsset(modifier))
            return false;
        string currentPath = AssetDatabase.GetAssetPath(target);
        return !string.IsNullOrEmpty(currentPath) && AssetDatabase.GetAssetPath(modifier) == currentPath;
    }

    // source=null => 새로운 FloatFieldBuffModifier 생성.
    // replaceSelection=true => 목록의 기존 참조를 전용 복제로 교체하지 않고, 새 슬롯에 복제 추가.
    private void CreateEmbeddedModifier(BuffModifier source, bool addAsNewSlot = false)
    {
        BuffEffect effect = (BuffEffect)target;
        if (!AssetDatabase.Contains(effect))
        {
            EditorUtility.DisplayDialog("버프 저장 필요", "먼저 BuffEffect를 프로젝트 에셋으로 저장한 후 모디파이어를 추가하세요.", "확인");
            return;
        }

        BuffModifier created = source == null
            ? (BuffModifier)CreateInstance<FloatFieldBuffModifier>()
            : Instantiate(source);
        created.name = source == null ? "Float Modifier" : source.name + " Copy";

        Undo.RegisterCreatedObjectUndo(created, "버프 모디파이어 생성");
        AssetDatabase.AddObjectToAsset(created, effect);

        serializedObject.Update();
        SerializedProperty array = serializedObject.FindProperty("modifiers");
        int selected = modifierList.index;
        bool replace = source != null && !addAsNewSlot && selected >= 0 && selected < array.arraySize;
        if (!replace)
        {
            selected = array.arraySize;
            array.InsertArrayElementAtIndex(selected);
        }
        array.GetArrayElementAtIndex(selected).objectReferenceValue = created;
        serializedObject.ApplyModifiedProperties();
        modifierList.index = selected;

        EditorUtility.SetDirty(effect);
        EditorUtility.SetDirty(created);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(effect));
    }

    private void RemoveModifier(ReorderableList list)
    {
        serializedObject.Update();
        SerializedProperty array = serializedObject.FindProperty("modifiers");
        int index = list.index;
        if (index < 0 || index >= array.arraySize)
            return;
        BuffModifier modifier = array.GetArrayElementAtIndex(index).objectReferenceValue as BuffModifier;

        bool deleteSubAsset = false;
        if (IsEmbeddedInCurrentBuff(modifier))
        {
            bool referencedAgain = false;
            for (int i = 0; i < array.arraySize; i++)
            {
                if (i != index && array.GetArrayElementAtIndex(i).objectReferenceValue == modifier)
                    referencedAgain = true;
            }
            if (!referencedAgain)
            {
                int choice = EditorUtility.DisplayDialogComplex("내장 모디파이어 제거",
                    "이 모디파이어는 BuffEffect 안에 저장되어 있습니다. 다른 에셋도 참조할 수 있으므로 내부 데이터 삭제는 신중하게 선택하세요.",
                    "목록에서만 제거", "내부 데이터까지 삭제", "취소");
                if (choice == 2)
                    return;
                deleteSubAsset = choice == 1;
            }
        }

        // ObjectReference 배열의 기본 DeleteArrayElementAtIndex는 참조만 null로 만들 수 있으므로 명시적으로 한 칸 제거.
        array.GetArrayElementAtIndex(index).objectReferenceValue = null;
        array.DeleteArrayElementAtIndex(index);
        serializedObject.ApplyModifiedProperties();
        if (deleteSubAsset && modifier != null)
        {
            Editor modifierEditor;
            if (editors.TryGetValue(modifier, out modifierEditor))
            {
                if (modifierEditor != null)
                    DestroyImmediate(modifierEditor);
                editors.Remove(modifier);
            }
            Undo.DestroyObjectImmediate(modifier);
            AssetDatabase.SaveAssets();
        }
        list.index = Mathf.Clamp(index, -1, array.arraySize - 1);
    }
}
#endif
