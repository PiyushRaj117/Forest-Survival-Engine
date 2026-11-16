#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ForestSurvival.EditorTools
{
    public static class AnimatorSetupUtility
    {
        private const string ControllerPath = "Assets/_Project/Ellen_AnimatorController.controller";
        private const string DeathAnimPath = "Assets/3D Game Kit - Character Pack/Characters/Ellen/AnimationClips/@EllenDeath.fbx";
        private const string Combo1AnimPath = "Assets/3D Game Kit - Character Pack/Characters/Ellen/AnimationClips/@EllenCombo1.fbx";
        private const string JumpAnimPath = "Assets/3D Game Kit - Character Pack/Characters/Ellen/AnimationClips/@EllenJump.fbx";

        [MenuItem("Tools/Forest Survival/Setup Character Animations (Jump, Attack, Death)")]
        public static void SetupAnimator()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("Animator Controller not found at: " + ControllerPath);
                return;
            }

            // 1. Ensure Parameters Exist
            EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Jump", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Respawn", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            // Load Animation Clips
            AnimationClip deathClip = LoadSubClip(DeathAnimPath);
            AnimationClip attackClip = LoadSubClip(Combo1AnimPath);
            AnimationClip jumpClip = LoadSubClip(JumpAnimPath);

            // 2. Setup Attack State
            AnimatorState attackState = FindOrCreateState(sm, "EllenAttack", attackClip);
            attackState.writeDefaultValues = true;

            // 3. Setup Death State
            AnimatorState deathState = FindOrCreateState(sm, "EllenDeath", deathClip);
            deathState.writeDefaultValues = true;

            // Find Idle State
            AnimatorState idleState = null;
            foreach (var state in sm.states)
            {
                if (state.state.name == "EllenIdle")
                {
                    idleState = state.state;
                    break;
                }
            }

            // 4. AnyState -> Death
            AnimatorStateTransition anyToDeath = null;
            foreach (var t in sm.anyStateTransitions)
            {
                if (t.destinationState == deathState)
                {
                    anyToDeath = t;
                    break;
                }
            }
            if (anyToDeath == null)
            {
                anyToDeath = sm.AddAnyStateTransition(deathState);
                anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
                anyToDeath.duration = 0.1f;
                anyToDeath.hasExitTime = false;
            }

            // Death -> Idle (on Respawn)
            if (idleState != null)
            {
                AnimatorStateTransition deathToIdle = null;
                foreach (var t in deathState.transitions)
                {
                    if (t.destinationState == idleState)
                    {
                        deathToIdle = t;
                        break;
                    }
                }
                if (deathToIdle == null)
                {
                    deathToIdle = deathState.AddTransition(idleState);
                    deathToIdle.AddCondition(AnimatorConditionMode.If, 0, "Respawn");
                    deathToIdle.duration = 0.2f;
                    deathToIdle.hasExitTime = false;
                }
            }

            // 5. AnyState -> Attack
            AnimatorStateTransition anyToAttack = null;
            foreach (var t in sm.anyStateTransitions)
            {
                if (t.destinationState == attackState)
                {
                    anyToAttack = t;
                    break;
                }
            }
            if (anyToAttack == null)
            {
                anyToAttack = sm.AddAnyStateTransition(attackState);
                anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                anyToAttack.duration = 0.05f;
                anyToAttack.hasExitTime = false;
                anyToAttack.canTransitionToSelf = false;
            }

            // Attack -> Idle (on Exit Time)
            if (idleState != null)
            {
                AnimatorStateTransition attackToIdle = null;
                foreach (var t in attackState.transitions)
                {
                    if (t.destinationState == idleState)
                    {
                        attackToIdle = t;
                        break;
                    }
                }
                if (attackToIdle == null)
                {
                    attackToIdle = attackState.AddTransition(idleState);
                    attackToIdle.hasExitTime = true;
                    attackToIdle.exitTime = 0.85f;
                    attackToIdle.duration = 0.15f;
                }
            }

            // 6. Connect Jump Trigger
            foreach (var state in sm.states)
            {
                if (state.state.name == "EllenJumpGoesUp")
                {
                    if (jumpClip != null && state.state.motion == null)
                    {
                        state.state.motion = jumpClip;
                    }

                    // Check if AnyState -> Jump exists
                    bool hasJumpTrans = false;
                    foreach (var t in sm.anyStateTransitions)
                    {
                        if (t.destinationState == state.state)
                        {
                            hasJumpTrans = true;
                            break;
                        }
                    }
                    if (!hasJumpTrans)
                    {
                        var jumpTrans = sm.AddAnyStateTransition(state.state);
                        jumpTrans.AddCondition(AnimatorConditionMode.If, 0, "Jump");
                        jumpTrans.duration = 0.05f;
                        jumpTrans.hasExitTime = false;
                        jumpTrans.canTransitionToSelf = false;
                    }

                    // Jump -> Idle
                    if (idleState != null)
                    {
                        bool hasExit = false;
                        foreach (var t in state.state.transitions)
                        {
                            if (t.destinationState == idleState) { hasExit = true; break; }
                        }
                        if (!hasExit)
                        {
                            var exitTrans = state.state.AddTransition(idleState);
                            exitTrans.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
                            exitTrans.duration = 0.2f;
                            exitTrans.hasExitTime = false;
                        }
                    }
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#4CAF50><b>[Animator Setup]</b></color> Jump, Attack, and Death states successfully wired into Ellen_AnimatorController!");
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == name) return;
            }
            controller.AddParameter(name, type);
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string stateName, AnimationClip clip)
        {
            foreach (var s in sm.states)
            {
                if (s.state.name == stateName)
                {
                    if (clip != null) s.state.motion = clip;
                    return s.state;
                }
            }

            AnimatorState newState = sm.AddState(stateName);
            if (clip != null) newState.motion = clip;
            return newState;
        }

        private static AnimationClip LoadSubClip(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object obj in assets)
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }
    }
}
#endif
