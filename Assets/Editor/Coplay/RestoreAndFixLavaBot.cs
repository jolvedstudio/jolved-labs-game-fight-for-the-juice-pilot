using UnityEngine;
using UnityEditor;
using System.IO;
using System.Net;

/// <summary>
/// Downloads the original lavaBot sprite (with transparent background from Gemini)
/// and sets correct import settings.
/// </summary>
public class RestoreAndFixLavaBot
{
    const string SPRITE_PATH = "Assets/CorgiEngine/Demos/Corgi2D/Sprites/Enemies/lavaBot.png";
    const string IMAGE_URL   = "https://gqoqjkkptwfbkwyssmnj.supabase.co/storage/v1/object/sign/coplay-prod/generated-images/2026/06/08/d52fbd10-0af8-4654-8000-fb69dfdf951f.png?token=eyJraWQiOiJzdG9yYWdlLXVybC1zaWduaW5nLWtleV84OTViMjlkNC00ZDM2LTQwZjItOTQyMC0xOTA0OWZjMDJiYzgiLCJhbGciOiJIUzI1NiJ9.eyJ1cmwiOiJjb3BsYXktcHJvZC9nZW5lcmF0ZWQtaW1hZ2VzLzIwMjYvMDYvMDgvZDUyZmJkMTAtMGFmOC00NjU0LTgwMDAtZmI2OWRmZGY5NTFmLnBuZyIsImlhdCI6MTc4MDk2Mjc4MiwiZXhwIjoxNzk2NTE0NzgyfQ.yDBOG-F2gjxD2KgJ3tnG9yzD_v9P9x4MOvMLnR0mGw8";

    public static string Execute()
    {
        // ── 1. Download the original transparent PNG from the generation URL ──
        string fullPath = Path.GetFullPath(SPRITE_PATH);

        try
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(IMAGE_URL, fullPath);
            }
        }
        catch (System.Exception e)
        {
            return "ERROR downloading image: " + e.Message;
        }

        AssetDatabase.ImportAsset(SPRITE_PATH, ImportAssetOptions.ForceUpdate);

        // ── 2. Set correct import settings ────────────────────────────────────
        TextureImporter importer = AssetImporter.GetAtPath(SPRITE_PATH) as TextureImporter;
        if (importer == null)
            return "ERROR: Could not get TextureImporter.";

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.alphaSource         = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.isReadable          = false;
        importer.mipmapEnabled       = false;
        importer.filterMode          = FilterMode.Point;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize      = 512;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        return "SUCCESS: lavaBot.png restored from source and reimported with correct transparency settings.";
    }
}
