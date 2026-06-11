using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using MoreMountains.CorgiEngine;

public class BuildHumanPlayerVariant
{
    public static string Execute()
    {
        const string basePrefab = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/PlayableCharacters/spine-space-cat.prefab";
        const string variantPath = "Assets/Game/Prefabs/SciFiHumanPlayer.prefab";
        const string ctrlPath = "Assets/Game/Animators/SciFiHumanPlayer.controller";
        const string idleFrame = "Assets/Chars/Sci Fi Character 2D/Player (Red)/Sprites/Idle0001.png";

        var sb = new StringBuilder();

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (controller == null) return "ERROR: human controller missing";

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(idleFrame);
        if (sprite == null)
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(idleFrame))
                if (o is Sprite s) { sprite = s; break; }
        if (sprite == null) return "ERROR: human idle sprite missing";

        // Create the variant fresh from the ORIGINAL space-cat (not my mech variant)
        var baseGo = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefab);
        if (baseGo == null) return "ERROR: base spine-space-cat prefab missing";

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(baseGo);
        try
        {
            // Target original player visible height = 0.85 units. Human opaque height = 7.20 units.
            const float targetVisibleHeight = 0.85f;
            const float humanOpaqueHeight = 7.20f;   // measured
            const float humanCanvasHeight = 10.80f;  // measured (1080px/100)
            float scale = targetVisibleHeight / humanOpaqueHeight; // ~0.118

            var model = instance.transform.Find("ModelContainer");
            if (model == null) return "ERROR: ModelContainer not found";

            // Hide all Spine renderers (keep GOs for jetpack emitter + weapon attach)
            int hidden = 0;
            foreach (var mr in model.GetComponentsInChildren<MeshRenderer>(true)) { mr.enabled = false; hidden++; }
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.enabled = false; hidden++; }

            // Add SpriteRenderer + Animator on ModelContainer (empty-path sprite curves require same GO)
            var go = model.gameObject;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 10;

            var animator = go.GetComponent<Animator>();
            if (animator != null) animator.runtimeAnimatorController = controller;

            // Apply scale
            model.localScale = new Vector3(scale, scale, scale);

            // Foot alignment: sprite is centered on canvas. The opaque body sits roughly centered too,
            // but the character stands on the bottom of the opaque region. The collider bottom is at
            // localY = collider.offset.y - collider.size.y/2 relative to root. We want the sprite's
            // opaque bottom to align with the collider bottom (approx root origin foot area).
            // With a centered 10.80-unit canvas at scale s, canvas center is at model.localPosition.y.
            // Opaque region is ~720px tall centered-ish; raise the model so feet ~ root bottom.
            var box = instance.GetComponent<BoxCollider2D>();
            float colliderBottom = box != null ? (box.offset.y - box.size.y / 2f) : -0.355f;
            // Opaque vertical center offset from canvas center (px): measured opaque y 720 tall in 1080 canvas.
            // Assume body roughly vertically centered; lift so opaque-bottom reaches colliderBottom.
            float scaledOpaqueHalf = (humanOpaqueHeight * scale) / 2f;
            model.localPosition = new Vector3(0f, colliderBottom + scaledOpaqueHalf, 0f);

            sb.AppendLine($"scale={scale:0.000} hidden={hidden} colliderBottom={colliderBottom:0.00} modelY={model.localPosition.y:0.00}");

            // Save as the variant prefab
            bool ok;
            PrefabUtility.SaveAsPrefabAsset(instance, variantPath, out ok);
            sb.AppendLine($"Saved variant ok={ok} at {variantPath}");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }

        return sb.ToString();
    }
}
