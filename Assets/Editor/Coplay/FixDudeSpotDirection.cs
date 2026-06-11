using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixDudeSpotDirection
{
    public static string Execute()
    {
        var dude = GameObject.Find("Level/Dude");
        if (dude == null) return "No Dude";
        var spotTf = dude.transform.Find("OverheadSpot");
        if (spotTf == null) return "No OverheadSpot";

        // The Dude's localScale.x is negative (-1.5), which mirrors child rotation.
        // Counter the mirror on this child so the cone aims predictably, then point it
        // straight DOWN onto his head.
        spotTf.localScale = new Vector3(-1f, 1f, 1f); // cancels parent's negative X
        spotTf.localPosition = new Vector3(0f, 2.4f, 0f);
        spotTf.localEulerAngles = new Vector3(0f, 0f, -90f); // cone faces down

        EditorUtility.SetDirty(spotTf.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return $"OverheadSpot fixed: localScale={spotTf.localScale}, euler={spotTf.localEulerAngles}, worldDown cone.";
    }
}
