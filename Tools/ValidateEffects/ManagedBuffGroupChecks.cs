using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class Runner
{
    private static int groupConstructorCalls, groupGetterCalls, groupStaticConstructorCalls;
    private abstract class GroupMetadataTarget : IBuffTarget
    {
        public UnityEngine.Object BuffTargetObject => null;
        public abstract string BuffTargetGroup { get; }
        public string BuffTargetDebugName => "NotAGroup";
        public void RefreshBuffedStat() { }
    }
    private sealed class LiteralGroupTarget : GroupMetadataTarget
    {
        public LiteralGroupTarget() { groupConstructorCalls++; }
        public override string BuffTargetGroup => "Catalog/Literal";
    }
    private class FieldGroupTarget : GroupMetadataTarget
    {
        private string group = "Catalog/FieldDefault";
        private string unrelated = "NotAGroup";
        public override string BuffTargetGroup => group;
    }
    private sealed class InheritedGroupTarget : FieldGroupTarget { }
    private sealed class ConstantGroupTarget : GroupMetadataTarget
    {
        private const string group = "Catalog/Constant";
        public override string BuffTargetGroup => group;
    }
    private sealed class StaticGroupTarget : GroupMetadataTarget
    {
        private static readonly string group = "Catalog/StaticDefault";
        static StaticGroupTarget() { groupStaticConstructorCalls++; }
        public override string BuffTargetGroup => group;
    }
    private sealed class ExplicitGroupTarget : IBuffTarget
    {
        public UnityEngine.Object BuffTargetObject => null;
        string IBuffTarget.BuffTargetGroup => "Catalog/Explicit";
        public string BuffTargetDebugName => "NotAGroup";
        public void RefreshBuffedStat() { }
    }
    [BuffTargetGroups("Catalog/Alias", "Catalog/Alias")]
    private sealed class AttributeGroupTarget : GroupMetadataTarget
    {
        public override string BuffTargetGroup => "Catalog/Property";
    }
    [BuffTargetGroups("Catalog/DynamicFallback")]
    private sealed class DynamicGroupTarget : GroupMetadataTarget
    {
        public override string BuffTargetGroup { get { groupGetterCalls++; throw new InvalidOperationException(); } }
    }
    [BuffTargetGroups("NotAGroup")]
    private sealed class UnrelatedGroupType { }

    private static bool Declares(Type type, string group)
        => Array.IndexOf(BuffTargetGroupDiscovery.GetDeclaredGroups(type), group) >= 0;

    private static void RunBuffGroupChecks()
    {
        Check("unannotated IBuffTarget literal group is found without constructing a scene object", () => {
            True(Declares(typeof(LiteralGroupTarget), "Catalog/Literal")); True(groupConstructorCalls == 0);
        });
        Check("IBuffTarget field initializer contributes its default group", () => {
            True(Declares(typeof(FieldGroupTarget), "Catalog/FieldDefault"));
        });
        Check("unrelated strings in a target are not collected as group names", () => {
            True(!Declares(typeof(FieldGroupTarget), "NotAGroup"));
        });
        Check("inherited IBuffTarget group getters and default fields are discovered", () => {
            True(Declares(typeof(InheritedGroupTarget), "Catalog/FieldDefault"));
        });
        Check("const group declarations are discovered", () => {
            True(Declares(typeof(ConstantGroupTarget), "Catalog/Constant"));
        });
        Check("static group defaults are read without running a static constructor", () => {
            True(Declares(typeof(StaticGroupTarget), "Catalog/StaticDefault")); True(groupStaticConstructorCalls == 0);
        });
        Check("explicit IBuffTarget group implementations are discovered", () => {
            True(Declares(typeof(ExplicitGroupTarget), "Catalog/Explicit"));
        });
        Check("optional declared aliases extend the property group without duplicates", () => {
            string[] groups = BuffTargetGroupDiscovery.GetDeclaredGroups(typeof(AttributeGroupTarget));
            True(groups.Length == 3 && Array.IndexOf(groups, "Catalog/Alias") >= 0 && Array.IndexOf(groups, "Catalog/Property") >= 0);
        });
        Check("metadata discovery never executes a dynamic group getter", () => {
            True(Declares(typeof(DynamicGroupTarget), "Catalog/DynamicFallback")); True(groupGetterCalls == 0);
        });
        Check("non IBuffTarget types cannot add declared groups", () => {
            True(BuffTargetGroupDiscovery.GetDeclaredGroups(typeof(UnrelatedGroupType)).Length == 0);
        });
        Check("catalog group names normalize separators and add all parents", () => {
            var groups = new SortedSet<string>(StringComparer.Ordinal);
            BuffTargetGroupDiscovery.AddGroup(groups, " /Enemy\\Boss//Fire/ ");
            True(groups.SetEquals(new[] { "Enemy", "Enemy/Boss", "Enemy/Boss/Fire" }));
            // Same normalization is used by the runtime, so the chosen parent actually matches.
            True(BuffTargetHandle.Group("Enemy/Boss").MatchesTarget(new RuntimeGroupTarget("Enemy\\Boss//Fire")));
            True(!BuffTargetHandle.Group("Enemy/Boss").MatchesTarget(new RuntimeGroupTarget("Enemy/BossExtra")));
        });
        Check("empty normalized group names do not create choices or runtime handles", () => {
            var groups = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string value in new[] { null, "", " ", "///", "\\\\" }) {
                BuffTargetGroupDiscovery.AddGroup(groups, value); True(BuffTargetHandle.Group(value) == null);
            }
            True(groups.Count == 0);
        });
    }
    private sealed class RuntimeGroupTarget : GroupMetadataTarget
    {
        private readonly string group;
        public RuntimeGroupTarget(string group) { this.group = group; }
        public override string BuffTargetGroup => group;
    }
}
