using System.Reflection;
using UnityEngine;

[CreateAssetMenu(fileName = "Float Field Buff Modifier", menuName = "GameData/Buff/Modifier/Float Field")]
public class FloatFieldBuffModifier : BuffModifier
{
    [Header("Target Field")]
    public string fieldName;

    [Header("Value")]
    public float addValue;

    [Tooltip("0.5 = 1.5น่, 1 = 2น่, 2 = 3น่")]
    public float multiplyValue;

    public override bool CanApplyTo(object stat, BuffQueryContext query)
    {
        if (!base.CanApplyTo(stat, query))
            return false;

        if (stat == null || string.IsNullOrEmpty(fieldName))
            return false;

        FieldInfo field = GetTargetField(stat);
        return field != null;
    }

    public override void ApplyTo(object stat, int stack, BuffQueryContext query)
    {
        ApplyAdditiveTo(stat, stack, query);
        ApplyMultiplicativeTo(stat, stack, query);
        ClampIfPossible(stat);
    }

    public override void ApplyAdditiveTo(object stat, int stack, BuffQueryContext query)
    {
        FieldInfo field = GetTargetField(stat);
        if (field == null)
            return;

        float value = (float)field.GetValue(stat);
        value += addValue * Mathf.Max(1, stack);
        field.SetValue(stat, value);
    }

    public override void ApplyMultiplicativeTo(object stat, int stack, BuffQueryContext query)
    {
        FieldInfo field = GetTargetField(stat);
        if (field == null)
            return;

        float value = (float)field.GetValue(stat);

        int safeStack = Mathf.Max(1, stack);
        float multiplier = 1f + multiplyValue;

        for (int i = 0; i < safeStack; i++)
            value *= multiplier;

        field.SetValue(stat, value);
    }

    private FieldInfo GetTargetField(object stat)
    {
        if (stat == null || string.IsNullOrEmpty(fieldName))
            return null;

        FieldInfo field = stat.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);

        if (field == null || field.FieldType != typeof(float))
            return null;

        return field;
    }
}