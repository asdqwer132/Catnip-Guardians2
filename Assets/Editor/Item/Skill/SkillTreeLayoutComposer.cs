using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal sealed class SkillTreeLayoutComposer
{
    [Serializable]
    internal sealed class Mix
    {
        public Vector3 radialConstellationFan = new Vector3(1, 2, 0);
        public Vector3 clusterSpiralDiamond;

        public Vector2 Project(float angle, float level, float side, float spacing, float twist)
        {
            var first = Vector3.Max(Vector3.zero, radialConstellationFan);
            var second = Vector3.Max(Vector3.zero, clusterSpiralDiamond);
            float total = first.x + first.y + first.z + second.x + second.y + second.z;
            float radius = level * spacing;
            float lateral = side * spacing * .55f;
            if (total <= 0) return Polar(angle, radius, lateral);

            return (first.x * Polar(angle, radius, lateral)
                + first.y * Polar(angle + twist + .16f * Mathf.Sin(level * 1.77f),
                    radius * (1 + .1f * Mathf.Sin(level * 2.31f)), lateral)
                + first.z * Polar(angle + side * .29f, radius + Mathf.Abs(side) * spacing * .25f, lateral * .3f)
                + second.x * Polar(angle + twist * Mathf.Sin(level),
                    radius + spacing * .27f * Mathf.Cos(level * Mathf.PI), lateral * 1.45f)
                + second.y * Polar(angle + level * twist, radius, lateral)
                + second.z * Polar(angle, radius, lateral * (.95f + .3f * Mathf.Cos(level * Mathf.PI)))) / total;
        }

        private static Vector2 Polar(float angle, float radius, float side)
        {
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return direction * radius + new Vector2(-direction.y, direction.x) * side;
        }
    }

    [Serializable]
    internal sealed class Branch
    {
        public SkillNodeData head;
        public Mix mix = new Mix();
        public float angleOffset;
        public float spacingMultiplier = 1f;
    }

    [Serializable]
    internal sealed class Settings
    {
        public Mix mix = new Mix();
        public List<Branch> branches = new List<Branch>();
        public int seed = 23;
        public float spacing = 320f;
        public float jitter = .25f;
        public float rotation = 90f;
        public float sweep = 360f;
        public float spread = 1.45f;
        public float twist = .18f;
        public float clearance = 30f;
    }

    private readonly List<SkillNodeData> ordered;
    private readonly Dictionary<SkillNodeData, List<SkillNodeData>> parents;
    private readonly Dictionary<SkillNodeData, int> depth = new Dictionary<SkillNodeData, int>();
    private readonly Dictionary<SkillNodeData, SkillNodeData> branch = new Dictionary<SkillNodeData, SkillNodeData>();
    public SkillNodeData Root { get; }
    public List<SkillNodeData> Heads { get; } = new List<SkillNodeData>();

    public SkillTreeLayoutComposer(SkillTreeLayoutData layout, SkillNodeData preferredRoot)
    {
        ordered = layout.nodeDatas.Where(node => node != null).Distinct().ToList();
        var members = new HashSet<SkillNodeData>(ordered);
        parents = ordered.ToDictionary(node => node, node => (node.requiredSkills ?? new List<SkillNodeData>())
            .Where(parent => parent != null && members.Contains(parent)).Distinct().ToList());
        Root = preferredRoot != null && members.Contains(preferredRoot) && parents[preferredRoot].Count == 0
            ? preferredRoot : ordered.FirstOrDefault(node => parents[node].Count == 0);

        var remaining = new HashSet<SkillNodeData>(ordered);
        while (remaining.Count > 0)
        {
            var ready = ordered.Where(node => remaining.Contains(node) && parents[node].All(depth.ContainsKey)).ToList();
            if (ready.Count == 0) throw new InvalidOperationException("The graph contains a prerequisite cycle.");
            foreach (var node in ready)
            {
                remaining.Remove(node);
                if (node == Root)
                {
                    depth[node] = 0;
                    continue;
                }

                var predecessors = parents[node];
                depth[node] = predecessors.Count == 0 ? 1 : 1 + predecessors.Max(parent => depth[parent]);
                var branchParent = predecessors.Where(parent => parent != Root)
                    .OrderByDescending(parent => depth[parent]).ThenBy(parent => ordered.IndexOf(parent)).FirstOrDefault();
                branch[node] = branchParent != null ? branch[branchParent] : node;
            }
        }
        Heads.AddRange(ordered.Where(node => branch.TryGetValue(node, out var head) && head == node));
    }

    public Dictionary<SkillNodeData, Vector2> Calculate(Settings settings, Vector2 cardSize)
    {
        var result = new Dictionary<SkillNodeData, Vector2>();
        if (Root == null) return result;

        result[Root] = Vector2.zero;
        var rng = new System.Random(settings.seed);
        int divisions = settings.sweep >= 360f ? Heads.Count : Mathf.Max(1, Heads.Count - 1);
        for (int i = 0; i < Heads.Count; i++)
        {
            var head = Heads[i];
            var overrides = settings.branches.FirstOrDefault(value => value.head == head);
            var mix = overrides?.mix ?? settings.mix;
            float angle = (settings.rotation - i * settings.sweep / divisions + (overrides?.angleOffset ?? 0f)) * Mathf.Deg2Rad;
            float spacing = settings.spacing * (overrides?.spacingMultiplier ?? 1f);
            float variation = settings.jitter * spacing;
            var levels = ordered.Where(node => node != Root && branch[node] == head).GroupBy(node => depth[node]);
            foreach (var level in levels)
            {
                var layer = level.ToList();
                for (int index = 0; index < layer.Count; index++)
                {
                    float lateral = (index - (layer.Count - 1) * .5f) * settings.spread;
                    var jitter = new Vector2((float)rng.NextDouble() * 2 - 1, (float)rng.NextDouble() * 2 - 1) * variation;
                    result[layer[index]] = mix.Project(angle, level.Key, lateral, spacing, settings.twist) + jitter;
                }
            }
        }

        Separate(result, cardSize + Vector2.one * settings.clearance);
        return result;
    }

    private void Separate(Dictionary<SkillNodeData, Vector2> points, Vector2 size)
    {
        for (int pass = 0; pass < 160; pass++)
        {
            bool moved = false;
            for (int i = 0; i < ordered.Count; i++)
            for (int j = i + 1; j < ordered.Count; j++)
            {
                var a = ordered[i];
                var b = ordered[j];
                var delta = points[b] - points[a];
                var overlap = size - new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
                if (overlap.x <= .1f || overlap.y <= .1f) continue;
                var shift = overlap.x < overlap.y
                    ? new Vector2(overlap.x * Mathf.Sign(delta.x), 0)
                    : new Vector2(0, overlap.y * Mathf.Sign(delta.y));
                if (a != Root) points[a] -= shift * (b == Root ? 1f : .5f);
                if (b != Root) points[b] += shift * (a == Root ? 1f : .5f);
                moved = true;
            }
            if (!moved) return;
        }
        throw new InvalidOperationException("The nodes could not be separated. Increase spacing or reduce clearance.");
    }
}
