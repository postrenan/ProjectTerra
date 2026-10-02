# Graph Report - testejogo  (2026-10-02)

## Corpus Check
- Large corpus: 82 files · ~917,644 words. Semantic extraction will be expensive (many Claude tokens). Consider running on a subfolder.

## Summary
- 865 nodes · 1437 edges · 33 communities (29 shown, 4 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 65 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Python Data Pipelines
- Core Architecture & Data
- Orbital Camera Controls
- Core Package Dependencies
- Binary Serialization & Save
- Geographic Region Database
- Terrain Data & Hydrography
- Terrain Streaming Service
- Vehicle Physics & Dynamics
- HDRP Graphics Dependencies
- Planetary Mesh Generation
- Unity Package Manifest
- Territory Selection & Raycast
- Floating Origin Precision
- Player Character Controller
- Unity Editor Integration
- Runtime Package Graph A
- Runtime Package Graph B
- Runtime Package Graph C
- HDRP Atmosphere & Sun
- Sandbox HUD & Telemetry
- Runtime Package Graph D
- Terrain PBR Material Factory
- Package Registry Manifest A
- Package Registry Manifest B
- Runtime Package Graph E
- Runtime Package Graph F
- Runtime Package Graph G
- CubeSphere Quadtree Mesh
- CubeSphere Face Geometry
- Visual Studio Tooling
- VS Code Tooling
- Unity Project Solution

## God Nodes (most connected - your core abstractions)
1. `PlanetInteractionController` - 36 edges
2. `PlanetQuadTreeNode` - 34 edges
3. `CubeSpherePlanet` - 33 edges
4. `RegionalSandboxManager` - 31 edges
5. `VehicleController` - 30 edges
6. `PlayerCharacterController` - 26 edges
7. `Vector3d` - 23 edges
8. `LoadingScreenController` - 22 edges
9. `SandboxHUD` - 20 edges
10. `RegionSaveData` - 17 edges

## Surprising Connections (you probably didn't know these)
- `Save System, Career and Economy` --conceptually_related_to--> `SaveManager`  [EXTRACTED]
  README.md → Assets/_Project/Scripts/Gameplay/SaveManager.cs
- `Sandbox HUD and Telemetry` --conceptually_related_to--> `SandboxHUD`  [EXTRACTED]
  README.md → Assets/_Project/Scripts/Sandbox/SandboxHUD.cs
- `Local Persistent Binary Cache (O(1))` --semantically_similar_to--> `Binary Geographic Database (PTRD Format)`  [INFERRED] [semantically similar]
  docs/ARCHITECTURE_1_TO_1_SCALE_TERRAIN.md → README.md
- `Cinematic Orbital Camera and Dive` --conceptually_related_to--> `OrbitCameraController`  [EXTRACTED]
  README.md → Assets/_Project/Scripts/Camera/OrbitCameraController.cs
- `Floating Origin and 64-bit Precision Coordinates` --rationale_for--> `FloatingOrigin`  [EXTRACTED]
  docs/ARCHITECTURE_1_TO_1_SCALE_TERRAIN.md → Assets/_Project/Scripts/Core/FloatingOrigin.cs

## Import Cycles
- None detected.

## Communities (33 total, 4 thin omitted)

### Community 0 - "Python Data Pipelines"
Cohesion: 0.05
Nodes (32): Binary Geographic Database (PTRD Format), Python Geographic Processing Pipeline, generate_region_geography(), get_continental_elevation(), load_regions_database(), main(), download_file(), generate_regions_db_and_id_map() (+24 more)

### Community 1 - "Core Architecture & Data"
Cohesion: 0.08
Nodes (21): RegionSaveCollection, Gravel Albedo (PBR), Gravel MaskMap (PBR), Gravel Normal (PBR), Rock Albedo (PBR), Rock MaskMap (PBR), Rock Normal (PBR), Sand Albedo (PBR) (+13 more)

### Community 2 - "Orbital Camera Controls"
Cohesion: 0.06
Nodes (9): OrbitCameraController, CardMode, LoadGameList, NewGamePrompt, RegionDetails, PlanetInteractionController, Instance, RegionDatabase (+1 more)

### Community 3 - "Core Package Dependencies"
Cohesion: 0.05
Nodes (45): dependencies, depth, source, version, dependencies, depth, source, version (+37 more)

### Community 4 - "Binary Serialization & Save"
Cohesion: 0.08
Nodes (14): RegionSaveData, IsStatsModified, StarterCareer, Aviator, Farmer, Fisherman, Trucker, SaveManager (+6 more)

### Community 5 - "Geographic Region Database"
Cohesion: 0.08
Nodes (11): RegionData, BoundingArea, RegionInfrastructure, RegionInfrastructureCollection, RegionInfrastructureDatabase, LoadingScreenController, Instance, LoadingTheme (+3 more)

### Community 6 - "Terrain Data & Hydrography"
Cohesion: 0.14
Nodes (5): GameObject, GeographicDataLoader, RegionalHydroData, RegionalSandboxManager, Instance

### Community 7 - "Terrain Streaming Service"
Cohesion: 0.09
Nodes (19): TerrainDataService, Instance, High Definition Render Pipeline (HDRP), Unity 6 (6000.6.3f1), AWS Open Data Terrain Tiles (Terrarium DEM), Copernicus DEM GLO-30, GPU Displacement and Procedural Amplification, Three-Tier Hybrid Streaming Architecture (+11 more)

### Community 8 - "Vehicle Physics & Dynamics"
Cohesion: 0.11
Nodes (10): VehicleBuilder, VehicleCategory, Boat, Plane, Tractor, Truck, VehicleController, currentSpeedKmh (+2 more)

### Community 9 - "HDRP Graphics Dependencies"
Cohesion: 0.06
Nodes (33): com.unity.burst, com.unity.collections, com.unity.mathematics, com.unity.render-pipelines.core, com.unity.render-pipelines.high-definition-config, com.unity.shadergraph, com.unity.test-framework, com.unity.ugui (+25 more)

### Community 10 - "Planetary Mesh Generation"
Cohesion: 0.08
Nodes (17): PlanetQuadTreeNode, AxisA, AxisB, BoundingRadius, Children, CubeCenter, CubeSize, Depth (+9 more)

### Community 11 - "Unity Package Manifest"
Cohesion: 0.06
Nodes (31): dependencies, com.unity.burst, com.unity.cinemachine, com.unity.collections, com.unity.ide.rider, com.unity.ide.visualstudio, com.unity.ide.vscode, com.unity.inputsystem (+23 more)

### Community 12 - "Territory Selection & Raycast"
Cohesion: 0.08
Nodes (9): CubeSpherePlanet, ChunkResolution, Instance, IsRotationPaused, MaxDepth, PlanetMaterial, PlanetRadius, SplitFactor (+1 more)

### Community 13 - "Floating Origin Precision"
Cohesion: 0.08
Nodes (12): FloatingOrigin, GlobalOriginOffset, Instance, Vector3d, forward, magnitude, normalized, one (+4 more)

### Community 14 - "Player Character Controller"
Cohesion: 0.11
Nodes (6): PlayerCharacterController, Instance, nearbyVehicle, VehicleCameraMode, FirstPerson, ThirdPerson

### Community 15 - "Unity Editor Integration"
Cohesion: 0.09
Nodes (21): dependencies, com.unity.ai.navigation, com.unity.collab-proxy, com.unity.ide.rider, com.unity.ide.visualstudio, com.unity.inputsystem, com.unity.render-pipelines.universal, com.unity.test-framework (+13 more)

### Community 16 - "Runtime Package Graph A"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, version, dependencies, depth, source, version (+14 more)

### Community 17 - "Runtime Package Graph B"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, version, depth, source, version, dependencies (+13 more)

### Community 18 - "Runtime Package Graph C"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, version, dependencies, depth, source, version (+13 more)

### Community 21 - "Runtime Package Graph D"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, version, dependencies, depth, source, url (+9 more)

### Community 22 - "Terrain PBR Material Factory"
Cohesion: 0.21
Nodes (5): TerrainPBRFactory, TextureMapType, Albedo, MaskMap, Normal

### Community 23 - "Package Registry Manifest A"
Cohesion: 0.13
Nodes (16): dependencies, dependencies, depth, source, url, version, depth, dependencies (+8 more)

### Community 24 - "Package Registry Manifest B"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, url, version, dependencies, depth, source (+8 more)

### Community 25 - "Runtime Package Graph E"
Cohesion: 0.14
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 26 - "Runtime Package Graph F"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 27 - "Runtime Package Graph G"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 29 - "CubeSphere Face Geometry"
Cohesion: 0.29
Nodes (7): CubeFaceDirection, Back, Down, Forward, Left, Right, Up

### Community 30 - "Visual Studio Tooling"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.ide.visualstudio

### Community 31 - "VS Code Tooling"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.ide.vscode

## Knowledge Gaps
- **310 isolated node(s):** `Instance`, `GlobalOriginOffset`, `zero`, `one`, `up` (+305 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 419 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `dependencies` connect `Runtime Package Graph E` to `Core Package Dependencies`, `HDRP Graphics Dependencies`, `Runtime Package Graph A`, `Runtime Package Graph B`, `Runtime Package Graph C`, `Runtime Package Graph D`, `Package Registry Manifest A`, `Package Registry Manifest B`, `Runtime Package Graph F`, `Runtime Package Graph G`, `Visual Studio Tooling`, `VS Code Tooling`?**
  _High betweenness centrality (0.074) - this node is a cross-community bridge._
- **Why does `CubeSpherePlanet` connect `Territory Selection & Raycast` to `Core Architecture & Data`, `Orbital Camera Controls`, `Binary Serialization & Save`, `Terrain Streaming Service`, `Planetary Mesh Generation`?**
  _High betweenness centrality (0.072) - this node is a cross-community bridge._
- **Why does `PlanetInteractionController` connect `Orbital Camera Controls` to `Core Architecture & Data`, `Geographic Region Database`, `Binary Serialization & Save`, `Territory Selection & Raycast`?**
  _High betweenness centrality (0.055) - this node is a cross-community bridge._
- **What connects `Instance`, `GlobalOriginOffset`, `zero` to the rest of the system?**
  _310 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Python Data Pipelines` be split into smaller, more focused modules?**
  _Cohesion score 0.05407925407925408 - nodes in this community are weakly interconnected._
- **Should `Core Architecture & Data` be split into smaller, more focused modules?**
  _Cohesion score 0.07562136435748282 - nodes in this community are weakly interconnected._
- **Should `Orbital Camera Controls` be split into smaller, more focused modules?**
  _Cohesion score 0.06009783368273934 - nodes in this community are weakly interconnected._