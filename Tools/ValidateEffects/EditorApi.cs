// Compile-only declarations for the Inspector and asset-driven combat tests. No Editor/GUI tests
// execute against these substitutes; use Unity EditMode for those tests.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    public sealed class InitializeOnLoadAttribute : Attribute { }
    public sealed class CanEditMultipleObjectsAttribute : Attribute { }
    public sealed class CustomEditorAttribute : Attribute { public CustomEditorAttribute(Type type, bool children = false) { } }
    [Flags] public enum GizmoType { Selected = 1, NonSelected = 2 }
    public sealed class DrawGizmoAttribute : Attribute { public DrawGizmoAttribute(GizmoType type) { } }
    public static class Handles { public static void Label(Vector3 position, string text) { } }
    public static class Selection { public static Transform[] transforms => Array.Empty<Transform>(); }
    public sealed class CustomPropertyDrawerAttribute : Attribute { public CustomPropertyDrawerAttribute(Type type) { } }
    public enum MessageType { None, Info, Warning, Error }
    public enum SerializedPropertyType { String }
    public enum PlayModeStateChange { EnteredEditMode, ExitingEditMode, EnteredPlayMode, ExitingPlayMode }
    public class SerializedProperty
    {
        public string name;
        public SerializedProperty Copy() => this;
        public SerializedProperty GetEndProperty() => this;
        public bool NextVisible(bool enterChildren) => false;
        public static bool EqualContents(SerializedProperty a, SerializedProperty b) => false;
        public string stringValue;
        public float floatValue;
        public bool hasMultipleDifferentValues;
        public SerializedPropertyType propertyType;
    }
    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object target) { }
        public void Update() { }
        public bool UpdateIfRequiredOrScript() => false;
        public bool ApplyModifiedProperties() => false;
        public SerializedProperty FindProperty(string name) => null;
    }
    public class Editor : ScriptableObject
    {
        public UnityEngine.Object target;
        public SerializedObject serializedObject;
        public virtual void OnInspectorGUI() { }
        public virtual bool RequiresConstantRepaint() => false;
        protected static void DrawPropertiesExcluding(SerializedObject value, params string[] excluded) { }
    }
    public class PropertyDrawer
    {
        public PropertyAttribute attribute;
        public virtual float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0;
        public virtual void OnGUI(Rect rect, SerializedProperty property, GUIContent label) { }
    }
    public static class EditorApplication
    {
        public static double timeSinceStartup;
        public static event Action hierarchyChanged, projectChanged;
        public static event Action<PlayModeStateChange> playModeStateChanged;
    }
    public static class TypeCache
    {
        public static IEnumerable<Type> GetTypesDerivedFrom<T>() => Array.Empty<Type>();
    }
    public static class EditorUtility { public static bool IsPersistent(UnityEngine.Object value) => false; }
    public static class Undo
    {
        public static event Action undoRedoPerformed;
        public static void IncrementCurrentGroup() { }
        public static void FlushUndoRecordObjects() { }
        public static void PerformUndo() { }
        public static void ClearUndo(UnityEngine.Object value) { }
    }
    public static class AssetDatabase
    {
        public static string[] FindAssets(string filter, string[] folders) => Array.Empty<string>();
        public static string GUIDToAssetPath(string guid) => null;
        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object => null;
        public static string CreateFolder(string parent, string name) => null;
        public static bool DeleteAsset(string path) => false;
    }
    public static class PrefabUtility
    {
        public static bool IsPartOfPrefabAsset(UnityEngine.Object value) => false;
        public static GameObject SaveAsPrefabAsset(GameObject value, string path) => null;
    }
    public static class EditorGUIUtility
    {
        public static float singleLineHeight = 18, standardVerticalSpacing = 2;
    }
    public static class EditorGUI
    {
        public static void BeginDisabledGroup(bool disabled) { }
        public static void EndDisabledGroup() { }
        public static bool showMixedValue;
        public static void BeginProperty(Rect rect, GUIContent label, SerializedProperty property) { }
        public static void EndProperty() { }
        public static void BeginChangeCheck() { }
        public static bool EndChangeCheck() => false;
        public static void LabelField(Rect rect, string label, string value) { }
        public static string TextField(Rect rect, GUIContent label, string value) => value;
        public static int Popup(Rect rect, GUIContent label, int selected, GUIContent[] options) => selected;
    }
    public static class EditorGUILayout
    {
        public static bool PropertyField(SerializedProperty property, bool includeChildren) => false;
        public static bool PropertyField(SerializedProperty property, GUIContent label, bool includeChildren) => false;
        public static float FloatField(string label, float value) => value;
        public static void BeginHorizontal() { }
        public static void EndHorizontal() { }
        public static void Space(float pixels) { }
        public static void LabelField(string label, GUIStyle style) { }
        public static string TextField(string label, string value) => value;
        public static string TextField(GUIContent label, string value) => value;
        public static int Popup(GUIContent label, int selected, GUIContent[] options) => selected;
        public static bool PropertyField(SerializedProperty property, GUIContent label) => false;
        public static void HelpBox(string message, MessageType type) { }
    }
    public static class EditorStyles { public static GUIStyle boldLabel, miniButton; }
    public static class ObjectNames { public static string NicifyVariableName(string value) => value; }
}
namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static bool IsPreviewSceneObject(UnityEngine.Object value) => false;
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup setup, NewSceneMode mode) => default;
        public static bool CloseScene(UnityEngine.SceneManagement.Scene scene, bool removeScene) => false;
    }
}
namespace UnityEditor.Compilation
{
    public enum AssembliesType { Player, Editor }
    public sealed class Assembly { public string name; }
    public static class CompilationPipeline
    {
        public static Assembly[] GetAssemblies(AssembliesType type) => Array.Empty<Assembly>();
    }
}
