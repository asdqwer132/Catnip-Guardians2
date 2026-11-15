using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

// Typed conversion is shared by all Extra sheets, including nested serializable lists.
public static class DataWorkbookValue
{
    public static FieldInfo Field(Type type, string name) => type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .FirstOrDefault(field => !field.IsStatic && !field.IsInitOnly && !field.IsDefined(typeof(NonSerializedAttribute))
            && (field.IsPublic || field.IsDefined(typeof(SerializeField))) && string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase));

    public static bool IsClear(string text) => text.Trim().Equals("null", StringComparison.OrdinalIgnoreCase);
    public static bool IsKeep(string text) => string.IsNullOrWhiteSpace(text) || text.Trim() == "-";

    public static object Parse(Type type, string text)
    {
        string value = text.Trim();
        if (IsClear(value)) return Empty(type);
        if (type == typeof(string)) return text;
        if (value.StartsWith("[", StringComparison.Ordinal) || value.StartsWith("{", StringComparison.Ordinal))
        {
            if (Encoding.UTF8.GetByteCount(value) > 4 * 1024 * 1024) throw new InvalidDataException("JSON 셀은 4MiB 이하로 입력하세요.");
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(value), XmlDictionaryReaderQuotas.Max))
                return Json(type, XElement.Load(reader));
        }
        if (typeof(UObject).IsAssignableFrom(type)) return Reference(type, value);
        if (type.IsArray || IsList(type))
        {
            Type element = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            if (element == typeof(Cost))
                return Collection(type, value.Split(';').Where(token => !string.IsNullOrWhiteSpace(token)).Select(token =>
                {
                    var pair = token.Split(':');
                    if (pair.Length != 2) throw new InvalidDataException("비용 형식은 CurrencyMain:10;CurrencySub1:2 입니다: " + token);
                    int amount = (int)Parse(typeof(int), pair[1]);
                    if (amount < 0) throw new InvalidDataException("비용은 0 이상이어야 합니다.");
                    return (object)new Cost((CurrencyType)Parse(typeof(CurrencyType), pair[0]), amount);
                }).ToArray());
            return Collection(type, value.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(token => Parse(element, token.Trim())).ToArray());
        }
        if (type.IsEnum)
        {
            object parsed;
            try { parsed = Enum.Parse(type, value, true); }
            catch (ArgumentException) { throw new InvalidDataException(type.Name + " 값이 아닙니다: " + value); }
            if (!Enum.IsDefined(type, parsed)) throw new InvalidDataException(type.Name + "에 정의되지 않은 값입니다: " + value);
            return parsed;
        }
        if (type == typeof(bool))
        {
            if (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            throw new InvalidDataException("불값은 true/false 또는 1/0으로 입력하세요: " + value);
        }
        if (type.IsPrimitive || type == typeof(decimal))
        {
            object parsed = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            if (parsed is float f && (float.IsNaN(f) || float.IsInfinity(f)) || parsed is double d && (double.IsNaN(d) || double.IsInfinity(d)))
                throw new InvalidDataException("유한한 수를 입력하세요: " + value);
            return parsed;
        }
        throw new InvalidDataException(type.Name + "은 JSON 객체/배열로 입력하세요.");
    }

    public static void Validate(FieldInfo field, object value)
    {
        if (value == null) return;
        var min = field.GetCustomAttribute<MinAttribute>();
        var range = field.GetCustomAttribute<RangeAttribute>();
        if (min == null && range == null) return;
        double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (min != null && number < min.min) throw new InvalidDataException(field.Name + "의 최소값은 " + min.min + "입니다.");
        if (range != null && (number < range.min || number > range.max)) throw new InvalidDataException(field.Name + "의 범위는 " + range.min + " ~ " + range.max + "입니다.");
    }

    private static bool IsList(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>);
    private static object Empty(Type type)
    {
        if (type == typeof(string)) return "";
        if (type.IsArray) return Array.CreateInstance(type.GetElementType(), 0);
        if (IsList(type)) return Activator.CreateInstance(type);
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
    private static object Collection(Type type, object[] values)
    {
        Type element = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
        var array = Array.CreateInstance(element, values.Length);
        for (int i = 0; i < values.Length; i++) array.SetValue(values[i], i);
        if (type.IsArray) return array;
        var list = (IList)Activator.CreateInstance(type);
        foreach (object value in array) list.Add(value);
        return list;
    }

    private static object Json(Type type, XElement node)
    {
        string kind = (string)node.Attribute("type");
        if (kind == "null")
        {
            if (type.IsValueType) throw new InvalidDataException(type.Name + "에 null을 사용할 수 없습니다.");
            return null;
        }
        if (type.IsArray || IsList(type))
        {
            if (kind != "array") throw new InvalidDataException(type.Name + "에 JSON 배열이 필요합니다.");
            Type element = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            return Collection(type, node.Elements().Select(child => Json(element, child)).ToArray());
        }
        if (kind == "object")
        {
            if (typeof(UObject).IsAssignableFrom(type) || type.IsPrimitive || type.IsEnum || type == typeof(string) || !type.IsSerializable)
                throw new InvalidDataException(type.Name + "은 객체 형식으로 입력할 수 없습니다. 에셋 참조는 경로나 ID를 사용하세요.");
            object result = JsonUtility.FromJson("{}", type);
            if (result == null) throw new InvalidDataException(type.Name + " JSON을 생성할 수 없습니다.");
            foreach (var child in node.Elements())
            {
                string name = XmlConvert.DecodeName(child.Name.LocalName);
                var field = Field(type, name) ?? throw new InvalidDataException(type.Name + "에 없는 JSON 필드입니다: " + name);
                object value = Json(field.FieldType, child);
                Validate(field, value);
                field.SetValue(result, value);
            }
            return result;
        }
        if (kind == "array") throw new InvalidDataException(type.Name + "은 배열이 아닙니다.");
        return Parse(type, node.Value);
    }

    private static UObject Reference(Type type, string locator)
    {
        if (locator.StartsWith("guid:", StringComparison.OrdinalIgnoreCase)) locator = AssetDatabase.GUIDToAssetPath(locator.Substring(5));
        if (locator.StartsWith("Assets/", StringComparison.Ordinal))
        {
            var parts = locator.Replace('\\', '/').Split('#');
            var assets = AssetDatabase.LoadAllAssetsAtPath(parts[0]).Where(asset => type.IsInstanceOfType(asset)
                && (parts.Length == 1 || asset.name == parts[1])).ToArray();
            if (assets.Length != 1) throw new InvalidDataException(type.Name + " 참조가 없거나 여러 개입니다 (경로#서브에셋이름): " + locator);
            return assets[0];
        }
        if (typeof(DefaultData).IsAssignableFrom(type))
        {
            var assets = AssetDatabase.FindAssets("t:" + type.Name)
                .SelectMany(guid => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid))).OfType<DefaultData>()
                .Where(asset => type.IsInstanceOfType(asset) && asset.dataId == locator).Distinct().ToArray();
            if (assets.Length == 1) return assets[0];
            throw new InvalidDataException(type.Name + " ID가 없거나 중복됩니다: " + locator + (assets.Length > 1 ? " (에셋 경로로 지정하세요)" : ""));
        }
        throw new InvalidDataException(type.Name + "의 Assets/... 에셋 경로가 필요합니다: " + locator);
    }
}
