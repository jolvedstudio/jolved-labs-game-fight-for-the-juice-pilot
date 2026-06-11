using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BuildTrooperPlayerController
{
    public static string Execute()
    {
        const string dir = "Assets/Game/Animators";
        const string ctrlPath = dir + "/MechTrooperPlayer.controller";
        const string animDir = "Assets/TheMech/Animation/Trooper/";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        AnimationClip Load(string n)
        {
            var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + n + ".anim");
            return c;
        }

        var idle = Load("Idle");
        var walk = Load("Walk") ?? idle; // Trooper has no Walk? fallback
        var jump = Load("Jump");
        var shoot = Load("Shoot") ?? Load("IdleAim");
        var hurt = Load("Hurt");
        var dead = Load("Dead");

        // Trooper folder might not have Walk/Shoot named exactly; report what loaded
        string report = $"idle={(idle!=null)} jump={(jump!=null)} shoot={(shoot!=null)} hurt={(hurt!=null)} dead={(dead!=null)}";

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);

        // Corgi-driven parameters (mirror the baked corgi animator)
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Walking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Running", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jumping", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Firing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("ySpeed", AnimatorControllerParameterType.Float);

        var sm = controller.layers[0].stateMachine;

        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sWalk = sm.AddState("Walk"); sWalk.motion = walk;
        var sJump = sm.AddState("Jump"); sJump.motion = jump;
        var sShoot = sm.AddState("Shoot"); sShoot.motion = shoot;
        sm.defaultState = sIdle;

        // Idle <-> Walk on Walking bool
        var t1 = sIdle.AddTransition(sWalk); t1.hasExitTime = false; t1.duration = 0.05f;
        t1.AddCondition(AnimatorConditionMode.If, 0, "Walking");
        var t2 = sWalk.AddTransition(sIdle); t2.hasExitTime = false; t2.duration = 0.05f;
        t2.AddCondition(AnimatorConditionMode.IfNot, 0, "Walking");

        // AnyState -> Jump when not grounded
        var tj = sm.AddAnyStateTransition(sJump); tj.hasExitTime = false; tj.duration = 0.05f;
        tj.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        tj.canTransitionToSelf = false;
        // Jump -> Idle when grounded
        var tjb = sJump.AddTransition(sIdle); tjb.hasExitTime = false; tjb.duration = 0.05f;
        tjb.AddCondition(AnimatorConditionMode.If, 0, "Grounded");

        // AnyState -> Shoot when Firing
        var ts = sm.AddAnyStateTransition(sShoot); ts.hasExitTime = false; ts.duration = 0.02f;
        ts.AddCondition(AnimatorConditionMode.If, 0, "Firing");
        ts.canTransitionToSelf = false;
        var tsb = sShoot.AddTransition(sIdle); tsb.hasExitTime = false; tsb.duration = 0.05f;
        tsb.AddCondition(AnimatorConditionMode.IfNot, 0, "Firing");
        tsb.AddCondition(AnimatorConditionMode.If, 0, "Grounded");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        return $"Built {ctrlPath}. Clips: {report}";
    }
}
