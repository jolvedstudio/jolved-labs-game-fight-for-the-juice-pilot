using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BuildHumanPlayerController
{
    public static string Execute()
    {
        const string dir = "Assets/Game/Animators";
        const string ctrlPath = dir + "/SciFiHumanPlayer.controller";
        const string animDir = "Assets/Chars/Sci Fi Character 2D/Player (Red)/Animations/";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        AnimationClip Load(string n) => AssetDatabase.LoadAssetAtPath<AnimationClip>(animDir + n + ".anim");

        var idle = Load("Idle");
        var run = Load("Run");
        var jump = Load("Jump");
        var shoot = Load("Shoot");
        var crouch = Load("Crouch");
        var dash = Load("Dash");
        var death = Load("Death");
        var hurt = Load("Hurt");

        string report = $"idle={idle!=null} run={run!=null} jump={jump!=null} shoot={shoot!=null} crouch={crouch!=null} dash={dash!=null} death={death!=null} hurt={hurt!=null}";

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);

        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Walking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Running", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Firing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Crouching", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Dashing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("ySpeed", AnimatorControllerParameterType.Float);

        var sm = controller.layers[0].stateMachine;

        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sRun = sm.AddState("Run"); sRun.motion = run ?? idle;
        var sJump = sm.AddState("Jump"); sJump.motion = jump;
        var sShoot = sm.AddState("Shoot"); sShoot.motion = shoot ?? idle;
        var sCrouch = sm.AddState("Crouch"); sCrouch.motion = crouch ?? idle;
        var sDash = sm.AddState("Dash"); sDash.motion = dash ?? run ?? idle;
        sm.defaultState = sIdle;

        // Idle <-> Run on Walking
        var t1 = sIdle.AddTransition(sRun); t1.hasExitTime=false; t1.duration=0.05f; t1.AddCondition(AnimatorConditionMode.If,0,"Walking");
        var t2 = sRun.AddTransition(sIdle); t2.hasExitTime=false; t2.duration=0.05f; t2.AddCondition(AnimatorConditionMode.IfNot,0,"Walking");

        // Jump on not grounded
        var tj = sm.AddAnyStateTransition(sJump); tj.hasExitTime=false; tj.duration=0.05f; tj.canTransitionToSelf=false;
        tj.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
        var tjb = sJump.AddTransition(sIdle); tjb.hasExitTime=false; tjb.duration=0.05f; tjb.AddCondition(AnimatorConditionMode.If,0,"Grounded");

        // Shoot on Firing
        var ts = sm.AddAnyStateTransition(sShoot); ts.hasExitTime=false; ts.duration=0.02f; ts.canTransitionToSelf=false;
        ts.AddCondition(AnimatorConditionMode.If,0,"Firing");
        var tsb = sShoot.AddTransition(sIdle); tsb.hasExitTime=false; tsb.duration=0.05f;
        tsb.AddCondition(AnimatorConditionMode.IfNot,0,"Firing"); tsb.AddCondition(AnimatorConditionMode.If,0,"Grounded");

        // Crouch
        var tc = sm.AddAnyStateTransition(sCrouch); tc.hasExitTime=false; tc.duration=0.05f; tc.canTransitionToSelf=false;
        tc.AddCondition(AnimatorConditionMode.If,0,"Crouching");
        var tcb = sCrouch.AddTransition(sIdle); tcb.hasExitTime=false; tcb.duration=0.05f; tcb.AddCondition(AnimatorConditionMode.IfNot,0,"Crouching");

        // Dash
        var td = sm.AddAnyStateTransition(sDash); td.hasExitTime=false; td.duration=0.02f; td.canTransitionToSelf=false;
        td.AddCondition(AnimatorConditionMode.If,0,"Dashing");
        var tdb = sDash.AddTransition(sIdle); tdb.hasExitTime=false; tdb.duration=0.05f; tdb.AddCondition(AnimatorConditionMode.IfNot,0,"Dashing");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return $"Built {ctrlPath}. Clips: {report}";
    }
}
