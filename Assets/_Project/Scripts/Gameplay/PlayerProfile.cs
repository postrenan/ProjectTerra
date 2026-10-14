using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectTerra.Gameplay
{
    /// <summary>
    /// Progresso de conta do jogador — o que viaja entre regiões. Fica num único
    /// arquivo (profile.bin), acima dos saves por região (save_*.bin).
    ///
    /// Princípio: a <b>perícia</b> acompanha o jogador (ele aprendeu a pilotar, não
    /// esquece ao mudar de região), mas a <b>operação</b> (dinheiro, dívidas, base,
    /// frota, reputação) continua ancorada em cada <see cref="RegionSaveData"/>.
    ///
    /// Regra das licenças (v1): uma licença passa a valer no mundo todo quando é
    /// <b>comprada</b> (ver <see cref="LearnLicense"/>). A licença gratuita da carreira
    /// inicial de cada save permanece local àquele save — assim começar numa região
    /// nova ainda exige escolher uma carreira e montar a operação do zero, enquanto o
    /// que foi conquistado com esforço continua valendo.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        /// <summary>Perícia acumulada no mundo todo (bitflags de <see cref="DriverLicense"/>).</summary>
        public int globalLicenses = 0;

        /// <summary>Renome global (meta-reputação). Reservado para o sistema de prestígio.</summary>
        public int renown = 0;

        /// <summary>Poupança que viaja entre regiões (transferência com atrito). Reservado.</summary>
        public long globalBank = 0;

        /// <summary>Progresso de domínio por região (carreiras concluídas). Reservado.</summary>
        public List<RegionMastery> mastery = new List<RegionMastery>();

        public DriverLicense GlobalLicenses
        {
            get => (DriverLicense)globalLicenses;
            set => globalLicenses = (int)value;
        }

        /// <summary>Marca uma licença como aprendida para sempre (vale em qualquer região).</summary>
        public void LearnLicense(DriverLicense lic) => globalLicenses |= (int)lic;

        public bool KnowsLicense(DriverLicense lic) => ((DriverLicense)globalLicenses & lic) == lic;

        /// <summary>Quantas regiões têm as 4 carreiras dominadas.</summary>
        public int MasteredRegionCount
        {
            get
            {
                int n = 0;
                if (mastery != null)
                    foreach (var m in mastery)
                        if (m != null && m.AllFourDone) n++;
                return n;
            }
        }

        public RegionMastery GetOrCreateMastery(int regionId)
        {
            if (mastery == null) mastery = new List<RegionMastery>();
            foreach (var m in mastery)
                if (m != null && m.regionId == regionId) return m;
            var created = new RegionMastery { regionId = regionId };
            mastery.Add(created);
            return created;
        }

        public const byte CurrentVersion = 1;

        public void WriteBinary(BinaryWriter writer)
        {
            writer.Write(globalLicenses);
            writer.Write(renown);
            writer.Write(globalBank);

            int count = mastery != null ? mastery.Count : 0;
            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                var m = mastery[i];
                writer.Write(m.regionId);
                writer.Write(m.careersMask);
            }
        }

        /// <summary>
        /// Lê um perfil. Uma versão mais recente que a suportada é recusada (devolve null)
        /// em vez de ser lida fora de fase, no mesmo espírito de <see cref="RegionSaveData.ReadBinary"/>.
        /// </summary>
        public static PlayerProfile ReadBinary(BinaryReader reader, byte version)
        {
            switch (version)
            {
                case 1:
                    return ReadBinaryV1(reader);
                default:
                    Debug.LogError($"[PlayerProfile] Perfil na versão {version} é mais recente que a suportada ({CurrentVersion}). Recusando carregar.");
                    return null;
            }
        }

        private static PlayerProfile ReadBinaryV1(BinaryReader reader)
        {
            var profile = new PlayerProfile
            {
                globalLicenses = reader.ReadInt32(),
                renown = reader.ReadInt32(),
                globalBank = reader.ReadInt64(),
            };

            int count = reader.ReadInt32();
            profile.mastery = new List<RegionMastery>(Mathf.Max(0, count));
            for (int i = 0; i < count; i++)
            {
                profile.mastery.Add(new RegionMastery
                {
                    regionId = reader.ReadInt32(),
                    careersMask = reader.ReadInt32(),
                });
            }
            return profile;
        }
    }

    /// <summary>Domínio de carreiras numa região: um bit por <see cref="StarterCareer"/>.</summary>
    [Serializable]
    public class RegionMastery
    {
        public int regionId;
        public int careersMask; // bit (1 &lt;&lt; (int)StarterCareer) por carreira concluída

        public bool HasCareer(StarterCareer c) => (careersMask & (1 << (int)c)) != 0;
        public void MarkCareer(StarterCareer c) => careersMask |= (1 << (int)c);

        /// <summary>True quando as 4 carreiras (Farmer, Trucker, Aviator, Fisherman) estão concluídas.</summary>
        public bool AllFourDone => (careersMask & 0b1111) == 0b1111;
    }
}
