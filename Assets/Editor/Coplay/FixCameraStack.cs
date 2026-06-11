using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixCameraStack
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        var mainGo = GameObject.Find("Main Camera");
        var uiGo = GameObject.Find("UICamera");
        if (mainGo == null || uiGo == null) return "Main Camera or UICamera not found";

        var mainCam = mainGo.GetComponent<Camera>();
        var uiCam = uiGo.GetComponent<Camera>();

        // Ensure Main Camera is a Base camera (it already is) and renders post-processing.
        var mainData = mainGo.GetComponent<UniversalAdditionalCameraData>();
        if (mainData == null) mainData = mainGo.AddComponent<UniversalAdditionalCameraData>();
        mainData.renderType = CameraRenderType.Base;

        // Give the UICamera URP data and make it an Overlay camera.
        var uiData = uiGo.GetComponent<UniversalAdditionalCameraData>();
        if (uiData == null) uiData = uiGo.AddComponent<UniversalAdditionalCameraData>();
        uiData.renderType = CameraRenderType.Overlay;

        // Add the UICamera to the Main Camera's stack (if not already present).
        if (!mainData.cameraStack.Contains(uiCam))
        {
            mainData.cameraStack.Add(uiCam);
            sb.AppendLine("Added UICamera as Overlay to Main Camera's stack.");
        }
        else
        {
            sb.AppendLine("UICamera already in stack.");
        }

        EditorUtility.SetDirty(mainGo);
        EditorUtility.SetDirty(uiGo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        sb.AppendLine($"Main(base) depth={mainCam.depth}, UI(overlay) cullMask={uiCam.cullingMask}.");
        return sb.ToString();
    }
}
