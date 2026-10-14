using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ProjectTerra.Editor
{
    /// <summary>
    /// Utilitário para configurar importação de animações humanoides da Universal Animation Library (UAL)
    /// e gerar o AnimatorController para personagens e jogadores.
    /// Menu: Tools -> Animations -> Setup Humanoid Animations
    /// </summary>
    public static class HumanoidAnimationSetup
    {
        private const string ANIMATIONS_DIR = "Assets/_Project/Animations";
        private const string UAL_DIR = "Assets/_Project/Animations/UniversalAnimationLibrary";
        private const string UAL_FBX_PATH = "Assets/_Project/Animations/UniversalAnimationLibrary/UAL1_Standard.fbx";
        private const string UAL_RM_FBX_PATH = "Assets/_Project/Animations/UniversalAnimationLibrary/UAL1_Standard_RM.fbx";
        private const string CONTROLLER_PATH = "Assets/_Project/Animations/PlayerHumanoidController.controller";

        [MenuItem("Tools/Animations/Setup Humanoid Animations")]
        public static void ExecuteSetup()
        {
            Debug.Log("[HumanoidAnimationSetup] Iniciando configuração da Universal Animation Library (UAL)...");

            if (!File.Exists(UAL_FBX_PATH))
            {
                Debug.LogError($"[HumanoidAnimationSetup] Arquivo FBX não encontrado em: {UAL_FBX_PATH}");
                return;
            }

            // 1. Configurar importador do FBX padrão (in-place)
            SetupFbxImporter(UAL_FBX_PATH);

            if (File.Exists(UAL_RM_FBX_PATH))
            {
                SetupFbxImporter(UAL_RM_FBX_PATH);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Criar ou atualizar AnimatorController
            CreatePlayerAnimatorController(UAL_FBX_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HumanoidAnimationSetup] Configuração da UAL concluída com sucesso!");
        }

        private static void SetupFbxImporter(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[HumanoidAnimationSetup] Importer não encontrado para {fbxPath}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;

            var defaultClips = importer.defaultClipAnimations;
            if (defaultClips == null || defaultClips.Length == 0)
            {
                Debug.Log($"[HumanoidAnimationSetup] FBX {fbxPath} possui configurações padrão.");
                importer.SaveAndReimport();
                return;
            }

            Debug.Log($"[HumanoidAnimationSetup] {fbxPath} possui {defaultClips.Length} clips.");

            var newClips = new List<ModelImporterClipAnimation>();
            foreach (var srcClip in defaultClips)
            {
                string takeName = srcClip.takeName;
                if (string.IsNullOrEmpty(takeName)) continue;

                string cleanName = takeName;
                int pipeIdx = cleanName.IndexOf('|');
                if (pipeIdx >= 0) cleanName = cleanName.Substring(pipeIdx + 1);

                var clip = new ModelImporterClipAnimation
                {
                    name = cleanName,
                    takeName = takeName,
                    firstFrame = srcClip.firstFrame,
                    lastFrame = srcClip.lastFrame,
                    wrapMode = WrapMode.Default
                };

                // Configurar looping para animações de locomoção e repouso
                bool shouldLoop = cleanName.Contains("Idle") ||
                                  cleanName.Contains("Walk") ||
                                  cleanName.Contains("Run") ||
                                  cleanName.Contains("Sprint") ||
                                  cleanName.Contains("Jog") ||
                                  cleanName.Contains("Swim") ||
                                  cleanName.Contains("Crouch") ||
                                  cleanName.Contains("Drive");

                if (shouldLoop)
                {
                    clip.loopTime = true;
                    clip.loopPose = true;
                }

                newClips.Add(clip);
            }

            if (newClips.Count > 0)
            {
                importer.clipAnimations = newClips.ToArray();
            }

            importer.SaveAndReimport();
        }

        private static void CreatePlayerAnimatorController(string fbxPath)
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var clips = new Dictionary<string, AnimationClip>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var a in allAssets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    clips[c.name] = c;
                }
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
            var rootStateMachine = controller.layers[0].stateMachine;

            // Parâmetros de locomoção e estado
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsSwimming", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsDriving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            AnimationClip idleClip = FindClip(clips, "Idle");
            AnimationClip walkClip = FindClip(clips, "Walk");
            AnimationClip runClip = FindClip(clips, "Run", "Sprint");
            AnimationClip jumpClip = FindClip(clips, "Jump");
            AnimationClip swimClip = FindClip(clips, "Swim");

            // Estados
            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip ?? idleClip;

            var runState = rootStateMachine.AddState("Run");
            runState.motion = runClip ?? walkClip ?? idleClip;

            // Transições Idle <-> Walk
            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.2f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.2f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            // Transições Walk <-> Run
            var toRun = walkState.AddTransition(runState);
            toRun.hasExitTime = false;
            toRun.duration = 0.2f;
            toRun.AddCondition(AnimatorConditionMode.If, 0, "IsSprinting");

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.2f;
            runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSprinting");

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.2f;
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            // Pulo se disponível
            if (jumpClip != null)
            {
                var jumpState = rootStateMachine.AddState("Jump");
                jumpState.motion = jumpClip;

                var anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
                anyToJump.hasExitTime = false;
                anyToJump.duration = 0.15f;
                anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");

                var jumpToIdle = jumpState.AddTransition(idleState);
                jumpToIdle.hasExitTime = true;
                jumpToIdle.exitTime = 0.85f;
                jumpToIdle.duration = 0.2f;
                jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            }

            // Natação se disponível
            if (swimClip != null)
            {
                var swimState = rootStateMachine.AddState("Swim");
                swimState.motion = swimClip;

                var anyToSwim = rootStateMachine.AddAnyStateTransition(swimState);
                anyToSwim.hasExitTime = false;
                anyToSwim.duration = 0.25f;
                anyToSwim.AddCondition(AnimatorConditionMode.If, 0, "IsSwimming");

                var swimToIdle = swimState.AddTransition(idleState);
                swimToIdle.hasExitTime = false;
                swimToIdle.duration = 0.25f;
                swimToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSwimming");
            }

            rootStateMachine.defaultState = idleState;
            EditorUtility.SetDirty(controller);
            Debug.Log($"[HumanoidAnimationSetup] AnimatorController gerado em {CONTROLLER_PATH} com {clips.Count} animações mapeadas.");
        }

        private static AnimationClip FindClip(Dictionary<string, AnimationClip> clips, params string[] patterns)
        {
            foreach (var pat in patterns)
            {
                foreach (var kvp in clips)
                {
                    if (kvp.Key.IndexOf(pat, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return kvp.Value;
                    }
                }
            }
            return null;
        }
    }
}
