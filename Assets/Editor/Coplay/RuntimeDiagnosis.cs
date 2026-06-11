using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;

/// <summary>
/// Comprehensive diagnosis: checks the blueRobot prefab internals,
/// the LavaBot prefab internals, and the scene YAML raw content
/// for any GUID cross-references.
/// </summary>
public class RuntimeDiagnosis
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // ── 1. Read raw scene YAML and search for ALL prefab GUIDs referenced ──
        string scenePath = "Assets/CorgiEngine/Demos/Corgi2D/Lava.unity";
        string sceneText = File.ReadAllText(Path.GetFullPath(scenePath));

        // Find all GUIDs in the scene file
        var guidMatches = System.Text.RegularExpressions.Regex.Matches(
            sceneText, @"guid: ([a-f0-9]{32})");
        var uniqueGuids = new System.Collections.Generic.HashSet<string>();
        foreach (System.Text.RegularExpressions.Match m in guidMatches)
            uniqueGuids.Add(m.Groups[1].Value);

        sb.AppendLine($"=== Unique GUIDs in scene ({uniqueGuids.Count}) ===");
        foreach (var guid in uniqueGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) && path.Contains("Prefabs/AI"))
                sb.AppendLine($"  AI Prefab: {path}  [{guid}]");
        }

        // ── 2. Check blueRobot prefab - does it have MMPoolableObject? ──────────
        string blueRobotPath = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/blueRobot.prefab";
        GameObject blueRobotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(blueRobotPath);
        sb.AppendLine($"\n=== blueRobot prefab components ===");
        if (blueRobotPrefab != null)
        {
            foreach (var comp in blueRobotPrefab.GetComponents<Component>())
                sb.AppendLine($"  {comp.GetType().Name}");
        }

        // ── 3. Check LavaBot prefab components ───────────────────────────────────
        string lavaBotPath = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/LavaBot.prefab";
        GameObject lavaBotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lavaBotPath);
        sb.AppendLine($"\n=== LavaBot prefab components ===");
        if (lavaBotPrefab != null)
        {
            foreach (var comp in lavaBotPrefab.GetComponents<Component>())
                sb.AppendLine($"  {comp.GetType().Name}");

            // Check sprite
            var sr = lavaBotPrefab.GetComponent<SpriteRenderer>();
            sb.AppendLine($"  SpriteRenderer.sprite = {(sr?.sprite != null ? sr.sprite.name : "NULL")}");

            // Check CorgiController DefaultParameters
            var cc = lavaBotPrefab.GetComponent<CorgiController>();
            sb.AppendLine($"  CorgiController.DefaultParameters = {(cc?.DefaultParameters != null ? "SET" : "NULL")}");

            // Check collider offset
            var col = lavaBotPrefab.GetComponent<BoxCollider2D>();
            if (col != null)
                sb.AppendLine($"  BoxCollider2D: size={col.size}, offset={col.offset}");
        }

        // ── 4. Check the blueRobot prefab's MMPoolableObject ─────────────────────
        sb.AppendLine($"\n=== blueRobot MMPoolableObject ===");
        if (blueRobotPrefab != null)
        {
            var pool = blueRobotPrefab.GetComponent<MMPoolableObject>();
            sb.AppendLine($"  Has MMPoolableObject: {pool != null}");
        }

        // ── 5. Check LavaBot scene instances - are they truly LavaBot prefab instances? ──
        sb.AppendLine($"\n=== Scene LavaBot prefab override check ===");
        // Read raw YAML around "LavaBot" entries
        int idx = 0;
        while ((idx = sceneText.IndexOf("LavaBot", idx)) >= 0)
        {
            int lineStart = sceneText.LastIndexOf('\n', idx) + 1;
            int lineEnd = sceneText.IndexOf('\n', idx);
            if (lineEnd < 0) lineEnd = sceneText.Length;
            string line = sceneText.Substring(lineStart, lineEnd - lineStart).Trim();
            if (line.Length < 200) // skip huge lines
                sb.AppendLine($"  YAML: {line}");
            idx += 7;
            if (idx > sceneText.Length) break;
        }

        return sb.ToString();
    }
}
