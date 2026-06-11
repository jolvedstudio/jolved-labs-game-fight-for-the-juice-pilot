using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Game;

public class WireDudeSpotFollow
{
    public static string Execute()
    {
        var dude = GameObject.Find("Level/Dude");
        var level = GameObject.Find("Level");
        if (dude == null) return "No Dude";
        // OverheadSpot was reparented under Level
        Transform spot = null;
        if (level != null)
        {
            var t = level.transform.Find("OverheadSpot");
            if (t != null) spot = t;
        }
        if (spot == null) return "No OverheadSpot under Level";

        var follow = spot.GetComponent<OverheadLightFollow>();
        if (follow == null) follow = spot.gameObject.AddComponent<OverheadLightFollow>();
        follow.Target = dude.transform;
        follow.Offset = new Vector3(0f, 3.8f, 0f);
        follow.LockVerticalToStart = false;

        EditorUtility.SetDirty(spot.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return "OverheadSpot now follows the Dude horizontally while keeping its downward cone.";
    }
}
