using System;
using UnityEngine;
using ProjectTerra.Sandbox;

namespace ProjectTerra.Audio
{
    /// <summary>
    /// Gerenciador de Áudio Ambiente da Província Regional:
    /// - Loop suave de pássaros e floresta (com atenuação dia/noite baseada no relógio da província).
    /// - Disparos periódicos de cantos de pássaros locais em 3D ao redor do jogador.
    /// - Canto do galo ao amanhecer ou próximo a áreas rurais.
    /// </summary>
    public class AmbientAudioManager : MonoBehaviour
    {
        public static AmbientAudioManager Instance { get; private set; }

        [Header("Configurações do Ambiente")]
        [Range(0f, 1f)] public float maxAmbientVolume = 0.25f;
        [Range(0f, 1f)] public float nightAmbientVolume = 0.05f;

        private AudioSource ambientLoopSource;
        private float nextBirdTime = 5f;
        private float lastRoosterHour = -1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            TerraAudioService.Initialize();
            SetupAmbientLoop();
        }

        private void SetupAmbientLoop()
        {
            if (TerraAudioService.BirdsForestLoop == null) return;

            ambientLoopSource = gameObject.AddComponent<AudioSource>();
            ambientLoopSource.clip = TerraAudioService.BirdsForestLoop;
            ambientLoopSource.loop = true;
            ambientLoopSource.spatialBlend = 0f; // 2D estéreo ambiente
            ambientLoopSource.volume = maxAmbientVolume;
            ambientLoopSource.playOnAwake = false;
            ambientLoopSource.Play();
        }

        private void Update()
        {
            UpdateAmbientVolumeByTimeOfDay();
            UpdateProceduralBirds();
            UpdateRoosterCrow();
        }

        private void UpdateAmbientVolumeByTimeOfDay()
        {
            if (ambientLoopSource == null) return;

            float currentHour = 12f;
            if (HDRPAtmosphereController.Instance != null)
            {
                currentHour = HDRPAtmosphereController.Instance.timeOfDay;
            }

            // Dia: 06h às 18h | Noite: 19h às 05h
            bool isDay = currentHour >= 6f && currentHour <= 18f;
            float targetVolume = isDay ? maxAmbientVolume : nightAmbientVolume;
            ambientLoopSource.volume = Mathf.MoveTowards(ambientLoopSource.volume, targetVolume, Time.deltaTime * 0.1f);
        }

        private void UpdateProceduralBirds()
        {
            nextBirdTime -= Time.deltaTime;
            if (nextBirdTime <= 0f)
            {
                // Próximo canto em 12 a 28 segundos
                nextBirdTime = UnityEngine.Random.Range(12f, 28f);

                Transform listenerTransform = Camera.main != null ? Camera.main.transform : transform;
                if (listenerTransform != null)
                {
                    TerraAudioService.PlayRandomBird(listenerTransform.position, UnityEngine.Random.Range(0.4f, 0.7f));
                }
            }
        }

        private void UpdateRoosterCrow()
        {
            if (HDRPAtmosphereController.Instance == null) return;

            float currentHour = HDRPAtmosphereController.Instance.timeOfDay;
            // Toca o galo entre 05:45 e 06:15 uma vez por ciclo matinal
            if (currentHour >= 5.75f && currentHour <= 6.25f)
            {
                if (Mathf.Abs(currentHour - lastRoosterHour) > 1.0f)
                {
                    lastRoosterHour = currentHour;
                    Transform target = Camera.main != null ? Camera.main.transform : transform;
                    if (target != null)
                    {
                        TerraAudioService.PlayRoosterCrow(target.position + Vector3.forward * 40f, 0.85f);
                    }
                }
            }
        }
    }
}
