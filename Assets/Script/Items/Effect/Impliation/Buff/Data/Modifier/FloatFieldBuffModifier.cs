
using System.Reflection;
using UnityEngine;

[CreateAssetMenu(fileName = "Float Field Buff Modifier", menuName = "Game/Buff/Modifier/Float Field")]
public class FloatFieldBuffModifier : BuffModifier
{
    [Header("Target Field")]
    public string fieldName;

    [Header("Value")]
    public float addValue;
    public float multiplyValue;

    public override bool CanApplyTo(object stat, BuffQueryContext query)
    {
        if (!base.CanApplyTo(stat, query))
            return false;

        if (stat == null || string.IsNullOrEmpty(fieldName))
            return false;

        FieldInfo field = stat.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        return field != null && field.FieldType == typeof(float);
    }

    public override void ApplyTo(object stat, int stack, BuffQueryContext query)
    {
        if (stat == null || string.IsNullOrEmpty(fieldName))
            return;

        FieldInfo field = stat.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (field == null || field.FieldType != typeof(float))
            return;

        float value = (float)field.GetValue(stat);

        for (int i = 0; i < Mathf.Max(1, stack); i++)
        {
            value += addValue;
            value *= 1f + multiplyValue;
        }

        field.SetValue(stat, value);
        ClampIfPossible(stat);
    }
}
