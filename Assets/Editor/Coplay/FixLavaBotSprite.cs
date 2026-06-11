using UnityEngine;
using UnityEditor;

/// <summary>
/// Fixes the lavaBot.png import settings for correct transparency.
/// </summary>
public class FixLavaBotSprite
{
    const string SPRITE_PATH = "Assets/CorgiEngine/Demos/Corgi2D/Sprites/Enemies/lavaBot.png";

    public static string Execute()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SPRITE_PATH) as TextureImporter;
        if (importer == null)
            return "ERROR: Could not find texture importer for " + SPRITE_PATH;

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.alphaSource         = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.isReadable          = false;
        importer.mipmapEnabled       = false;
        importer.filterMode          = FilterMode.Point;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        return "SUCCESS: lavaBot.png import settings fixed — alpha transparency enabled, uncompressed, point filter.";
    }
}
