using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Audio
{
    /// <summary>
    /// Serviço central de áudio do Project Terra.
    /// Carrega de forma otimizada os áudios de Resources/Audio/ e fornece métodos estáticos
    /// para disparo de sons 3D e gerenciamento de trilhas e efeitos.
    /// </summary>
    public static class TerraAudioService
    {
        // Clips cacheados
        public static AudioClip TractorEngineLoop { get; private set; }
        public static AudioClip CowMoo { get; private set; }
        public static AudioClip SheepSound { get; private set; }
        public static AudioClip SheepBaa { get; private set; }
        public static AudioClip BirdsForestLoop { get; private set; }
        public static AudioClip BirdGreatTit { get; private set; }
        public static AudioClip BirdDoveCall { get; private set; }
        public static AudioClip BirdCrowCall { get; private set; }
        public static AudioClip FarmRoosterCrow { get; private set; }

        private static bool isInitialized = false;
        private static readonly List<AudioClip> birdClips = new List<AudioClip>();

        static TerraAudioService()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (isInitialized) return;

            TractorEngineLoop = Resources.Load<AudioClip>("Audio/Vehicles/tractor_engine_loop");
            CowMoo = Resources.Load<AudioClip>("Audio/Animals/cow_moo");
            SheepSound = Resources.Load<AudioClip>("Audio/Animals/sheep_sound");
            SheepBaa = Resources.Load<AudioClip>("Audio/Animals/sheep_baa");

            BirdsForestLoop = Resources.Load<AudioClip>("Audio/Ambient/birds_forest_loop");
            BirdGreatTit = Resources.Load<AudioClip>("Audio/Ambient/bird_great_tit");
            BirdDoveCall = Resources.Load<AudioClip>("Audio/Ambient/bird_dove_call");
            BirdCrowCall = Resources.Load<AudioClip>("Audio/Ambient/bird_crow_call");
            FarmRoosterCrow = Resources.Load<AudioClip>("Audio/Ambient/farm_rooster_crow");

            birdClips.Clear();
            if (BirdGreatTit != null) birdClips.Add(BirdGreatTit);
            if (BirdDoveCall != null) birdClips.Add(BirdDoveCall);
            if (BirdCrowCall != null) birdClips.Add(BirdCrowCall);

            isInitialized = true;
            Debug.Log("[TerraAudioService] Sistema de áudio inicializado com sucesso.");
        }

        /// <summary>
        /// Reproduz um som 3D no espaço do mundo com atenuação e destruição automática.
        /// </summary>
        public static AudioSource Play3D(AudioClip clip, Vector3 position, float volume = 1.0f, float minDistance = 3f, float maxDistance = 60f, float pitch = 1.0f)
        {
            if (clip == null) return null;

            GameObject tempGO = new GameObject($"Audio_3D_{clip.name}");
            tempGO.transform.position = position;

            AudioSource source = tempGO.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.5f, 2.0f);
            source.spatialBlend = 1.0f; // 100% 3D
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.dopplerLevel = 0.5f;
            source.playOnAwake = false;

            source.Play();
            GameObject.Destroy(tempGO, clip.length / Mathf.Max(0.1f, source.pitch) + 0.2f);

            return source;
        }

        /// <summary>
        /// Toca um canto aleatório de pássaro (chapim, pomba ou corvo) próximo à posição informada.
        /// </summary>
        public static void PlayRandomBird(Vector3 nearPosition, float volume = 0.6f)
        {
            if (birdClips.Count == 0) return;
            AudioClip clip = birdClips[UnityEngine.Random.Range(0, birdClips.Count)];
            Vector3 offset = UnityEngine.Random.insideUnitSphere * 15f;
            offset.y = Mathf.Abs(offset.y) + 5f; // Sempre acima no céu / copa das árvores
            float pitch = UnityEngine.Random.Range(0.92f, 1.08f);
            Play3D(clip, nearPosition + offset, volume, 5f, 120f, pitch);
        }

        /// <summary>
        /// Toca o canto do galo da fazenda.
        /// </summary>
        public static void PlayRoosterCrow(Vector3 farmPosition, float volume = 0.8f)
        {
            if (FarmRoosterCrow == null) return;
            float pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            Play3D(FarmRoosterCrow, farmPosition, volume, 10f, 200f, pitch);
        }

        /// <summary>
        /// Toca o mugido da vaca com variação natural de pitch.
        /// </summary>
        public static void PlayCowMoo(Vector3 position, float volume = 0.8f)
        {
            if (CowMoo == null) return;
            float pitch = UnityEngine.Random.Range(0.92f, 1.06f);
            Play3D(CowMoo, position, volume, 3f, 50f, pitch);
        }

        /// <summary>
        /// Toca o balido da ovelha com variação entre os dois sons.
        /// </summary>
        public static void PlaySheepBaa(Vector3 position, float volume = 0.75f)
        {
            AudioClip clip = (UnityEngine.Random.value > 0.5f && SheepBaa != null) ? SheepBaa : SheepSound;
            if (clip == null) return;
            float pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            Play3D(clip, position, volume, 3f, 45f, pitch);
        }
    }
}
