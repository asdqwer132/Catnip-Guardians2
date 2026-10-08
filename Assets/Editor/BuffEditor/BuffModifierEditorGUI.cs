#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// BuffModifier의 기존 문자열 직렬화는 유지하고, Editor에서만 타입/float 필드 목록을 제공한다.
/// </summary>
internal static class BuffModifierEditorGUI
{
    private const BindingFlags PublicFloatFields = BindingFlags.Public | BindingFlags.Instance;
    private static List<Type> availableTypes;

    private static List<Type> StatTypes
    {
        get
        {
            if (availableTypes == null)
                availableTypes = FindStatTypes();
            return availableTypes;
        }
    }

    // 스탯 클래스 또는 필드를 새로 만든 후 명시적으로 목록을 다시 계산할 때 사용한다.
    // 씬 오브젝트를 조회하지 않고, 로드된 C# 어셈블리의 타입 정보만 확인한다.
    private static void RefreshStatTypes()
    {
        availableTypes = FindStatTypes();
    }

    private static List<Type> FindStatTypes()
    {
        var result = new List<Type>();
        var seen = new HashSet<string>();
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int a = 0; a < assemblies.Length; a++)
        {
            Type[] types;
            try { types = assemblies[a].GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types; }
            catch { continue; }

            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type == null || !type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
                    continue;
                if (type == typeof(BuffInfo) || !IsGameStat(type))
                    continue;
                if (GetFloatFields(type).Count == 0)
                    continue;
                if (type.FullName != null && seen.Add(type.FullName))
                    result.Add(type);
            }
        }
        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return result;
    }

    private static bool IsGameStat(Type type)
    {
        Type[] interfaces = type.GetInterfaces();
        for (int i = 0; i < interfaces.Length; i++)
        {
            Type contract = interfaces[i];
            if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IGameStat<>) &&
                contract.GetGenericArguments()[0] == type)
                return true;
        }
        return false;
    }

    private static List<FieldInfo> GetFloatFields(Type type)
    {
        var fields = new List<FieldInfo>();
        foreach (FieldInfo field in type.GetFields(PublicFloatFields))
        {
            // 런타임의 FloatFieldBuffModifier가 수정할 수 있는 필드만 노출한다.
            if (field.FieldType == typeof(float) && !field.IsInitOnly && !field.IsLiteral)
                fields.Add(field);
        }
        fields.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return fields;
    }

    private static Type FindType(string serializedName)
    {
        if (string.IsNullOrEmpty(serializedName))
            return null;
        for (int i = 0; i < StatTypes.Count; i++)
        {
            Type type = StatTypes[i];
            if (type.Name == serializedName || type.FullName == serializedName)
                return type;
        }
        return null;
    }

    internal static string Summary(BuffModifier modifier)
    {
        if (modifier == null)
            return "모디파이어를 연결하거나 새로 만드세요";

        FloatFieldBuffModifier floatModifier = modifier as FloatFieldBuffModifier;
        if (floatModifier == null)
            return modifier.GetType().Name;

        string type = string.IsNullOrEmpty(floatModifier.targetStatTypeName)
            ? "전체 스탯" : floatModifier.targetStatTypeName;
        string field = string.IsNullOrEmpty(floatModifier.fieldName)
            ? "(필드 미설정)" : floatModifier.fieldName;
        return type + "." + field + "   [더하기 " +
               floatModifier.addValue.ToString("+0.###;-0.###;0") +
               " | 곱하기 " + floatModifier.multiplyValue.ToString("+0.###;-0.###;0") + "]";
    }

    internal static void DrawFloatModifier(SerializedObject serializedModifier)
    {
        serializedModifier.UpdateIfRequiredOrScript();
        SerializedProperty typeName = serializedModifier.FindProperty("targetStatTypeName");
        SerializedProperty fieldName = serializedModifier.FindProperty("fieldName");
        SerializedProperty add = serializedModifier.FindProperty("addValue");
        SerializedProperty multiply = serializedModifier.FindProperty("multiplyValue");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("적용할 스탯과 필드", EditorStyles.boldLabel);
        if (GUILayout.Button(new GUIContent("목록 새로고침", "IGameStat<T> 구현 타입과 public float 필드를 다시 확인합니다."),
                EditorStyles.miniButton, GUILayout.Width(100f)))
            RefreshStatTypes();
        EditorGUILayout.EndHorizontal();
        DrawStatType(typeName, fieldName);
        Type selectedType = FindType(typeName.stringValue);
        if (selectedType == null)
        {
            if (string.IsNullOrEmpty(typeName.stringValue))
                EditorGUILayout.HelpBox("새 모디파이어는 먼저 스탯 종류를 선택하세요. '전체 스탯'은 같은 필드명을 가진 모든 스탯에 적용됩니다.", MessageType.Info);
            else
            {
                EditorGUILayout.HelpBox("기존 스탯 이름을 찾지 못했습니다. 원래 문자열은 보존됩니다. 직접 수정하거나 드롭다운에서 새 타입을 선택하세요.", MessageType.Warning);
                typeName.stringValue = EditorGUILayout.TextField("기존 스탯 문자열", typeName.stringValue);
            }

            fieldName.stringValue = EditorGUILayout.TextField(new GUIContent("필드명 (직접 입력)"), fieldName.stringValue);
        }
        else
        {
            DrawStatField(selectedType, fieldName);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("변경 수치", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(add, new GUIContent("더하기 (+)", "중첩 시 스택 수만큼 더합니다."));
        EditorGUILayout.PropertyField(multiply, new GUIContent("곱하기 배율 변화", "0.5 = +50% (x1.5), 1 = +100% (x2). 기존 저장값과 계산 방식은 그대로 유지됩니다."));
        EditorGUILayout.HelpBox("곱하기 증가량은 0.5 = 1.5배, 1 = 2배입니다. 모든 더하기를 먼저 적용한 뒤 모든 곱하기를 적용합니다.", MessageType.None);
        if (add.floatValue == 0f && multiply.floatValue == 0f)
            EditorGUILayout.HelpBox("더하기와 곱하기가 모두 0이므로 이 모디파이어는 수치를 변경하지 않습니다.", MessageType.Warning);
        serializedModifier.ApplyModifiedProperties();
    }

    private static void DrawStatType(SerializedProperty typeName, SerializedProperty fieldName)
    {
        var names = new List<GUIContent> { new GUIContent("전체 스탯 (필드명 직접 입력)") };
        int selected = string.IsNullOrEmpty(typeName.stringValue) ? 0 : -1;
        for (int i = 0; i < StatTypes.Count; i++)
        {
            Type type = StatTypes[i];
            names.Add(new GUIContent(type.Name, type.FullName));
            if (typeName.stringValue == type.Name || typeName.stringValue == type.FullName)
                selected = i + 1;
        }

        // 삭제되었거나 새로 추가된 타입 명칭은 사용자가 재선택하기 전까지 보존한다.
        if (selected < 0)
        {
            selected = names.Count;
            names.Add(new GUIContent("기존 값 유지: " + typeName.stringValue));
        }

        int next = EditorGUILayout.Popup(new GUIContent("스탯 종류"), selected, names.ToArray());
        if (next == selected || next >= StatTypes.Count + 1)
            return;

        typeName.stringValue = next == 0 ? string.Empty : StatTypes[next - 1].FullName;
        if (next == 0)
            return; // 전체 스탯을 선택할 때 기존 필드명은 유지한다.

        List<FieldInfo> fields = GetFloatFields(StatTypes[next - 1]);
        bool fieldFound = false;
        for (int i = 0; i < fields.Count; i++)
        {
            if (fields[i].Name == fieldName.stringValue)
                fieldFound = true;
        }
        if (!fieldFound)
            fieldName.stringValue = fields.Count > 0 ? fields[0].Name : string.Empty;
    }

    private static void DrawStatField(Type selectedType, SerializedProperty fieldName)
    {
        List<FieldInfo> fields = GetFloatFields(selectedType);
        var names = new List<GUIContent>();
        int selected = -1;
        for (int i = 0; i < fields.Count; i++)
        {
            FieldInfo field = fields[i];
            TooltipAttribute tooltip = Attribute.GetCustomAttribute(field, typeof(TooltipAttribute)) as TooltipAttribute;
            string hint = tooltip != null ? tooltip.tooltip : field.Name;
            names.Add(new GUIContent(ObjectNames.NicifyVariableName(field.Name) + "  (" + field.Name + ")", hint));
            if (field.Name == fieldName.stringValue)
                selected = i;
        }
        if (selected < 0)
        {
            selected = names.Count;
            names.Add(new GUIContent(string.IsNullOrEmpty(fieldName.stringValue)
                ? "필드를 선택하세요" : "미확인 필드: " + fieldName.stringValue));
        }

        int next = EditorGUILayout.Popup(new GUIContent("수정할 필드"), selected, names.ToArray());
        if (next < fields.Count && next != selected)
            fieldName.stringValue = fields[next].Name;

        if (next >= fields.Count)
            EditorGUILayout.HelpBox("현재 필드가 선택된 스탯에 존재하지 않습니다. 드롭다운에서 유효한 float 필드를 선택하세요.", MessageType.Warning);
    }
}
#endif
