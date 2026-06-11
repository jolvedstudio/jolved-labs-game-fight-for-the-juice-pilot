using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixDudeSpotDirection2
{
    public static string Execute()
    {
        var dude = GameObject.Find("Level/Dude");
        if (dude == null) return "No Dude";
        var level = GameObject.Find("Level");
        var spotTf = dude.transform.Find("OverheadSpot");
        if (spotTf == null) return "No OverheadSpot";

        // Reparent out of the mirrored (-1.5 x) Dude transform so rotation is predictable.
        spotTf.SetParent(level != null ? level.transform : null, true);
        spotTf.localScale = Vector3.one;

        // Place above the Dude's head (world) and aim the cone straight DOWN.
        Vector3 dudeWorld = dude.transform.position; // ~(-15.12, -7.99)
        spotTf.position = new Vector3(dudeWorld.x, dudeWorld.y + 3.8f, 0f);

        // For a URP 2D spot (point light w/ angles) the cone is centered on the local +Y (up).
        // Rotate 180deg so it faces down.
        spotTf.rotation = Quaternion.Euler(0f, 0f, 180f);

        EditorUtility.SetDirty(spotTf.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return $"OverheadSpot reparented to Level, world pos={spotTf.position}, rot={spotTf.eulerAngles}.";
    }
}
