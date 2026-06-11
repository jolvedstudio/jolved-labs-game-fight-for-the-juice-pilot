using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MoreMountains.CorgiEngine;

public class FixEnemyHeightAll
{
    static string FixOne(string enemyPath, float scale)
    {
        var enemy = GameObject.Find(enemyPath);
        if (enemy == null) return $"{enemyPath}: NOT FOUND";
        if (enemy.transform.Find("Visual") != null) return $"{enemyPath}: already fixed";

        var sr = enemy.GetComponent<SpriteRenderer>();
        var anim = enemy.GetComponent<Animator>();
        if (sr == null || anim == null) return $"{enemyPath}: missing SR/Animator on root";

        var ctrl = anim.runtimeAnimatorController;
        var sprite = sr.sprite;
        int sortOrder = sr.sortingOrder;
        int sortLayer = sr.sortingLayerID;
        var updateMode = anim.updateMode;
        var cullingMode = anim.cullingMode;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(enemy.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = new Vector3(scale, scale, scale);

        var vsr = visual.AddComponent<SpriteRenderer>();
        vsr.sprite = sprite;
        vsr.sortingOrder = sortOrder;
        vsr.sortingLayerID = sortLayer;

        var vanim = visual.AddComponent<Animator>();
        vanim.runtimeAnimatorController = ctrl;
        vanim.updateMode = updateMode;
        vanim.cullingMode = cullingMode;

        Object.DestroyImmediate(anim);
        Object.DestroyImmediate(sr);

        var character = enemy.GetComponent<Character>();
        if (character != null)
        {
            var so = new SerializedObject(character);
            var animProp = so.FindProperty("CharacterAnimator");
            if (animProp != null) animProp.objectReferenceValue = vanim;
            var modelProp = so.FindProperty("CharacterModel");
            if (modelProp != null) modelProp.objectReferenceValue = visual;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(character);
        }

        return $"{enemyPath}: fixed, visible height={vsr.bounds.size.y:0.00}";
    }

    public static string Execute()
    {
        const float scale = 2.00f / 5.02f; // ~0.398
        var sb = new StringBuilder();
        sb.AppendLine(FixOne("Level/Enemies/blueRobot 1", scale));
        sb.AppendLine(FixOne("Level/Enemies/blueRobot 2", scale));
        sb.AppendLine(FixOne("Level/Enemies/blueRobot 3", scale));

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return sb.ToString();
    }
}
