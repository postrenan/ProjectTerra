using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;
using ProjectTerra.Planet.TerrainStreaming;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Ferramenta interativa de pintura e substituição de texturas do terreno em tempo real.
    /// Permite pintar qualquer área com Concreto, Terra Simples, Asfalto, Brita, Grama, etc.
    /// com suporte a anel projetor 3D, controle de raio e força, atalhos de teclado e paleta gráfica.
    /// </summary>
    public class GroundPainterTool : MonoBehaviour
    {
        public static GroundPainterTool Instance { get; private set; }

        [Header("Configurações do Pincel")]
        public bool isToolActive = false;
        public int selectedLayerIndex = TerrainPBRFactory.LayerConcrete; // Padrão: Concreto Urbano (9)
        public float brushRadius = 16f;        // Raio em metros (2m a 80m)
        public float brushStrength = 0.85f;    // Intensidade de mistura (0.1 a 1.0)

        private Terrain activeTerrain;
        private LineRenderer brushRingRenderer;
        private const int RingSegments = 40;
        private Vector3 lastHitPoint;
        private bool hasHitTerrain = false;
        public bool HasHitTerrain => hasHitTerrain;

        private Rect paletteRect = new Rect(Screen.width - 340, 70, 320, 520);
        private Vector2 scrollPos;
        private bool isMouseOverGui = false;

        private void Awake()
        {
            Instance = this;
            EnsureBrushRing();
        }

        private void Start()
        {
            if (activeTerrain == null)
            {
                activeTerrain = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.activeTerrain : Terrain.activeTerrain;
            }
        }

        private void EnsureBrushRing()
        {
            if (brushRingRenderer != null) return;

            var ringObj = new GameObject("GroundBrush_ProjectorRing");
            ringObj.transform.SetParent(transform);
            brushRingRenderer = ringObj.AddComponent<LineRenderer>();
            brushRingRenderer.positionCount = RingSegments + 1;
            brushRingRenderer.loop = true;
            brushRingRenderer.startWidth = 0.35f;
            brushRingRenderer.endWidth = 0.35f;
            brushRingRenderer.useWorldSpace = true;

            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            var mat = new Material(s);
            mat.color = new Color(0.2f, 0.8f, 1f, 0.85f);
            brushRingRenderer.sharedMaterial = mat;
            brushRingRenderer.enabled = false;
        }

        public void ToggleTool()
        {
            SetToolActive(!isToolActive);
        }

        public void SetToolActive(bool active)
        {
            isToolActive = active;
            if (brushRingRenderer != null) brushRingRenderer.enabled = isToolActive;

            if (isToolActive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SandboxHUD.Instance?.ShowToast($"🎨 Pincel Ativado: {GetSelectedInfo().displayName} (Raio: {brushRadius:F0}m)");
            }
            else
            {
                hasHitTerrain = false;
                SandboxHUD.Instance?.ShowToast("🎨 Pincel de Terreno Desativado");
            }
        }

        private void Update()
        {
            // O IMGUI não consome o teclado do Input System: digitar qualquer "p" no
            // console (ex: /spawn plane) chegava em Keyboard.current.wasPressedThisFrame
            // e abria o pincel 3D, destravando o cursor com o console ainda em foco.
            // Os outros sistemas (PlowImplement, VehicleController) já checam IsInputBlocked.
            if (PlayerCharacterController.Instance != null && PlayerCharacterController.Instance.IsInputBlocked())
            {
                if (isToolActive) SetToolActive(false);
                return;
            }

            // Atalho universal para ligar/desligar o pincel de terreno: tecla [P]
            if (TerraInput.GetKeyDown(Key.P))
            {
                ToggleTool();
            }

            if (!isToolActive) return;

            if (activeTerrain == null)
            {
                activeTerrain = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.activeTerrain : Terrain.activeTerrain;
                if (activeTerrain == null) return;
            }

            // Atalhos rápidos no teclado numérico para texturas populares
            HandleQuickSelectKeys();

            // Ajuste dinâmico de raio via Scroll do Mouse (quando não sobrevoando janela OnGUI)
            if (!isMouseOverGui)
            {
                float scroll = TerraInput.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    brushRadius = Mathf.Clamp(brushRadius + scroll * 20f, 2f, 80f);
                }
            }

            // Raycast da Câmera para o Terreno
            Camera cam = Camera.main;
            if (cam == null && PlayerCharacterController.Instance != null)
            {
                cam = PlayerCharacterController.Instance.GetComponentInChildren<Camera>();
            }

            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(TerraInput.MousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 2500f))
                {
                    lastHitPoint = hit.point;
                    hasHitTerrain = true;
                    UpdateRingPositions(hit.point, brushRadius);

                    // Ação de Pintar: Botão Esquerdo mantido pressionado
                    if (TerraInput.GetMouseButton(0) && !isMouseOverGui)
                    {
                        PaintAtWorldPos(hit.point, selectedLayerIndex, brushStrength);
                    }
                    // Ação de Apagar / Restaurar Grama: Botão Direito ou Shift + Botão Esquerdo
                    else if ((TerraInput.GetMouseButton(1) || (TerraInput.GetKey(Key.LeftShift) && TerraInput.GetMouseButton(0))) && !isMouseOverGui)
                    {
                        PaintAtWorldPos(hit.point, TerrainPBRFactory.LayerGrass, brushStrength);
                    }
                }
                else
                {
                    hasHitTerrain = false;
                    if (brushRingRenderer != null) brushRingRenderer.enabled = false;
                }
            }
        }

        private void HandleQuickSelectKeys()
        {
            if (TerraInput.GetKeyDown(Key.Digit1) || TerraInput.GetKeyDown(Key.Numpad1)) SelectLayer(TerrainPBRFactory.LayerConcrete);    // 1: Concreto
            if (TerraInput.GetKeyDown(Key.Digit2) || TerraInput.GetKeyDown(Key.Numpad2)) SelectLayer(TerrainPBRFactory.LayerDirtSimple);  // 2: Terra Simples
            if (TerraInput.GetKeyDown(Key.Digit3) || TerraInput.GetKeyDown(Key.Numpad3)) SelectLayer(TerrainPBRFactory.LayerAsphalt);     // 3: Asfalto
            if (TerraInput.GetKeyDown(Key.Digit4) || TerraInput.GetKeyDown(Key.Numpad4)) SelectLayer(TerrainPBRFactory.LayerGrass);       // 4: Grama Lush
            if (TerraInput.GetKeyDown(Key.Digit5) || TerraInput.GetKeyDown(Key.Numpad5)) SelectLayer(TerrainPBRFactory.LayerGravel);      // 5: Cascalho / Britas
            if (TerraInput.GetKeyDown(Key.Digit6) || TerraInput.GetKeyDown(Key.Numpad6)) SelectLayer(TerrainPBRFactory.LayerCobblestone); // 6: Paralelepípedo
            if (TerraInput.GetKeyDown(Key.Digit7) || TerraInput.GetKeyDown(Key.Numpad7)) SelectLayer(TerrainPBRFactory.LayerSand);        // 7: Areia
            if (TerraInput.GetKeyDown(Key.Digit8) || TerraInput.GetKeyDown(Key.Numpad8)) SelectLayer(TerrainPBRFactory.LayerSoil);        // 8: Solo Arado
            if (TerraInput.GetKeyDown(Key.Digit9) || TerraInput.GetKeyDown(Key.Numpad9)) SelectLayer(TerrainPBRFactory.LayerMud);         // 9: Lama Úmida
            if (TerraInput.GetKeyDown(Key.Digit0) || TerraInput.GetKeyDown(Key.Numpad0)) SelectLayer(TerrainPBRFactory.LayerRedClay);     // 0: Argila Vermelha
        }

        public void SelectLayer(int layerIdx)
        {
            if (layerIdx >= 0 && layerIdx < TerrainGroundPainter.AvailableTextures.Length)
            {
                selectedLayerIndex = layerIdx;
                var info = TerrainGroundPainter.AvailableTextures[layerIdx];
                if (brushRingRenderer != null && brushRingRenderer.sharedMaterial != null)
                {
                    brushRingRenderer.sharedMaterial.color = new Color(info.swatchColor.r, info.swatchColor.g, info.swatchColor.b, 0.9f);
                }
                SandboxHUD.Instance?.ShowToast($"🖌️ Selecionado: {info.displayName}");
            }
        }

        public void PaintAtWorldPos(Vector3 worldPos, int layerIdx, float strength)
        {
            if (activeTerrain == null) return;
            TerrainGroundPainter.PaintGroundAtWorldPos(activeTerrain, worldPos, layerIdx, brushRadius, strength, updateGrass: true);
        }

        public void PaintPlayerArea(int layerIdx, float radius)
        {
            var player = PlayerCharacterController.Instance;
            Vector3 pos = player != null ? player.transform.position : Vector3.zero;
            if (activeTerrain == null)
            {
                activeTerrain = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.activeTerrain : Terrain.activeTerrain;
            }
            if (activeTerrain != null)
            {
                TerrainGroundPainter.PaintGroundAtWorldPos(activeTerrain, pos, layerIdx, radius, 1.0f, updateGrass: true);
                var info = (layerIdx >= 0 && layerIdx < TerrainGroundPainter.AvailableTextures.Length) 
                    ? TerrainGroundPainter.AvailableTextures[layerIdx] : default;
                SandboxHUD.Instance?.ShowToast($"🎨 Área pintada: {info.displayName} (R={radius:F0}m)");
            }
        }

        private void UpdateRingPositions(Vector3 center, float radius)
        {
            if (brushRingRenderer == null) return;
            brushRingRenderer.enabled = true;

            var info = GetSelectedInfo();
            Color ringColor = info.swatchColor;
            ringColor.a = 0.90f;
            brushRingRenderer.startColor = ringColor;
            brushRingRenderer.endColor = ringColor;

            for (int i = 0; i <= RingSegments; i++)
            {
                float angle = (i / (float)RingSegments) * Mathf.PI * 2f;
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;
                float y = center.y + 0.15f;

                if (activeTerrain != null)
                {
                    y = activeTerrain.SampleHeight(new Vector3(x, 0f, z)) + activeTerrain.transform.position.y + 0.15f;
                }

                brushRingRenderer.SetPosition(i, new Vector3(x, y, z));
            }
        }

        public GroundTextureInfo GetSelectedInfo()
        {
            if (selectedLayerIndex >= 0 && selectedLayerIndex < TerrainGroundPainter.AvailableTextures.Length)
            {
                return TerrainGroundPainter.AvailableTextures[selectedLayerIndex];
            }
            return TerrainGroundPainter.AvailableTextures[0];
        }

        #region Interface Gráfica OnGUI

        private void OnGUI()
        {
            if (!isToolActive) return;

            paletteRect.x = Screen.width - 340;
            isMouseOverGui = paletteRect.Contains(Event.current.mousePosition);

            GUI.skin.window.fontSize = 12;
            paletteRect = GUI.Window(8891, paletteRect, DrawPaletteWindow, "🎨 Pincel de Terreno (Pressione [P] p/ Fechar)");
        }

        private void DrawPaletteWindow(int windowId)
        {
            var info = GetSelectedInfo();

            // Cabeçalho da textura ativa
            GUILayout.BeginHorizontal();
            // Swatch color box
            var oldColor = GUI.color;
            GUI.color = info.swatchColor;
            GUILayout.Box("", GUILayout.Width(28), GUILayout.Height(28));
            GUI.color = oldColor;

            GUILayout.BeginVertical();
            GUILayout.Label($"<b>{info.displayName}</b>", GUILayout.Height(18));
            GUILayout.Label($"<size=10>{info.description}</size>", GUILayout.Height(16));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Controles de Raio e Força
            GUILayout.Label($"Raio do Pincel: <b>{brushRadius:F0} metros</b> (ou use Scroll do mouse)");
            brushRadius = GUILayout.HorizontalSlider(brushRadius, 2f, 80f);

            GUILayout.Label($"Intensidade de Pintura: <b>{brushStrength * 100f:F0}%</b>");
            brushStrength = GUILayout.HorizontalSlider(brushStrength, 0.1f, 1.0f);

            GUILayout.Space(8);
            GUILayout.Label("<b>Escolha a Textura do Chão:</b>");

            // Lista rolável de texturas
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(240));
            var textures = TerrainGroundPainter.AvailableTextures;

            for (int i = 0; i < textures.Length; i++)
            {
                var t = textures[i];
                bool isSelected = (i == selectedLayerIndex);

                GUILayout.BeginHorizontal();
                var c = GUI.color;
                GUI.color = t.swatchColor;
                GUILayout.Box("", GUILayout.Width(18), GUILayout.Height(18));
                GUI.color = isSelected ? Color.cyan : c;

                string btnText = isSelected ? $"<b>▶ {t.displayName}</b>" : t.displayName;
                if (GUILayout.Button(btnText, GUILayout.Height(22)))
                {
                    SelectLayer(i);
                }
                GUI.color = c;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6);

            // Ações rápidas
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pintar Pátio do Jogador (R=25m)", GUILayout.Height(26)))
            {
                PaintPlayerArea(selectedLayerIndex, 25f);
            }
            if (GUILayout.Button("Restaurar Grama", GUILayout.Height(26)))
            {
                PaintPlayerArea(TerrainPBRFactory.LayerGrass, 35f);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Fechar Ferramenta de Pintura [P]", GUILayout.Height(24)))
            {
                SetToolActive(false);
            }

            GUI.DragWindow();
        }

        #endregion
    }
}
