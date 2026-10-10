using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

// 그룹 선언을 읽을 때 컴포넌트 생성자나 getter를 실행하지 않는다.
public static class BuffTargetGroupDiscovery
{
    private struct Instruction
    {
        public OpCode code;
        public int token;
    }

    private static readonly Dictionary<short, OpCode> codes = BuildOpCodes();

    public static string[] GetDeclaredGroups(Type type)
    {
        var groups = new SortedSet<string>(StringComparer.Ordinal);
        if (type == null || !typeof(IBuffTarget).IsAssignableFrom(type) || type.ContainsGenericParameters)
            return new string[0];
        var declared = (BuffTargetGroupsAttribute)Attribute.GetCustomAttribute(type, typeof(BuffTargetGroupsAttribute), true);
        if (declared != null)
            foreach (string group in declared.Groups) AddGroup(groups, group);

        try
        {
            InterfaceMapping map = type.GetInterfaceMap(typeof(IBuffTarget));
            for (int i = 0; i < map.InterfaceMethods.Length; i++)
                if (map.InterfaceMethods[i].Name == "get_BuffTargetGroup")
                    ReadGetter(groups, map.TargetMethods[i]);
        }
        catch (ArgumentException) { }
        catch (NotSupportedException) { }
        var result = new string[groups.Count];
        groups.CopyTo(result);
        return result;
    }

    public static void AddGroup(ISet<string> groups, string value)
    {
        string group = BuffTargetHandle.NormalizeGroupName(value);
        while (!string.IsNullOrEmpty(group))
        {
            groups.Add(group);
            int separator = group.LastIndexOf('/');
            if (separator < 0) break;
            group = group.Substring(0, separator);
        }
    }

    private static void ReadGetter(ISet<string> groups, MethodInfo getter)
    {
        List<Instruction> instructions = ReadInstructions(getter);
        Instruction? value = null;
        foreach (Instruction instruction in instructions)
        {
            OpCode code = instruction.code;
            if (code == OpCodes.Ldstr || code == OpCodes.Ldfld || code == OpCodes.Ldsfld)
            {
                if (value.HasValue) return;
                value = instruction;
            }
            else if (code != OpCodes.Nop && code != OpCodes.Ret && code != OpCodes.Ldarg_0 &&
                code != OpCodes.Br && code != OpCodes.Br_S &&
                code != OpCodes.Ldloc_0 && code != OpCodes.Ldloc_1 && code != OpCodes.Ldloc_2 && code != OpCodes.Ldloc_3 &&
                code != OpCodes.Stloc_0 && code != OpCodes.Stloc_1 && code != OpCodes.Stloc_2 && code != OpCodes.Stloc_3 &&
                code != OpCodes.Ldloc && code != OpCodes.Ldloc_S && code != OpCodes.Stloc && code != OpCodes.Stloc_S)
                return;
        }
        if (!value.HasValue) return;
        if (value.Value.code == OpCodes.Ldstr)
        {
            AddGroup(groups, getter.Module.ResolveString(value.Value.token));
            return;
        }

        FieldInfo field = ResolveField(getter, value.Value.token);
        if (field.FieldType != typeof(string)) return;
        if (field.IsStatic)
            ReadFieldInitializer(groups, field, field.DeclaringType.TypeInitializer);
        else
            foreach (ConstructorInfo constructor in field.DeclaringType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                ReadFieldInitializer(groups, field, constructor);
    }

    private static void ReadFieldInitializer(ISet<string> groups, FieldInfo field, ConstructorInfo constructor)
    {
        if (constructor == null) return;
        List<Instruction> instructions = ReadInstructions(constructor);
        for (int i = 1; i < instructions.Count; i++)
        {
            Instruction instruction = instructions[i];
            if ((instruction.code == OpCodes.Stfld || instruction.code == OpCodes.Stsfld) &&
                instructions[i - 1].code == OpCodes.Ldstr && ResolveField(constructor, instruction.token) == field)
                AddGroup(groups, constructor.Module.ResolveString(instructions[i - 1].token));
        }
    }

    private static FieldInfo ResolveField(MethodBase method, int token)
        => method.Module.ResolveField(token, method.DeclaringType.GetGenericArguments(),
            method is MethodInfo info ? info.GetGenericArguments() : Type.EmptyTypes);

    private static Dictionary<short, OpCode> BuildOpCodes()
    {
        var result = new Dictionary<short, OpCode>();
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.FieldType == typeof(OpCode))
            {
                var code = (OpCode)field.GetValue(null);
                result[code.Value] = code;
            }
        return result;
    }

    private static List<Instruction> ReadInstructions(MethodBase method)
    {
        var result = new List<Instruction>();
        byte[] body = method.GetMethodBody()?.GetILAsByteArray();
        if (body == null) return result;
        int position = 0;
        while (position < body.Length)
        {
            int value = body[position++];
            if (value == 0xfe) value = 0xfe00 | body[position++];
            OpCode code = codes[unchecked((short)value)];
            int size;
            switch (code.OperandType)
            {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8:
                case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch: size = 4 + 4 * BitConverter.ToInt32(body, position); break;
                default: size = 4; break;
            }
            result.Add(new Instruction { code = code, token = size == 4 ? BitConverter.ToInt32(body, position) : 0 });
            position += size;
        }
        return result;
    }
}
