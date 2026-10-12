using System;

// Only reflection over the real stat/field dropdown list executes here.
// Native Unity Editor GUI and scene APIs are not exercised by these checks.
public static class EditorMetadataChecks
{
    public static void Main()
    {
        new BuffDurationEditorTests().ModifierStatDropdownIncludesBuffInfoAndItsDurationField();
        Console.WriteLine("PASS actual modifier dropdown metadata includes BuffInfo.duration (no Unity GUI execution).");
        new SummonArchitectureEditorTests().ModifierDropdownSeparatesCreationAndActorFields();
        Console.WriteLine("PASS actual modifier dropdown separates SummonSpawnStat and SummonActorStat (no Unity GUI execution).");
    }
}
