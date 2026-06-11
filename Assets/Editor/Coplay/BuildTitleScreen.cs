using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LavaRun;

/// <summary>
/// Builds the "Fight for the Juice" intro / title screen:
///  - Game title + tagline
///  - Short story blurb (Mars colony / abandoned robot plant / hunt for juice=energy)
///  - Character portraits (hero + enemy)
///  - PLAY button (loads first level)
///  - "Choose your Player (Coming soon)" disabled button
/// </summary>
public class BuildTitleScreen
{
    const string ScenePath = "Assets/Game/Scenes/StartScreen.unity";
    const string HeroSprite = "Assets/TheMech/png/trooper/Idle__000.png";
    const string HumanSprite = "Assets/Chars/Sci Fi Character 2D/Player (Red)/Sprites/Idle0001.png";
    const string EnemySprite = "Assets/CorgiEngine/Demos/Corgi2D/Sprites/Enemies/lavaBot.png";

    public static string Execute()
    {
        var sb = new StringBuilder();

        var bg = new Color(0.04f, 0.03f, 0.06f, 1f);
        var accent = new Color(1f, 0.62f, 0.12f, 1f);     // juice/energy orange
        var accent2 = new Color(0.35f, 0.9f, 1f, 1f);      // cool tech cyan

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- Camera ----
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        cam.orthographic = true;
        camGO.tag = "MainCamera";

        // ---- EventSystem ----
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // ---- Canvas ----
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ---- Background gradient panels (cheap vignette feel) ----
        var bgPanel = CreatePanel(canvasGO.transform, "BackdropGlow", new Color(0.10f, 0.05f, 0.02f, 0.6f),
            new Vector2(0.5f, 0.5f), new Vector2(1700, 700), new Vector2(0, 60));

        // ---- Title ----
        CreateText(canvasGO.transform, "Title", "FIGHT FOR THE JUICE", 120, FontStyle.Bold, accent,
            new Vector2(0.5f, 0.86f), new Vector2(1700, 200));

        // ---- Tagline ----
        CreateText(canvasGO.transform, "Tagline", "Escape the plant. Find the juice. Stay powered.", 40,
            FontStyle.Italic, accent2, new Vector2(0.5f, 0.76f), new Vector2(1500, 80));

        // ---- Story blurb ----
        string story =
            "Distant future. Humanity's Mars colony runs on JUICE \u2014 raw energy.\n" +
            "Deep in an abandoned robot manufacturing plant, a lone worker-bot flickers awake,\n" +
            "its power cells almost dry. The assembly line still hums with hostile machines.\n" +
            "Fight through the dark, drain every drop of juice you can find,\n" +
            "and escape the plant before your lights go out for good.";
        CreateText(canvasGO.transform, "Story", story, 33, FontStyle.Normal, new Color(0.85f, 0.85f, 0.9f, 1f),
            new Vector2(0.5f, 0.55f), new Vector2(1500, 360));

        // ---- Character portraits ----
        // Hero (left)
        CreateCharacter(canvasGO.transform, "Hero", HeroSprite, "THE WORKER-BOT", accent2,
            new Vector2(0.22f, 0.34f), 360f);
        // Enemy (right)
        CreateCharacter(canvasGO.transform, "Enemy", EnemySprite, "FACTORY DRONES", new Color(1f, 0.3f, 0.25f),
            new Vector2(0.78f, 0.34f), 320f);

        // ---- PLAY button ----
        var loaderHost = canvasGO.AddComponent<MenuSceneButton>();
        loaderHost.SceneToLoad = "Lava";
        loaderHost.LoadingSceneName = "LoadingScreen";
        loaderHost.AutoLoadDelay = 0f;

        var playBtn = CreateButton(canvasGO.transform, "PlayButton", "PLAY", accent,
            new Color(0.1f, 0.05f, 0.02f, 1f), new Vector2(0.5f, 0.14f), new Vector2(440, 110), true);
        var call = new UnityEngine.Events.UnityAction(loaderHost.Load);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(playBtn.onClick, call);

        // ---- "Choose your Player (Coming soon)" disabled button ----
        var chooseBtn = CreateButton(canvasGO.transform, "ChoosePlayerButton", "CHOOSE YOUR PLAYER \u2014 COMING SOON",
            new Color(0.25f, 0.25f, 0.30f, 1f), new Color(0.65f, 0.65f, 0.7f, 1f),
            new Vector2(0.5f, 0.06f), new Vector2(720, 70), false);
        chooseBtn.interactable = false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        sb.AppendLine("Built 'Fight for the Juice' title screen at " + ScenePath);

        // ---- Ensure build settings: StartScreen first, then levels ----
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool Has(string p) => scenes.Exists(s => s.path == p);
        // Make StartScreen index 0
        scenes.RemoveAll(s => s.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        sb.AppendLine("StartScreen set as first build scene.");

        return sb.ToString();
    }

    // ---------------- helpers ----------------

    static Image CreatePanel(Transform parent, string name, Color color, Vector2 anchor, Vector2 size, Vector2 offset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
        return img;
    }

    static void CreateCharacter(Transform parent, string name, string spritePath, string label, Color labelColor,
        Vector2 anchor, float height)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        var holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        var hrt = holder.AddComponent<RectTransform>();
        hrt.anchorMin = hrt.anchorMax = anchor;
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.sizeDelta = new Vector2(height, height);
        hrt.anchoredPosition = Vector2.zero;

        var imgGO = new GameObject("Portrait");
        imgGO.transform.SetParent(holder.transform, false);
        var img = imgGO.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
            // size proportional to sprite aspect
            float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            var irt = imgGO.GetComponent<RectTransform>();
            irt.sizeDelta = new Vector2(height * aspect, height);
            irt.anchoredPosition = new Vector2(0, 30);
        }
        else
        {
            img.color = new Color(0.3f, 0.3f, 0.35f, 1f);
        }

        CreateText(holder.transform, "Name", label, 30, FontStyle.Bold, labelColor,
            new Vector2(0.5f, 0f), new Vector2(420, 60)).rectTransform.anchoredPosition = new Vector2(0, -40);
    }

    static Button CreateButton(Transform parent, string name, string label, Color bg, Color textColor,
        Vector2 anchor, Vector2 size, bool bold)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        var btn = go.AddComponent<Button>();
        CreateText(go.transform, "Label", label, bold ? 48 : 30, bold ? FontStyle.Bold : FontStyle.Normal,
            textColor, new Vector2(0.5f, 0.5f), size);
        return btn;
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
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        return txt;
    }
}
