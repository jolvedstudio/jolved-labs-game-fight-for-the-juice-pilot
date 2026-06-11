using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LavaRun;

public class BuildMenuScenes
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        BuildMenuScene(
            path: "Assets/Game/Scenes/StartScreen.unity",
            title: "LAVA RUN",
            subtitle: "Reach the gate. Grab the coins. Don't touch the lava.",
            buttonLabel: "PLAY",
            loadScene: "Lava",
            autoLoadDelay: 0f,
            bg: new Color(0.10f, 0.03f, 0.02f, 1f),
            accent: new Color(1f, 0.5f, 0.15f, 1f),
            sb: sb);

        BuildMenuScene(
            path: "Assets/Game/Scenes/Win.unity",
            title: "YOU WIN!",
            subtitle: "You survived the lava. Thanks for playing!",
            buttonLabel: "PLAY AGAIN",
            loadScene: "Lava",
            autoLoadDelay: 0f,
            bg: new Color(0.05f, 0.06f, 0.12f, 1f),
            accent: new Color(1f, 0.85f, 0.2f, 1f),
            sb: sb);

        // Register both in build settings if missing
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        void Ensure(string p)
        {
            if (!scenes.Exists(s => s.path == p))
                scenes.Insert(0, new EditorBuildSettingsScene(p, true));
        }
        Ensure("Assets/Game/Scenes/Win.unity");
        Ensure("Assets/Game/Scenes/StartScreen.unity");
        EditorBuildSettings.scenes = scenes.ToArray();
        sb.AppendLine("Build settings updated with StartScreen + Win.");

        return sb.ToString();
    }

    static void BuildMenuScene(string path, string title, string subtitle, string buttonLabel,
        string loadScene, float autoLoadDelay, Color bg, Color accent, StringBuilder sb)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera with solid background
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        cam.orthographic = true;
        camGO.tag = "MainCamera";

        // EventSystem
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Title
        CreateText(canvasGO.transform, "Title", title, 110, FontStyle.Bold, accent,
            new Vector2(0.5f, 0.75f), new Vector2(1400, 200));

        // Subtitle
        CreateText(canvasGO.transform, "Subtitle", subtitle, 42, FontStyle.Normal, Color.white,
            new Vector2(0.5f, 0.58f), new Vector2(1400, 120));

        // Button
        var btnGO = new GameObject("PlayButton");
        btnGO.transform.SetParent(canvasGO.transform, false);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = accent;
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0.35f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.sizeDelta = new Vector2(420, 110);
        btnRT.anchoredPosition = Vector2.zero;
        var btn = btnGO.AddComponent<Button>();

        CreateText(btnGO.transform, "Label", buttonLabel, 48, FontStyle.Bold, new Color(0.1f, 0.05f, 0.02f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(420, 110));

        // Loader logic
        var loader = canvasGO.AddComponent<MenuSceneButton>();
        loader.SceneToLoad = loadScene;
        loader.LoadingSceneName = "LoadingScreen";
        loader.AutoLoadDelay = autoLoadDelay;

        // Wire button -> loader.Load()
        var call = new UnityEngine.Events.UnityAction(loader.Load);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, call);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);
        sb.AppendLine($"Built {System.IO.Path.GetFileNameWithoutExtension(path)} (button -> '{loadScene}')");
    }

    static Text CreateText(Transform parent, string name, string content, int size, FontStyle style,
        Color color, Vector2 anchor, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        return txt;
    }
}
