using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ProjectTerra.Editor
{
    // Sem [InitializeOnLoad]: antes o static constructor chamava ExecuteSetup em
    // TODA recompilação/domínio reload, executando 5x SaveAndReimport + 5x Refresh +
    // 5 regenerações de controller sem nenhuma verificação. Um crash do Editor no meio
    // deixava os assets pela metade, e o AssetDatabase.Refresh() no meio da execução
    // podia disparar outro reload que abortava o setup. Agora só roda a pedido
    // explícito pelo menu Tools/Fauna/Setup Animal Animations.
    public static class AnimalAnimationSetup
    {
        private const string ANIMATORS_DIR = "Assets/_Project/Resources/Models/Animals/Animators";

        [MenuItem("Tools/Fauna/Setup Animal Animations")]
        public static void ExecuteSetup()
        {
            Debug.Log("[AnimalAnimationSetup] Iniciando configuração de animações dos animais...");
            EnsureDirectories();

            string[] animalFbxPaths = new string[]
            {
                "Assets/_Project/Resources/Models/Animals/Farm/Cow.fbx",
                "Assets/_Project/Resources/Models/Animals/Farm/Horse.fbx",
                "Assets/_Project/Resources/Models/Animals/Farm/Sheep.fbx",
                "Assets/_Project/Resources/Models/Animals/Wild/Red Fox.fbx",
                "Assets/_Project/Resources/Models/Animals/Wild/Wolf.fbx"
            };

            // Também modelos em Models/Animals se existirem
            string[] extraFbxPaths = new string[]
            {
                "Assets/_Project/Models/Animals/Farm/Cow.fbx",
                "Assets/_Project/Models/Animals/Farm/Horse.fbx",
                "Assets/_Project/Models/Animals/Farm/Sheep.fbx",
                "Assets/_Project/Models/Animals/Wild/Red Fox.fbx",
                "Assets/_Project/Models/Animals/Wild/Wolf.fbx"
            };

            foreach (var path in animalFbxPaths)
            {
                SetupFbxClips(path);
            }

            foreach (var path in extraFbxPaths)
            {
                if (File.Exists(path))
                {
                    SetupFbxClips(path);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Gerar Animator Controllers
            foreach (var path in animalFbxPaths)
            {
                CreateAnimatorControllerForModel(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AnimalAnimationSetup] Configuração de animações concluída com sucesso!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Models/Animals/Animators"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Models/Animals"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project/Resources/Models", "Animals");
                }
                AssetDatabase.CreateFolder("Assets/_Project/Resources/Models/Animals", "Animators");
            }
        }

        private static void SetupFbxClips(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[AnimalAnimationSetup] Importer não encontrado para {fbxPath}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;

            var defaultClips = importer.defaultClipAnimations;
            if (defaultClips == null || defaultClips.Length == 0)
            {
                Debug.LogWarning($"[AnimalAnimationSetup] Nenhum clip padrão encontrado em {fbxPath}");
                return;
            }

            Debug.Log($"[AnimalAnimationSetup] {fbxPath} possui {defaultClips.Length} takes padrão.");

            var newClips = new List<ModelImporterClipAnimation>();
            foreach (var srcClip in defaultClips)
            {
                string takeName = srcClip.takeName;
                if (string.IsNullOrEmpty(takeName)) continue;

                // Extrair nome amigável (ex: "Armature|Idle" -> "Idle", "WolfArmature|Walking" -> "Walk")
                string cleanName = takeName;
                int pipeIdx = cleanName.IndexOf('|');
                if (pipeIdx >= 0)
                {
                    cleanName = cleanName.Substring(pipeIdx + 1);
                }

                // Normalizações comuns
                if (cleanName.Equals("Walking", System.StringComparison.OrdinalIgnoreCase))
                    cleanName = "Walk";
                else if (cleanName.Equals("ArmatureAction", System.StringComparison.OrdinalIgnoreCase))
                    cleanName = "Walk"; // Raposa tem caminhada no ArmatureAction

                var clip = new ModelImporterClipAnimation
                {
                    name = cleanName,
                    takeName = takeName,
                    firstFrame = srcClip.firstFrame,
                    lastFrame = srcClip.lastFrame,
                    wrapMode = WrapMode.Default
                };

                // Configurar looping para animações de ciclo
                bool shouldLoop = cleanName.Contains("Idle") ||
                                  cleanName.Contains("Walk") ||
                                  cleanName.Contains("Run") ||
                                  cleanName.Contains("Swim") ||
                                  cleanName.Contains("Fly");

                if (shouldLoop)
                {
                    clip.loopTime = true;
                    clip.loopPose = true;
                }

                newClips.Add(clip);
                Debug.Log($"   -> Configurado clip: '{clip.name}' (take: '{clip.takeName}', frames: {clip.firstFrame}-{clip.lastFrame}, loop: {clip.loopTime})");
            }

            importer.clipAnimations = newClips.ToArray();
            importer.SaveAndReimport();
        }

        private static void CreateAnimatorControllerForModel(string fbxPath)
        {
            string modelName = Path.GetFileNameWithoutExtension(fbxPath).Replace(" ", "");
            string controllerPath = $"{ANIMATORS_DIR}/{modelName}Controller.controller";

            // Carregar todos os sub-assets para encontrar os AnimationClips importados
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var clips = new Dictionary<string, AnimationClip>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var a in allAssets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    clips[c.name] = c;
                    Debug.Log($"   Encontrado sub-clip '{c.name}' no asset {fbxPath}");
                }
            }

            if (clips.Count == 0)
            {
                Debug.LogWarning($"[AnimalAnimationSetup] Nenhum clip carregado para gerar controller em {fbxPath}");
                return;
            }

            // Criar ou sobrescrever AnimatorController
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;

            // Parâmetros do Animator
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            // IsRunning e IsGrazing sao escritos todo frame por AnimalController.cs
            // (graze e corrida). Sem declara-los aqui, o Unity loga "parameter not
            // found" e o estado de pastoreio nunca fica expressavel no grafo.
            controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsGrazing", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            // Identificar clip Idle e Walk
            AnimationClip idleClip = null;
            AnimationClip walkClip = null;
            AnimationClip runClip = null;

            // Passe 1: resolve walk por nome especifico. O matcher anterior aceitava "run"
            // aqui, entao se o Run fosse enumerado primeiro ele virava o clip de
            // caminhada -- e o estado "Run" deixava de existir, porque runClip
            // acabava igual a walkClip. Resultado no Cow.controller commitado:
            // o estado Walk apontava para o clip Run e todos os animais galopavam.
            foreach (var kvp in clips)
            {
                string name = kvp.Key.ToLower();
                if (idleClip == null && name.Contains("idle")) idleClip = kvp.Value;
                if (walkClip == null && (name.Contains("walk") || name.Contains("action"))) walkClip = kvp.Value;
            }

            // Passe 2: so agora escolhe o run, pulando o clip que ja virou walk.
            foreach (var kvp in clips)
            {
                if (kvp.Value == walkClip) continue;
                if (kvp.Key.ToLower().Contains("run")) { runClip = kvp.Value; break; }
            }

            // Fallback: se não achou idle ou walk especificamente, use os disponíveis
            if (idleClip == null)
            {
                foreach (var c in clips.Values) { idleClip = c; break; }
            }
            if (walkClip == null)
            {
                walkClip = idleClip;
            }

            // Criar estados
            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;

            // Transição Idle -> Walk
            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.25f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

            // Transição Walk -> Idle
            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.25f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            // Estado Run se disponível
            if (runClip != null && runClip != walkClip)
            {
                var runState = rootStateMachine.AddState("Run");
                runState.motion = runClip;

                var toRun = walkState.AddTransition(runState);
                toRun.hasExitTime = false;
                toRun.duration = 0.2f;
                toRun.AddCondition(AnimatorConditionMode.Greater, 3.0f, "Speed");

                var runToWalk = runState.AddTransition(walkState);
                runToWalk.hasExitTime = false;
                runToWalk.duration = 0.25f;
                runToWalk.AddCondition(AnimatorConditionMode.Less, 3.0f, "Speed");

                var runToIdle = runState.AddTransition(idleState);
                runToIdle.hasExitTime = false;
                runToIdle.duration = 0.25f;
                runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            }

            rootStateMachine.defaultState = idleState;
            EditorUtility.SetDirty(controller);
            Debug.Log($"[AnimalAnimationSetup] Criado AnimatorController em {controllerPath} (Idle: {idleClip?.name}, Walk: {walkClip?.name})");
        }
    }
}
