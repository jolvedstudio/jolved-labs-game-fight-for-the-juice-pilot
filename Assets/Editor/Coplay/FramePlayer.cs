using UnityEngine;
using UnityEditor;
using Game;

public class FramePlayer
{
    public static string Execute()
    {
        var pt = Object.FindFirstObjectByType<PlayerTorch>();
        if (pt == null) return "no player";
        var view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            view.in2DMode = true;
            view.LookAt(pt.transform.position, Quaternion.identity, 14f);
            view.Repaint();
        }
        return $"Framed player at {pt.transform.position}";
    }
}
