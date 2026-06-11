using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using MoreMountains.CorgiEngine;

public class ScaleCorgiCharacter
{
    public static string Execute()
    {
        string prefabPath = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/PlayableCharacters/spine-space-corgi.prefab";

        // Load and open the prefab for editing
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
            return "ERROR: Could not load prefab at: " + prefabPath;

        // Edit the prefab directly
        using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = editScope.prefabContentsRoot;

            // --- 1. Scale the root transform to match the cat's visual size ---
            // Cat collider height: 0.7100, Corgi collider height: 2.3667
            // Scale factor: 0.7100 / 2.3667 = 0.3
            float scaleFactor = 0.3f;
            root.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

            // --- 2. Adjust the BoxCollider2D to match the cat's collider exactly ---
            // The collider is on the root. Since we scaled the transform by 0.3,
            // the collider size in local space needs to be the cat's size / scaleFactor
            // so that in world space it equals the cat's collider size.
            // Cat collider: width=0.31, height=0.71
            BoxCollider2D col = root.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.size = new Vector2(0.31f / scaleFactor, 0.71f / scaleFactor);
                col.offset = new Vector2(0f, 0f);
            }

            // --- 3. Adjust CorgiController parameters to match the cat ---
            CorgiController controller = root.GetComponent<CorgiController>();
            if (controller != null)
            {
                controller.NumberOfHorizontalRays = 4;
                controller.NumberOfVerticalRays = 4;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return $"SUCCESS: spine-space-corgi scaled to {0.3f} (matching spine-space-cat collider height). Prefab saved.";
    }
}
