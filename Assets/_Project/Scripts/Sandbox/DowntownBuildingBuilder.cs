using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Construtor de edifícios modulares urbanos utilizando o kit Downtown City MegaKit.
    /// Gera fachadas comerciais/administrativas, entradas, telhados, unidades de ar-condicionado e postes/floreiras urbanas.
    /// </summary>
    public static class DowntownBuildingBuilder
    {
        private static Material matMetal;
        private static Material matTrim;
        private static Material matRoof;
        private static Material matConcrete;

        private static void EnsureMaterials()
        {
            if (matMetal != null) return;

            // Carregar texturas PBR de Resources
            var texMetalBase = Resources.Load<Texture2D>("Models/Structures/DowntownCity/Textures/T_MetalConcrete_BaseColor");
            var texMetalNorm = Resources.Load<Texture2D>("Models/Structures/DowntownCity/Textures/T_MetalConcrete_Normal");

            var texTrimBase = Resources.Load<Texture2D>("Models/Structures/DowntownCity/Textures/T_Trim_BaseColor");
            var texTrimNorm = Resources.Load<Texture2D>("Models/Structures/DowntownCity/Textures/T_Trim_Normal");

            var texRoofBase = Resources.Load<Texture2D>("Models/Structures/DowntownCity/Textures/T_RoofSlate_BaseColor");
            var texRoofNorm = Resources.Load<Texture2D>("Models/Structures/DowntownCity/Textures/T_RoofSlate_Normal");

            var texConcBase = Resources.Load<Texture2D>("Models/Structures/DowntownCity/Textures/T_Concrete_BaseColor");
            var texConcNorm = Resources.Load<Texture2D>("Models/Structures/DowntownCity/Textures/T_Concrete_Normal");

            // Materiais URP Lit
            matMetal = URPMaterialHelper.CreateURPLitMaterial("Downtown_MetalConcrete_Mat", Color.white, 0.45f, 0.25f);
            URPMaterialHelper.SetupTextures(matMetal, texMetalBase, texMetalNorm);

            matTrim = URPMaterialHelper.CreateURPLitMaterial("Downtown_Trim_Mat", Color.white, 0.35f, 0.10f);
            URPMaterialHelper.SetupTextures(matTrim, texTrimBase, texTrimNorm);

            matRoof = URPMaterialHelper.CreateURPLitMaterial("Downtown_Roof_Mat", Color.white, 0.25f, 0.05f);
            URPMaterialHelper.SetupTextures(matRoof, texRoofBase, texRoofNorm);

            matConcrete = URPMaterialHelper.CreateURPLitMaterial("Downtown_Concrete_Mat", Color.white, 0.20f, 0.0f);
            URPMaterialHelper.SetupTextures(matConcrete, texConcBase, texConcNorm);
        }

        private static GameObject LoadPiece(string name)
        {
            return Resources.Load<GameObject>($"Models/Structures/DowntownCity/{name}");
        }

        /// <summary>
        /// Constrói um complexo predial modular urbano na posição e dimensões indicadas.
        /// </summary>
        public static GameObject BuildBuilding(Transform parent, Vector3 position, Vector3 footprintSize, Color tintColor, string buildingName)
        {
            EnsureMaterials();

            var buildingRoot = new GameObject(buildingName);
            buildingRoot.transform.SetParent(parent);
            buildingRoot.transform.position = position;

            float width = Mathf.Max(footprintSize.x, 12f);
            float height = Mathf.Max(footprintSize.y, 8f);
            float length = Mathf.Max(footprintSize.z, 12f);

            // Carregar peças modulares
            var wallPrefab = LoadPiece("Metal_FirstFloor_Wall") ?? LoadPiece("Trim_FirstFloor_Wall");
            var winPrefab = LoadPiece("Metal_FirstFloor_Window") ?? LoadPiece("Trim_FirstFloor_Window_001");
            var cornerPrefab = LoadPiece("Trim_Corner");
            var doorPrefab = LoadPiece("Door_1") ?? LoadPiece("Entrance_Concrete_2x1");
            var roofPrefab = LoadPiece("Roof_Slate_Center");
            var acPrefab = LoadPiece("Prop_ACUnit");
            var bollardPrefab = LoadPiece("Prop_Bollard");
            var planterPrefab = LoadPiece("Prop_Planter_Single");

            // Corpo principal / Fundação com colisor
            var baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "Building_CoreVolume";
            baseBox.transform.SetParent(buildingRoot.transform);
            baseBox.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            baseBox.transform.localScale = new Vector3(width, height, length);

            var coreMat = URPMaterialHelper.CreateURPLitMaterial($"{buildingName}_CoreMat", tintColor, 0.25f, 0.05f);
            baseBox.GetComponent<Renderer>().sharedMaterial = coreMat;

            // Se as peças modulares estiverem disponíveis, adiciona detalhes de fachada e cobertura
            float moduleSize = 3.0f;
            int countX = Mathf.FloorToInt(width / moduleSize);
            int countZ = Mathf.FloorToInt(length / moduleSize);

            // Fachada frontal (Z negativo)
            if (wallPrefab != null)
            {
                var facadeFront = new GameObject("Facade_Front").transform;
                facadeFront.SetParent(buildingRoot.transform);
                facadeFront.localPosition = Vector3.zero;

                for (int x = -countX / 2; x <= countX / 2; x++)
                {
                    Vector3 piecePos = new Vector3(x * moduleSize, 0f, -length * 0.5f);
                    GameObject prefabToSpawn = (x == 0 && doorPrefab != null) ? doorPrefab : (Mathf.Abs(x) % 2 == 1 && winPrefab != null ? winPrefab : wallPrefab);
                    
                    var inst = Object.Instantiate(prefabToSpawn, facadeFront);
                    inst.transform.localPosition = piecePos;
                    inst.transform.localRotation = Quaternion.identity;
                    ApplyDowntownMaterial(inst);
                }
            }

            // Cantos ornamentados
            if (cornerPrefab != null)
            {
                Vector3[] cornerOffsets = new Vector3[]
                {
                    new Vector3(-width * 0.5f, 0f, -length * 0.5f),
                    new Vector3( width * 0.5f, 0f, -length * 0.5f),
                    new Vector3(-width * 0.5f, 0f,  length * 0.5f),
                    new Vector3( width * 0.5f, 0f,  length * 0.5f)
                };

                for (int c = 0; c < cornerOffsets.Length; c++)
                {
                    var cornerInst = Object.Instantiate(cornerPrefab, buildingRoot.transform);
                    cornerInst.transform.localPosition = cornerOffsets[c];
                    ApplyDowntownMaterial(cornerInst);
                }
            }

            // Unidades de climatização (AC) no telhado
            if (acPrefab != null)
            {
                int acCount = Mathf.Clamp(Mathf.FloorToInt((width * length) / 250f), 1, 4);
                for (int a = 0; a < acCount; a++)
                {
                    Vector3 acPos = new Vector3(
                        (a - acCount / 2f + 0.5f) * (width * 0.35f),
                        height,
                        Random.Range(-length * 0.25f, length * 0.25f)
                    );
                    var acInst = Object.Instantiate(acPrefab, buildingRoot.transform);
                    acInst.transform.localPosition = acPos;
                    acInst.transform.localScale = Vector3.one * 1.2f;
                    ApplyDowntownMaterial(acInst);
                }
            }

            // Postes de proteção (bollards) e floreiras ao longo da calçada frontal
            if (bollardPrefab != null || planterPrefab != null)
            {
                var streetProps = new GameObject("StreetProps_Front").transform;
                streetProps.SetParent(buildingRoot.transform);
                streetProps.localPosition = Vector3.zero;

                for (int b = -countX / 2; b <= countX / 2; b++)
                {
                    Vector3 propPos = new Vector3(b * moduleSize, 0f, -length * 0.5f - 1.8f);
                    GameObject toSpawn = (Mathf.Abs(b) % 3 == 0 && planterPrefab != null) ? planterPrefab : bollardPrefab;
                    if (toSpawn != null)
                    {
                        var propInst = Object.Instantiate(toSpawn, streetProps);
                        propInst.transform.localPosition = propPos;
                        ApplyDowntownMaterial(propInst);
                    }
                }
            }

            return buildingRoot;
        }

        private static void ApplyDowntownMaterial(GameObject root)
        {
            if (root == null) return;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                string rName = rend.gameObject.name.ToLower();
                if (rName.Contains("roof") || rName.Contains("slate"))
                    rend.sharedMaterial = matRoof;
                else if (rName.Contains("trim") || rName.Contains("ornament"))
                    rend.sharedMaterial = matTrim;
                else if (rName.Contains("concrete") || rName.Contains("stair") || rName.Contains("entrance"))
                    rend.sharedMaterial = matConcrete;
                else
                    rend.sharedMaterial = matMetal;
            }
        }
    }
}
