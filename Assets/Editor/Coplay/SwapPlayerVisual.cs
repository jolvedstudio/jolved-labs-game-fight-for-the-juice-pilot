using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SwapPlayerVisual
{
    public static string Execute()
    {
        const string variantPath = "Assets/Game/Prefabs/MechTrooperPlayer.prefab";
        const string ctrlPath = "Assets/Game/Animators/MechTrooperPlayer.controller";
        const string idleFrame = "Assets/TheMech/png/trooper/Idle__000.png";

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (controller == null) return "ERROR: trooper controller missing";

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(idleFrame);
        if (sprite == null)
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(idleFrame))
                if (o is Sprite s) { sprite = s; break; }
        if (sprite == null) return "ERROR: trooper idle sprite missing";

        var sb = new StringBuilder();

        // Load prefab contents for safe editing
        var root = PrefabUtility.LoadPrefabContents(variantPath);
        try
        {
            var model = root.transform.Find("ModelContainer");
            if (model == null) return "ERROR: ModelContainer not found in variant";

            // 1. Hide all existing Spine mesh renderers (keep GameObjects for jetpack emitter + weapon attach)
            int hidden = 0;
            foreach (var mr in model.GetComponentsInChildren<MeshRenderer>(true)) { mr.enabled = false; hidden++; }
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.enabled = false; hidden++; }

            // 2. Add SpriteRenderer on ModelContainer (same GO as the Animator, required by empty-path sprite curves)
            var go = model.gameObject;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 10;

            // 3. Swap the Animator controller to the Trooper one
            var animator = go.GetComponent<Animator>();
            if (animator != null) animator.runtimeAnimatorController = controller;

            // 4. Scale tuning. ModelContainer currently 0.3. Sprite is 100 px/unit.
            // Measure sprite world height at scale 1, then pick container scale to hit ~0.85 unit tall target.
            float spriteUnitsTall = sprite.rect.height / sprite.pixelsPerUnit; // e.g. 3.0
            float targetTall = 0.95f;
            float newScale = targetTall / spriteUnitsTall;
            model.localScale = new Vector3(newScale, newScale, newScale);
            sb.AppendLine($"sprite px={sprite.rect.width}x{sprite.rect.height}, unitsTall={spriteUnitsTall:0.00}, newScale={newScale:0.000}");

            PrefabUtility.SaveAsPrefabAsset(root, variantPath);
            sb.AppendLine($"Hidden renderers={hidden}. SpriteRenderer+Animator set on ModelContainer. Saved {variantPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return sb.ToString();
    }
}
