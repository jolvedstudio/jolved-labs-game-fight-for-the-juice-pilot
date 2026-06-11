using System.IO;
using UnityEngine;
using UnityEditor;

public class CaptureGameCamera
{
    public static string Execute()
    {
        var camGo = GameObject.Find("Main Camera");
        if (camGo == null) return "No Main Camera";
        var cam = camGo.GetComponent<Camera>();

        int w = 1024, h = 1024;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        cam.targetTexture = prev;
        RenderTexture.active = null;

        // sample average brightness + a few pixels to verify it's not a flat blue screen
        var px = tex.GetPixels();
        float r=0,g=0,b=0; 
        for (int i=0;i<px.Length;i++){r+=px[i].r;g+=px[i].g;b+=px[i].b;}
        r/=px.Length; g/=px.Length; b/=px.Length;

        string outPath = "Assets/Editor/Coplay/_gamecam.png";
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(outPath);

        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);

        return $"Game camera avg color = ({r:F3},{g:F3},{b:F3}). Saved {outPath}. " +
               (r>0.4f&&b>0.4f&&g>0.4f ? "WARNING: looks flat/bright (possible blank)." : "Looks like dark lit scene (good).");
    }
}
