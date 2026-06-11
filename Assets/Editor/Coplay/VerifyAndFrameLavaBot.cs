using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class VerifyAndFrameLavaBot
{
    public static string Execute()
    {
        // Check sprite asset
        string spritePath = "Assets/CorgiEngine/Demos/Corgi2D/Sprites/Enemies/lavaBot.png";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
        if (tex == null)
            return "ERROR: Texture not found.";

        string texInfo = $"Texture: {tex.width}x{tex.height}, format={tex.format}, alphaIsTransparency check via importer";

        // Check importer settings
        TextureImporter imp = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        string impInfo = imp != null
            ? $"alphaSource={imp.alphaSource}, alphaIsTransparency={imp.alphaIsTransparency}, textureType={imp.textureType}, readable={imp.isReadable}"
            : "importer not found";

        // Check the SpriteRenderer on LavaBot 0
        GameObject lavaBot = GameObject.Find("LavaBot 0");
        string goInfo = "LavaBot 0 not found in scene";
        if (lavaBot != null)
        {
            SpriteRenderer sr = lavaBot.GetComponent<SpriteRenderer>();
            goInfo = sr != null
                ? $"SpriteRenderer found. Sprite={(sr.sprite != null ? sr.sprite.name : "NULL")}, enabled={sr.enabled}, color={sr.color}"
                : "No SpriteRenderer on LavaBot 0";

            // Frame it in scene view
            Selection.activeGameObject = lavaBot;
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                sv.FrameSelected();
                sv.Repaint();
            }
        }

        return $"{texInfo}\n{impInfo}\n{goInfo}";
    }
}
