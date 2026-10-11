#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class BuffDurationEditorTests
{
    [Test]
    public void ModifierStatDropdownIncludesBuffInfoAndItsDurationField()
    {
        // 실제 드롭다운이 사용하는 목록을 확인합니다. GUI를 열거나 씬 객체를 만들지 않습니다.
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        PropertyInfo statTypes = typeof(BuffModifierEditorGUI).GetProperty("StatTypes", flags);
        var types = (IList<System.Type>)statTypes.GetValue(null);
        Assert.That(types, Does.Contain(typeof(BuffInfo)), "버프 지속시간 타입이 드롭다운에서 제외되면 안 됩니다.");

        MethodInfo getFields = typeof(BuffModifierEditorGUI).GetMethod("GetFloatFields", flags);
        var fields = (List<FieldInfo>)getFields.Invoke(null, new object[] { typeof(BuffInfo) });
        Assert.That(fields.Exists(field => field.Name == nameof(BuffInfo.duration)), Is.True,
            "BuffInfo 선택 후 duration 필드를 고를 수 있어야 합니다.");
    }
}
#endif
