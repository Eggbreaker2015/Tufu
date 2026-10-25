using System;
using System.Collections.Generic;
using Codebase.Amulet.Upgrades;
using UnityEngine;

namespace Codebase.UI.Amulet
{
	public sealed class AmuletUpgradeGraphLayoutResolver
	{
		private readonly AmuletUpgradeConfig config;

		private readonly Dictionary<string, AmuletUpgradeDef> upgradesByKey;

		private readonly Dictionary<string, List<string>> children;

		private readonly Dictionary<string, List<string>> parents;

		private readonly Dictionary<string, int> depths;

		private readonly Dictionary<string, float> subtreeWeights;

		private readonly Dictionary<string, Vector2> initialDirections;

		private const float SpringStrength = 0.24f;

		private const float DepthStrength = 0.035f;

		private const float AngularStrength = 0.012f;

		private const float CollisionStrength = 0.72f;

		private const int FinalCollisionIterations = 20;

		public AmuletUpgradeGraphLayoutResolver(AmuletUpgradeConfig config)
		{
// Original ELF VA 0x13cb414.
            upgradesByKey = new Dictionary<string,AmuletUpgradeDef>();
            children = new Dictionary<string,List<string>>();
            parents = new Dictionary<string,List<string>>();
            depths = new Dictionary<string,int>();
            subtreeWeights = new Dictionary<string,float>();
            initialDirections = new Dictionary<string,Vector2>();
            this.config = config;
		}

		public void Build(Dictionary<string, Vector2> positions, Dictionary<string, int> nodeLevels)
		{
// Original ELF VA 0x13cb61c.
            positions.Clear();
            nodeLevels.Clear();
            BuildGraph();
            CalculateDepths();
            foreach (var pair in depths) nodeLevels[pair.Key] = pair.Value;
            var levels = new List<int>();
            foreach (AmuletUpgradeDef definition in config.Upgrades)
                if (!levels.Contains(definition.RequiredAmuletLevel)) levels.Add(definition.RequiredAmuletLevel);
            levels.Sort();
            foreach (int level in levels) BuildLevel(level,positions);
		}

		private void BuildGraph()
		{
// Original ELF VA 0x13cbd50: only links within the same amulet-level page affect layout.
            upgradesByKey.Clear();
            children.Clear();
            parents.Clear();
            foreach (AmuletUpgradeDef definition in config.Upgrades)
            {
                upgradesByKey[definition.Key] = definition;
                children[definition.Key] = new List<string>();
                parents[definition.Key] = new List<string>();
            }
            foreach (AmuletUpgradeDef definition in config.Upgrades)
            {
                if (definition.RequiredUpgradeKeys == null) continue;
                foreach (string parentKey in definition.RequiredUpgradeKeys)
                {
                    if (!upgradesByKey.TryGetValue(parentKey,out AmuletUpgradeDef parent) || parent.RequiredAmuletLevel != definition.RequiredAmuletLevel) continue;
                    children[parentKey].Add(definition.Key);
                    parents[definition.Key].Add(parentKey);
                }
            }
		}

		private void CalculateDepths()
		{
// Original ELF VA 0x13cc484.
            depths.Clear();
            var visiting = new HashSet<string>();
            foreach (AmuletUpgradeDef definition in config.Upgrades) CalculateDepth(definition.Key,visiting);
		}

		private int CalculateDepth(string key, HashSet<string> visiting)
		{
// Original ELF VA 0x13cd2a8, including its cycle boundary.
            if (depths.TryGetValue(key,out int cached)) return cached;
            if (!visiting.Add(key)) return 0;
            int depth = 0;
            foreach (string parentKey in parents[key]) depth = Math.Max(depth,CalculateDepth(parentKey,visiting) + 1);
            visiting.Remove(key);
            depths[key] = depth;
            return depth;
		}

		private void BuildLevel(int level, Dictionary<string, Vector2> positions)
		{
// Original ELF VA 0x13cc6ac, with source ordering retained for deterministic Random draws.
            var keys = new List<string>();
            var roots = new List<string>();
            foreach (AmuletUpgradeDef definition in config.Upgrades)
            {
                if (definition.RequiredAmuletLevel != level) continue;
                keys.Add(definition.Key);
                if (parents[definition.Key].Count == 0) roots.Add(definition.Key);
            }
            if (keys.Count == 0) return;
            if (roots.Count == 0) roots.Add(keys[0]);
            subtreeWeights.Clear();
            initialDirections.Clear();
            foreach (string root in roots) CalculateSubtreeWeight(root,new HashSet<string>());
            var random = new System.Random(CombineHash(config.Seed,level));
            float rotation = (float)random.NextDouble() * Mathf.PI;
            rotation += rotation;
            var positioned = new HashSet<string>();
            float totalWeight = 0f;
            foreach (string root in roots) totalWeight += subtreeWeights.GetValueOrDefault(root,1f);
            float cursor = rotation;
            for (int index = 0; index < roots.Count; index++)
            {
                string root = roots[index];
                float sector = subtreeWeights.GetValueOrDefault(root,1f) * (Mathf.PI * 2f) / totalWeight;
                Vector2 position = roots.Count == 1 ? Vector2.zero : Direction(cursor + sector * .5f) * config.MinimumNodeDistance;
                positions[root] = position;
                initialDirections[root] = position.normalized;
                positioned.Add(root);
                PositionChildren(root,cursor,cursor + sector,random,positioned,positions);
                cursor += sector;
            }
            foreach (string key in keys)
            {
                if (positions.ContainsKey(key)) continue;
                float randomAngle = (float)random.NextDouble() * Mathf.PI;
                Vector2 direction = Direction(rotation + randomAngle + randomAngle);
                positions[key] = direction * (config.TargetEdgeLength * Math.Max(1,depths.GetValueOrDefault(key)));
                initialDirections[key] = direction;
            }
            RelaxLevel(keys,roots,positions);
		}

		private float CalculateSubtreeWeight(string key, HashSet<string> visiting)
		{
// Original ELF VA 0x13cd56c: weight counts leaf spans, with one on a cycle.
            if (subtreeWeights.TryGetValue(key,out float cached)) return cached;
            if (!visiting.Add(key)) return 1f;
            float sum = 0f;
            foreach (string child in children[key]) sum += CalculateSubtreeWeight(child,visiting);
            visiting.Remove(key);
            float weight = Math.Max(1f,sum);
            subtreeWeights[key] = weight;
            return weight;
		}

		private void PositionChildren(string parentKey, float startAngle, float endAngle, System.Random random, HashSet<string> positioned, Dictionary<string, Vector2> positions)
		{
// Original ELF VA 0x13cd864. Shared children are positioned by their first parent only.
            List<string> childKeys = children[parentKey];
            if (childKeys.Count == 0) return;
            float totalWeight = 0f;
            foreach (string child in childKeys) totalWeight += subtreeWeights.GetValueOrDefault(child,1f);
            float cursor = startAngle;
            foreach (string child in childKeys)
            {
                float sector = (endAngle - startAngle) * subtreeWeights.GetValueOrDefault(child,1f) / totalWeight;
                if (positioned.Add(child))
                {
                    Vector2 direction = Direction(cursor + sector * .5f);
                    int depth = Math.Max(1,depths.GetValueOrDefault(child));
                    Vector2 jitter = NextInsideUnitCircle(random);
                    positions[child] = direction * (config.TargetEdgeLength * depth) + jitter * config.InitialLayoutJitter;
                    initialDirections[child] = direction;
                    PositionChildren(child,cursor,cursor + sector,random,positioned,positions);
                }
                cursor += sector;
            }
		}

		private void RelaxLevel(List<string> keys, List<string> roots, Dictionary<string, Vector2> positions)
		{
// Original ELF VA 0x13cddb0. Native constants verified against original PT_LOAD bytes.
            var fixedNodes = new HashSet<string>(roots);
            var edges = new List<(string parent,string child)>();
            foreach (string key in keys)
                foreach (string parent in parents[key]) edges.Add((parent,key));
            float edgeLength = Math.Max(config.TargetEdgeLength,config.MinimumNodeDistance);
            int iterations = Math.Max(1,config.LayoutIterations);
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                float cooling = 1f + Mathf.Clamp01((float)iteration / iterations) * -.65f;
                foreach (var edge in edges)
                {
                    Vector2 delta = positions[edge.child] - positions[edge.parent];
                    float distance = Math.Max(.001f,delta.magnitude);
                    Vector2 correction = (delta / distance) * (cooling * (distance - edgeLength) * SpringStrength);
                    ApplyPairCorrection(edge.parent,edge.child,correction,fixedNodes,positions);
                }
                foreach (string key in keys)
                {
                    if (fixedNodes.Contains(key)) continue;
                    Vector2 position = positions[key];
                    float targetRadius = config.TargetEdgeLength * Math.Max(1,depths.GetValueOrDefault(key));
                    float radius = Math.Max(.001f,position.magnitude);
                    Vector2 radialDirection = position / radius;
                    Vector2 initialDirection = initialDirections.GetValueOrDefault(key,radialDirection);
                    float radialCorrection = cooling * (targetRadius - radius) * DepthStrength;
                    positions[key] = position + radialDirection * radialCorrection + (initialDirection * radius - position) * cooling * AngularStrength;
                }
                ResolveCollisions(keys,fixedNodes,positions,cooling * CollisionStrength);
            }
            for (int iteration = 0; iteration < FinalCollisionIterations; iteration++) ResolveCollisions(keys,fixedNodes,positions,1f);
		}

		private void ApplyPairCorrection(string firstKey, string secondKey, Vector2 correction, HashSet<string> fixedNodes, Dictionary<string, Vector2> positions)
		{
// Original ELF VA 0x13ce880. Spring corrections attract.
            bool firstFixed = fixedNodes.Contains(firstKey), secondFixed = fixedNodes.Contains(secondKey);
            if (firstFixed && secondFixed) return;
            if (firstFixed) positions[secondKey] -= correction;
            else if (secondFixed) positions[firstKey] += correction;
            else
            {
                positions[firstKey] += correction * .5f;
                positions[secondKey] -= correction * .5f;
            }
		}

		private void ResolveCollisions(List<string> keys, HashSet<string> fixedNodes, Dictionary<string, Vector2> positions, float strength)
		{
// Original ELF VA 0x13cea54; coincident nodes use ordered-key deterministic direction.
            float minimumDistance = config.MinimumNodeDistance;
            for (int first = 0; first < keys.Count - 1; first++)
            {
                string firstKey = keys[first];
                for (int second = first + 1; second < keys.Count; second++)
                {
                    string secondKey = keys[second];
                    Vector2 delta = positions[secondKey] - positions[firstKey];
                    float distance = delta.magnitude;
                    if (distance >= minimumDistance) continue;
                    Vector2 direction = distance <= .001f ? DirectionFromKeys(firstKey,secondKey) : delta / distance;
                    ApplyCollisionCorrection(firstKey,secondKey,direction * ((minimumDistance - distance) * strength),fixedNodes,positions);
                }
            }
		}

		private void ApplyCollisionCorrection(string firstKey, string secondKey, Vector2 correction, HashSet<string> fixedNodes, Dictionary<string, Vector2> positions)
		{
// Original ELF VA 0x13cecbc. Collision corrections repel, opposite to the spring helper.
            bool firstFixed = fixedNodes.Contains(firstKey), secondFixed = fixedNodes.Contains(secondKey);
            if (firstFixed && secondFixed) return;
            if (firstFixed) positions[secondKey] += correction;
            else if (secondFixed) positions[firstKey] -= correction;
            else
            {
                positions[firstKey] -= correction * .5f;
                positions[secondKey] += correction * .5f;
            }
		}

		private Vector2 DirectionFromKeys(string firstKey, string secondKey)
		{
// Original ELF VA 0x13cec64: UCVTF S0,W8,#32, then two single-precision PI products.
            uint combined = unchecked((uint)CombineHash(StableHash(firstKey),StableHash(secondKey)));
            float angle = ((float)combined / 4294967296f) * Mathf.PI;
            return Direction(angle + angle);
		}

		private Vector2 NextInsideUnitCircle(System.Random random)
		{
// Original ELF VA 0x13ce800 consumes angle draw before radius draw.
            float angle = (float)random.NextDouble() * Mathf.PI;
            angle += angle;
            float radius = Mathf.Sqrt((float)random.NextDouble());
            return Direction(angle) * radius;
		}

		private Vector2 Direction(float angle)
		{
// Original ELF VA 0x13cd848 calls sincosf and returns (cos,sin).
            return new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
		}

		private int StableHash(string value)
		{
// Original ELF VA 0x13cee90: UTF-16 characters, unsigned wrapping multiply by 31.
            unchecked
            {
                int hash = 17;
                for (int index = 0; index < value.Length; index++) hash = hash * 31 + value[index];
                return hash;
            }
		}

		private int CombineHash(int first, int second)
		{
// Original ELF VA 0x13cd838.
            return unchecked(first * 397) ^ second;
		}
	}
}
