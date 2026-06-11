using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public class TuneGlobalLight
{
    public static string Execute()
    {
        var g = GameObject.Find("Level/Lighting2D/GlobalLight2D");
        if (g == null) return "GlobalLight2D not found";
        var light = g.GetComponent<Light2D>();
        light.color = new Color(0.55f, 0.62f, 0.85f);
        light.intensity = 0.30f; // slightly brighter baseline so the level is readable
        EditorUtility.SetDirty(light);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return $"Global light tuned: intensity={light.intensity}";
    }
}
