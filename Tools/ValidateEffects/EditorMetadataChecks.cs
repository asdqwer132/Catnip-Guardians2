using System;

// Only reflection over the real stat/field dropdown list executes here.
// Native Unity Editor GUI and scene APIs are not exercised by these checks.
public static class EditorMetadataChecks
{
    public static void Main()
    {
        new BuffDurationEditorTests().ModifierStatDropdownIncludesBuffInfoAndItsDurationField();
        Console.WriteLine("PASS actual modifier dropdown metadata includes BuffInfo.duration (no Unity GUI execution).");
        new SummonArchitectureEditorTests().ModifierDropdownSeparatesCreationAndModuleFields();
        Console.WriteLine("PASS actual modifier dropdown separates creation and nine module stat categories (no Unity GUI execution).");
        new SummonArchitectureEditorTests().InspectorLabelsGroupCreationAttackMovementAndSupportStats();
        Console.WriteLine("PASS actual inspector labels group creation, attack, movement and support stats (no Unity GUI execution).");
    }
}
