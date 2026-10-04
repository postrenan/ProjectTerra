using System;
using UnityEngine;
using ProjectTerra.Planet;
using ProjectTerra.Planet.TerrainStreaming;

namespace ProjectTerra.Sandbox
{
    public partial class InGameCommandConsole
    {
        #region Comandos de Estradas (Tipos de Pavimento e Faixas)

        private void CmdRoad(string[] args)
        {
            var rsm = RegionalSandboxManager.Instance;
            if (args.Length == 0)
            {
                LogMessage("═════════ SISTEMA DE ESTRADAS & RODOVIAS ═════════", Color.cyan);
                LogMessage($"🛣️ <b>Superfície Atual:</b> {RegionalSandboxManager.GlobalPreferredSurface}  •  <b>Faixas:</b> {(int)RegionalSandboxManager.GlobalPreferredLanes} faixas", Color.white);
                LogMessage("📐 <b>Tipos Suportados:</b> Asfalto, Concreto, Terra (chão batido), Britas (cascalho)", Color.yellow);
                LogMessage("🚦 <b>Faixas Suportadas:</b> 2 faixas (simples), 4 faixas (dupla), 6 faixas (expressa), 8 faixas (super rodovia)", Color.yellow);
                LogMessage("💡 <b>Uso:</b>", Color.gray);
                LogMessage("  • /road spawn <asfalto|concreto|terra|brita> <2|4|6|8>  <i>(Gera pista de teste à frente)</i>", Color.white);
                LogMessage("  • /road type <asfalto|concreto|terra|brita>  <i>(Altera o pavimento padrão da região)</i>", Color.white);
                LogMessage("  • /road lanes <2|4|6|8>  <i>(Altera o número de faixas padrão)</i>", Color.white);
                LogMessage("  • /road rebuild  <i>(Reconstrói a malha regional com as configurações)</i>", Color.white);
                return;
            }

            string sub = args[0].ToLower();

            if (sub == "spawn" || sub == "criar" || sub == "gerar")
            {
                RoadSurfaceType surf = RegionalSandboxManager.GlobalPreferredSurface;
                RoadLaneCount lanes = RegionalSandboxManager.GlobalPreferredLanes;

                if (args.Length > 1)
                {
                    surf = ParseSurfaceType(args[1], surf);
                }
                if (args.Length > 2)
                {
                    lanes = ParseLaneCount(args[2], lanes);
                }

                var player = PlayerCharacterController.Instance;
                Vector3 origin = (player != null) ? player.transform.position : Vector3.zero;
                Vector3 fwd = (player != null) ? player.transform.forward : Vector3.forward;

                Func<Vector3, float> heightFunc = rsm != null ? rsm.GetTerrainHeight : (Func<Vector3, float>)(v => 0f);
                var roadObj = RoadMeshBuilder.SpawnShowcaseRoad(origin, fwd, surf, lanes, heightFunc);

                LogMessage($"✨ Pista de teste gerada à sua frente: <b>{surf}</b> com <b>{(int)lanes} faixas</b>!", Color.green);
                SandboxHUD.Instance?.ShowToast($"🛣️ Estrada Gerada: {surf} ({(int)lanes} Faixas)");
                return;
            }

            if (sub == "type" || sub == "tipo" || sub == "pavimento")
            {
                if (args.Length < 2)
                {
                    LogMessage("Uso: /road type <asfalto | concreto | terra | brita>", Color.yellow);
                    return;
                }

                RoadSurfaceType newSurf = ParseSurfaceType(args[1], RegionalSandboxManager.GlobalPreferredSurface);
                RegionalSandboxManager.GlobalPreferredSurface = newSurf;
                RegionalSandboxManager.UseCustomGlobalRoadSettings = true;

                LogMessage($"🛣️ Tipo de estrada global alterado para: <b>{newSurf}</b>.", Color.green);
                LogMessage("💡 Digite <b>/road rebuild</b> para atualizar a malha existente.", Color.yellow);
                SandboxHUD.Instance?.ShowToast($"🛣️ Pavimento: {newSurf}");
                return;
            }

            if (sub == "lanes" || sub == "faixas")
            {
                if (args.Length < 2)
                {
                    LogMessage("Uso: /road lanes <2 | 4 | 6 | 8>", Color.yellow);
                    return;
                }

                RoadLaneCount newLanes = ParseLaneCount(args[1], RegionalSandboxManager.GlobalPreferredLanes);
                RegionalSandboxManager.GlobalPreferredLanes = newLanes;
                RegionalSandboxManager.UseCustomGlobalRoadSettings = true;

                LogMessage($"🛣️ Configuração de faixas alterada para: <b>{(int)newLanes} faixas</b>.", Color.green);
                LogMessage("💡 Digite <b>/road rebuild</b> para atualizar a malha existente.", Color.yellow);
                SandboxHUD.Instance?.ShowToast($"🛣️ Faixas: {(int)newLanes}");
                return;
            }

            if (sub == "rebuild" || sub == "reconstruir" || sub == "reload")
            {
                if (rsm != null)
                {
                    rsm.RebuildRoadNetwork();
                    LogMessage("🔄 Malha viária regional reconstruída com sucesso sem vãos!", Color.green);
                    SandboxHUD.Instance?.ShowToast("🔄 Malha Viária Reconstruída!");
                }
                else
                {
                    LogMessage("❌ Gerenciador Regional (RegionalSandboxManager) não está ativo na cena.", Color.red);
                }
                return;
            }

            if (sub == "list" || sub == "tipos")
            {
                LogMessage("📋 <b>Superfícies:</b> Asfalto (rodovias), Concreto (vias expressas), Terra (rural batido), Brita (cascalho compactado).", Color.cyan);
                LogMessage("📋 <b>Faixas:</b> 2 (pista simples), 4 (pista dupla), 6 (autoestrada com barreira), 8 (super tronco metropolitano).", Color.white);
                return;
            }

            LogMessage($"❌ Subcomando desconhecido: '{sub}'. Digite /road para ver as opções.", Color.red);
        }

        private static RoadSurfaceType ParseSurfaceType(string str, RoadSurfaceType defaultType)
        {
            string s = str.ToLower();
            if (s.Contains("concreto") || s.Contains("concrete") || s.Contains("cimento"))
                return RoadSurfaceType.Concrete;
            if (s.Contains("terra") || s.Contains("dirt") || s.Contains("chao") || s.Contains("solo") || s.Contains("barro"))
                return RoadSurfaceType.Dirt;
            if (s.Contains("brita") || s.Contains("britas") || s.Contains("gravel") || s.Contains("cascalho") || s.Contains("pedra"))
                return RoadSurfaceType.Gravel;
            if (s.Contains("asfalto") || s.Contains("asphalt") || s.Contains("piche"))
                return RoadSurfaceType.Asphalt;

            return defaultType;
        }

        private static RoadLaneCount ParseLaneCount(string str, RoadLaneCount defaultCount)
        {
            if (int.TryParse(str, out int val))
            {
                if (val <= 2) return RoadLaneCount.TwoLanes;
                if (val <= 4) return RoadLaneCount.FourLanes;
                if (val <= 6) return RoadLaneCount.SixLanes;
                return RoadLaneCount.EightLanes;
            }
            return defaultCount;
        }

        #endregion

        #region Comandos de Pintura do Chão / Terreno

        private void CmdPaint(string[] args)
        {
            var rsm = RegionalSandboxManager.Instance;
            var terrain = rsm != null ? rsm.activeTerrain : Terrain.activeTerrain;
            if (terrain == null)
            {
                LogMessage("❌ Nenhum terreno ativo encontrado.", Color.red);
                return;
            }

            if (args.Length == 0)
            {
                LogMessage("═════════ PINTURA & SUBSTITUIÇÃO DE CHÃO ═════════", Color.cyan);
                LogMessage("🎨 <b>Uso:</b> /paint <textura> [raio_metros]", Color.white);
                LogMessage("  • <i>Exemplos:</i> /paint concreto 25  |  /paint terra 30  |  /paint asfalto 15  |  /paint grama 40", Color.yellow);
                LogMessage("  • <b>/paint tool</b> ou pressione <b>[P]</b> para abrir o pincel interativo 3D com mouse!", Color.green);
                LogMessage("  • <b>/paint list</b> para ver todas as 16 texturas disponíveis.", Color.white);
                return;
            }

            string sub = args[0].ToLower();

            if (sub == "tool" || sub == "pincel" || sub == "mouse" || sub == "gui")
            {
                GroundPainterTool.Instance?.ToggleTool();
                bool active = GroundPainterTool.Instance != null && GroundPainterTool.Instance.isToolActive;
                LogMessage($"🎨 Pincel interativo 3D: {(active ? "ATIVADO (Pressione [P] para fechar)" : "DESATIVADO")}", active ? Color.green : Color.yellow);
                return;
            }

            if (sub == "list" || sub == "texturas" || sub == "tipos")
            {
                LogMessage("📋 <b>16 TEXTURAS DISPONÍVEIS DE CHÃO:</b>", Color.cyan);
                for (int i = 0; i < TerrainGroundPainter.AvailableTextures.Length; i++)
                {
                    var t = TerrainGroundPainter.AvailableTextures[i];
                    LogMessage($"  <b>[{i}] {t.displayName}:</b> {t.description}", Color.white);
                }
                LogMessage("💡 Pressione [P] no jogo para selecionar visualmente com prévia!", Color.yellow);
                return;
            }

            // Identificar camada
            int layerIdx = TerrainGroundPainter.FindLayerIndex(sub, -1);
            if (layerIdx < 0)
            {
                LogMessage($"❌ Textura '{sub}' não reconhecida. Digite /paint list para ver as opções.", Color.red);
                return;
            }

            float radius = 18f;
            if (args.Length > 1 && float.TryParse(args[1], out float customRadius))
            {
                radius = Mathf.Clamp(customRadius, 2f, 150f);
            }

            var player = PlayerCharacterController.Instance;
            Vector3 center = player != null ? player.transform.position : Vector3.zero;

            bool success = TerrainGroundPainter.PaintGroundAtWorldPos(terrain, center, layerIdx, radius, 1.0f, updateGrass: true);
            if (success)
            {
                var info = TerrainGroundPainter.AvailableTextures[layerIdx];
                LogMessage($"🎨 Chão aos seus pés pintado com sucesso: <b>{info.displayName}</b> (Raio: {radius:F0}m)!", Color.green);
                SandboxHUD.Instance?.ShowToast($"🎨 Chão Pintado: {info.displayName} ({radius:F0}m)");
            }
            else
            {
                LogMessage("❌ Falha ao aplicar pintura no terreno.", Color.red);
            }
        }

        #endregion
    }
}
