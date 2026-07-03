using System;
using System.Collections.Generic;
using System.IO;
using InterviewDemo.FinancialDataRoom;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace InterviewDemo.FinancialDataRoom.Editor
{
    /// <summary>
    /// Creates the interview-demo scene from existing project assets without changing their sources.
    /// Re-running the command replaces only the scene at <see cref="ScenePath"/>.
    /// </summary>
    public static class FinancialDataRoomSceneBuilder
    {
        const string MenuPath = "Tools/Financial Data Room/Build Demo Scene";
        const string DemoRoot = "Assets/InterviewDemo/FinancialDataRoom";
        const string ScenePath = DemoRoot + "/Scenes/VR_Financial_Data_Room.unity";

        // Room_Modern's serialized collider shell is 6 m × 4 m (24 m²).
        // This generated shell is 19 m × 13 m (247 m², approximately 10.3× the area).
        const float PlayableWidth = 19f;
        const float PlayableDepth = 13f;
        const float FloorY = 0f;
        const float PerimeterHeight = 3.2f;
        const float DashboardZ = 5.82f;
        const float ConsoleZ = 2f;
        const float SpawnZ = -3.75f;

        // The player starts south of the presentation and looks toward world +Z.
        // "Audience facing" therefore means a surface normal pointing toward world -Z.
        static readonly Vector3 ExperienceForward = Vector3.forward;
        static readonly Vector3 AudienceFacing = Vector3.back;
        static readonly Vector3 WorldUp = Vector3.up;
        static readonly Vector3 CanvasReadableLocalFront = Vector3.back;
        static readonly Vector3 WallVisualAuthoredLocalFront = Vector3.forward;
        static readonly Vector3 TabletopVisualAuthoredLocalFront = Vector3.forward;

        const string RoomPath = "Assets/_Course Library/_Prefabs/Rooms/Room_Modern.prefab";
        const string TablePath = "Assets/_Course Library/_Prefabs/Tables/Table_Dining_Modern_Dark.prefab";
        const string TelevisionPath = "Assets/_Course Library/_Prefabs/Televisions/TV_FlatWallMounted.prefab";
        // These course-library prefabs contain reusable visuals and colliders only.
        // The verified course interaction scripts are added to generated scene instances below;
        // no component is applied to or expected on the source prefabs.
        const string KnobVisualPrefabPath =
            "Assets/_Course Library/_Prefabs/Controls/Control_Knob.prefab";
        const string SliderVisualPrefabPath =
            "Assets/_Course Library/_Prefabs/Controls/Control_Slider_Blue.prefab";
        const string LeverVisualPrefabPath =
            "Assets/_Course Library/_Prefabs/Controls/Control_Lever_Blue.prefab";
        const string RunButtonVisualPrefabPath =
            "Assets/_Course Library/_Prefabs/Controls/Control_Button_Play.prefab";
        const string ResetButtonVisualPrefabPath =
            "Assets/_Course Library/_Prefabs/Controls/Control_Button_Stop.prefab";

        const string PhysicalControlScriptPath =
            DemoRoot + "/Scripts/FinancialPhysicalControl.cs";
        const string ActionButtonScriptPath =
            DemoRoot + "/Scripts/FinancialActionButton.cs";
        const string ScreenMaterialPath =
            "Assets/_Course Library/Materials/Object Materials/Material_Screen.mat";
        const string WallMaterialPath =
            DemoRoot + "/Materials/FinancialRoom_Wall_Neutral.mat";

        const string XrOriginPath =
            "Assets/Samples/XR Interaction Toolkit/3.0.8/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string InputActionsPath =
            "Assets/Samples/XR Interaction Toolkit/3.0.8/Starter Assets/XRI Default Input Actions.inputactions";
        const string SimulatorPath =
            "Assets/Samples/XR Interaction Toolkit/3.1.1/XR Device Simulator/" +
            "XRInteractionSimulator/XR Interaction Simulator.prefab";

        static readonly Color DashboardBackground = new Color(0.018f, 0.035f, 0.065f, 0.96f);
        static readonly Color PrimaryText = new Color(0.88f, 0.96f, 1f);
        static readonly Color AccentText = new Color(0.22f, 0.90f, 0.72f);
        static readonly Color MutedText = new Color(0.55f, 0.70f, 0.80f);

        [MenuItem(MenuPath)]
        public static void BuildDemoScene()
        {
            if (!ValidateAssets(out var validationMessage))
            {
                EditorUtility.DisplayDialog("Financial Data Room", validationMessage, "OK");
                return;
            }

            var currentScene = SceneManager.GetActiveScene();
            if (!currentScene.IsValid() || string.IsNullOrEmpty(currentScene.path))
            {
                EditorUtility.DisplayDialog(
                    "Financial Data Room",
                    "Open a saved scene before running the builder. The current scene will not be modified.",
                    "OK");
                return;
            }

            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                if (!SceneManager.GetSceneAt(index).isDirty)
                    continue;

                EditorUtility.DisplayDialog(
                    "Financial Data Room",
                    "A loaded scene has unsaved changes. Save it manually, then run the builder again. " +
                    "The builder will never save or discard changes in an existing scene.",
                    "OK");
                return;
            }

            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Rebuild Financial Data Room?",
                    "This will replace only:\n\n" + ScenePath +
                    "\n\nNo prefab source, existing scene, package, or project setting will be changed.",
                    "Rebuild Scene",
                    "Cancel"))
            {
                return;
            }

            var previousScenePath = currentScene.path;

            try
            {
                EnsureSceneDirectory();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var root = new GameObject("Financial Data Room Demo");
                SceneManager.MoveGameObjectToScene(root, scene);

                var experienceLayout =
                    new GameObject("Experience Layout (+Z Camera-to-Dashboard)").transform;
                experienceLayout.SetParent(root.transform, false);
                experienceLayout.localPosition = Vector3.zero;
                experienceLayout.localRotation = Quaternion.identity;
                experienceLayout.localScale = Vector3.one;

                var roomStyleSource = BuildEnvironment(experienceLayout);
                BuildLighting(experienceLayout);
                BuildXrFoundation(experienceLayout);

                var dashboard = BuildDashboard(experienceLayout);
                BuildControlsAndController(experienceLayout, dashboard);
                DisableVerifiedRoomStyleSource(experienceLayout, roomStyleSource);

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Unity could not save the generated demo scene.");

                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(root);
                Debug.Log(
                    "Financial Data Room scene created successfully. " +
                    "Build Settings were intentionally left unchanged.\n" + ScenePath,
                    root);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath))
                    EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);

                EditorUtility.DisplayDialog(
                    "Financial Data Room Build Failed",
                    "The demo scene was not completed. The previous saved scene has been reopened.\n\n" +
                    exception.Message,
                    "OK");
            }
        }

        static bool ValidateAssets(out string message)
        {
            var missing = new List<string>();
            RequireAsset<GameObject>(RoomPath, missing);
            RequireRoomStructure(missing);
            RequireAsset<GameObject>(TablePath, missing);
            RequireAsset<GameObject>(TelevisionPath, missing);
            RequireAsset<GameObject>(XrOriginPath, missing);
            RequireAsset<InputActionAsset>(InputActionsPath, missing);
            RequireAsset<GameObject>(SimulatorPath, missing);
            RequireAsset<Material>(ScreenMaterialPath, missing);
            RequireAsset<Material>(WallMaterialPath, missing);

            RequireControlVisual(KnobVisualPrefabPath, string.Empty, missing);
            RequireControlVisual(SliderVisualPrefabPath, "Dimmer_Handle", missing);
            RequireControlVisual(LeverVisualPrefabPath, "Lever_Switch", missing);
            RequireControlVisual(RunButtonVisualPrefabPath, string.Empty, missing);
            RequireControlVisual(ResetButtonVisualPrefabPath, string.Empty, missing);

            RequireScriptType<FinancialPhysicalControl>(PhysicalControlScriptPath, missing);
            RequireScriptType<FinancialActionButton>(ActionButtonScriptPath, missing);
            ValidateFinancialModelSamples(missing);

            if (missing.Count == 0)
            {
                message = string.Empty;
                return true;
            }

            message =
                "The scene was not changed because required reusable assets are missing or invalid:\n\n" +
                string.Join("\n", missing);
            return false;
        }

        static void ValidateFinancialModelSamples(ICollection<string> validationErrors)
        {
            var risks = new[] { 0f, 0.5f, 1f };
            var horizons = new[] { 1, 5, 10 };
            foreach (var risk in risks)
            {
                foreach (var horizon in horizons)
                {
                    var projection = FinancialProjectionModel.Calculate(risk, horizon, false);
                    var allocationTotal =
                        projection.equityAllocation +
                        projection.bondAllocation +
                        projection.cashAllocation;
                    if (!Mathf.Approximately(allocationTotal, 1f))
                    {
                        validationErrors.Add(
                            $"Financial model allocation does not total 100% at risk {risk}.");
                    }

                    if (!IsFinitePositive(projection.baselineTerminalValue) ||
                        !IsFinitePositive(projection.stressTerminalValue) ||
                        projection.baselineForecastValues == null ||
                        projection.stressForecastValues == null ||
                        projection.baselineForecastValues.Length !=
                        FinancialProjectionModel.ForecastPointCount ||
                        projection.stressForecastValues.Length !=
                        FinancialProjectionModel.ForecastPointCount)
                    {
                        validationErrors.Add(
                            $"Financial model produced invalid output at risk {risk}, " +
                            $"horizon {horizon}.");
                        continue;
                    }

                    var stressMinimum = projection.stressForecastValues[0];
                    foreach (var value in projection.stressForecastValues)
                    {
                        if (!IsFinitePositive(value))
                        {
                            validationErrors.Add(
                                $"Financial model produced a non-finite stress point at " +
                                $"risk {risk}, horizon {horizon}.");
                            break;
                        }

                        stressMinimum = Mathf.Min(stressMinimum, value);
                    }

                    if (stressMinimum >= FinancialProjectionModel.StartingPortfolioValue ||
                        projection.stressTerminalValue <= stressMinimum ||
                        projection.stressTerminalValue >= projection.baselineTerminalValue)
                    {
                        validationErrors.Add(
                            $"Financial model stress path lacks the expected shock/recovery " +
                            $"relationship at risk {risk}, horizon {horizon}.");
                    }
                }
            }
        }

        static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }

        static void RequireAsset<T>(string path, ICollection<string> missing) where T : UnityEngine.Object
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
                missing.Add(path);
        }

        static void RequireRoomStructure(ICollection<string> missing)
        {
            var room = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath);
            if (room == null)
                return;

            var requiredChildren = new[]
            {
                "Room_Modern_Floor",
                "Room_Modern_Walls",
                "Room_Colliders",
                "Room_Modern_Door",
                "Room_Modern_Window",
                "Room_Modern_Window_Glass",
            };

            foreach (var childName in requiredChildren)
            {
                if (room.transform.Find(childName) == null)
                    missing.Add(RoomPath + " (missing child \"" + childName + "\")");
            }

            var colliderShell = room.transform.Find("Room_Colliders");
            if (colliderShell != null && colliderShell.GetComponents<BoxCollider>().Length == 0)
                missing.Add(RoomPath + " (Room_Colliders has no BoxCollider components)");
        }

        static void RequireControlVisual(
            string path,
            string interactiveChildName,
            ICollection<string> missing)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                missing.Add(path);
                return;
            }

            var interactiveTransform = string.IsNullOrEmpty(interactiveChildName)
                ? prefab.transform
                : prefab.transform.Find(interactiveChildName);

            if (interactiveTransform == null)
            {
                missing.Add(path + " (missing child \"" + interactiveChildName + "\")");
                return;
            }

            if (interactiveTransform.GetComponent<Collider>() == null)
                missing.Add(path + " (\"" + interactiveTransform.name + "\" has no Collider)");
        }

        static void RequireScriptType<T>(string path, ICollection<string> missing)
            where T : Component
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script == null || script.GetClass() != typeof(T))
                missing.Add(path + " (expected MonoBehaviour " + typeof(T).Name + ")");
        }

        static void EnsureSceneDirectory()
        {
            var directory = Path.GetDirectoryName(ScenePath);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("The demo scene path has no directory.");

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        static GameObject BuildEnvironment(Transform parent)
        {
            var environment = new GameObject("Environment").transform;
            environment.SetParent(parent, false);

            var room = InstantiatePrefab(
                RoomPath,
                environment,
                Vector3.zero,
                Quaternion.identity,
                "Room_Modern Style Source");
            BuildExpandedRoom(room, environment);

            InstantiatePrefab(
                TablePath,
                environment,
                new Vector3(0f, FloorY, ConsoleZ - 0.18f),
                Quaternion.Euler(0f, 90f, 0f),
                "Analysis Table");
            return room;
        }

        static void DisableVerifiedRoomStyleSource(
            Transform experienceLayout,
            GameObject roomStyleSource)
        {
            if (experienceLayout == null || roomStyleSource == null)
                throw new InvalidOperationException(
                    "The generated layout and Room_Modern style source are required.");

            var requiredReplacements = new[]
            {
                RequireActiveRenderer(
                    experienceLayout,
                    "Environment/Expanded Playable Room (19m x 13m)/Expanded Floor Visual"),
                RequireActiveRenderer(experienceLayout, "Environment/Analysis Table"),
                RequireActiveRenderer(
                    experienceLayout,
                    "Portfolio Stress Lab/Dashboard Backing Axis Correction (+Z to -Z)/" +
                    "Dashboard Display"),
            };

            foreach (var replacement in requiredReplacements)
            {
                if (replacement == roomStyleSource.transform ||
                    replacement.IsChildOf(roomStyleSource.transform))
                {
                    throw new InvalidOperationException(
                        replacement.name +
                        " is still owned by Room_Modern and cannot serve as a generated replacement.");
                }
            }

            RequireActiveLight(experienceLayout, "Lighting/Key Light");
            RequireActiveLight(experienceLayout, "Lighting/Dashboard Fill");
            RequireActiveLight(experienceLayout, "Lighting/Room Fill");

            roomStyleSource.SetActive(false);
            if (roomStyleSource.activeSelf)
                throw new InvalidOperationException(
                    "Room_Modern Style Source could not be disabled safely.");
        }

        static Transform RequireActiveRenderer(Transform root, string relativePath)
        {
            var target = root.Find(relativePath);
            var renderer = target != null ? target.GetComponentInChildren<Renderer>(true) : null;
            if (target == null ||
                !target.gameObject.activeInHierarchy ||
                renderer == null ||
                !renderer.enabled)
            {
                throw new InvalidOperationException(
                    "Required generated visible object is unavailable: " + relativePath);
            }

            return target;
        }

        static Light RequireActiveLight(Transform root, string relativePath)
        {
            var target = root.Find(relativePath);
            var light = target != null ? target.GetComponent<Light>() : null;
            if (target == null ||
                !target.gameObject.activeInHierarchy ||
                light == null ||
                !light.enabled)
            {
                throw new InvalidOperationException(
                    "Required generated light is unavailable: " + relativePath);
            }

            return light;
        }

        static void BuildExpandedRoom(GameObject room, Transform environment)
        {
            var sourceFloor = RequireDirectChild(room.transform, "Room_Modern_Floor");
            var sourceWalls = RequireDirectChild(room.transform, "Room_Modern_Walls");
            var sourceColliders = RequireDirectChild(room.transform, "Room_Colliders");
            var sourceDoor = RequireDirectChild(room.transform, "Room_Modern_Door");
            var sourceWindow = RequireDirectChild(room.transform, "Room_Modern_Window");
            var sourceWindowGlass = RequireDirectChild(room.transform, "Room_Modern_Window_Glass");

            var floorRenderer = sourceFloor.GetComponent<Renderer>();
            var floorMaterial = floorRenderer != null ? floorRenderer.sharedMaterial : null;
            var wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath);
            if (wallMaterial == null)
                throw new InvalidOperationException(
                    "The verified wall material could not be loaded: " + WallMaterialPath);

            // Extract only the floor material. These source visuals are not stretched across
            // the expanded room, and the whole source root is disabled after all independent
            // floor, table, dashboard, and lighting replacements have been verified.
            sourceFloor.gameObject.SetActive(false);
            sourceWalls.gameObject.SetActive(false);
            sourceColliders.gameObject.SetActive(false);
            sourceDoor.gameObject.SetActive(false);
            sourceWindow.gameObject.SetActive(false);
            sourceWindowGlass.gameObject.SetActive(false);

            var architecture = new GameObject("Expanded Playable Room (19m x 13m)").transform;
            architecture.SetParent(environment, false);

            CreateArchitectureBlock(
                "Expanded Floor Visual",
                architecture,
                new Vector3(0f, FloorY - 0.03f, 0f),
                new Vector3(PlayableWidth, 0.06f, PlayableDepth),
                floorMaterial,
                false);

            var safetyFloor = CreateArchitectureBlock(
                "Safety Floor Collider (Invisible)",
                architecture,
                new Vector3(0f, FloorY - 0.10f, 0f),
                new Vector3(PlayableWidth, 0.20f, PlayableDepth),
                null,
                true);
            safetyFloor.layer = 0;
            safetyFloor.GetComponent<Renderer>().enabled = false;

            var safetyCollider = safetyFloor.GetComponent<BoxCollider>();
            var teleportArea = safetyFloor.AddComponent<TeleportationArea>();
            teleportArea.colliders.Clear();
            teleportArea.colliders.Add(safetyCollider);

            const float wallThickness = 0.20f;
            var wallY = FloorY + (PerimeterHeight * 0.5f);
            CreatePerimeterWall(
                "Perimeter Wall - Left",
                architecture,
                new Vector3(-(PlayableWidth * 0.5f), wallY, 0f),
                new Vector3(wallThickness, PerimeterHeight, PlayableDepth),
                wallMaterial);
            CreatePerimeterWall(
                "Perimeter Wall - Right",
                architecture,
                new Vector3(PlayableWidth * 0.5f, wallY, 0f),
                new Vector3(wallThickness, PerimeterHeight, PlayableDepth),
                wallMaterial);
            CreatePerimeterWall(
                "Perimeter Wall - Front",
                architecture,
                new Vector3(0f, wallY, -(PlayableDepth * 0.5f)),
                new Vector3(PlayableWidth, PerimeterHeight, wallThickness),
                wallMaterial);
            CreatePerimeterWall(
                "Perimeter Wall - Dashboard",
                architecture,
                new Vector3(0f, wallY, PlayableDepth * 0.5f),
                new Vector3(PlayableWidth, PerimeterHeight, wallThickness),
                wallMaterial);

            ValidateGeneratedPerimeterWalls(architecture, wallMaterial);
        }

        static Transform RequireDirectChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child == null)
                throw new InvalidOperationException(
                    RoomPath + " is missing the verified child \"" + childName + "\".");

            return child;
        }

        static GameObject CreateArchitectureBlock(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool colliderEnabled)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localRotation = Quaternion.identity;
            block.transform.localScale = scale;
            block.layer = 0;

            var renderer = block.GetComponent<Renderer>();
            if (material != null)
                renderer.sharedMaterial = material;

            block.GetComponent<BoxCollider>().enabled = colliderEnabled;
            return block;
        }

        static GameObject CreatePerimeterWall(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material wallMaterial)
        {
            var wall = CreateArchitectureBlock(
                name,
                parent,
                position,
                scale,
                wallMaterial,
                true);
            var renderer = wall.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return wall;
        }

        static void ValidateGeneratedPerimeterWalls(Transform architecture, Material wallMaterial)
        {
            var wallCount = 0;
            foreach (Transform child in architecture)
            {
                if (!child.name.StartsWith("Perimeter Wall - ", StringComparison.Ordinal))
                    continue;

                wallCount++;
                var renderer = child.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled)
                    throw new InvalidOperationException(
                        child.name + " must have one enabled generated Renderer.");

                if (renderer.sharedMaterial != wallMaterial)
                    throw new InvalidOperationException(
                        child.name + " does not use the project-owned stable wall material.");

                if (child.GetComponent<BoxCollider>() == null)
                    throw new InvalidOperationException(
                        child.name + " must retain its generated BoxCollider.");
            }

            if (wallCount != 4)
                throw new InvalidOperationException(
                    "Expected exactly four generated perimeter walls, but found " + wallCount + ".");
        }

        static void BuildLighting(Transform parent)
        {
            var lighting = new GameObject("Lighting").transform;
            lighting.SetParent(parent, false);

            var keyLightObject = new GameObject("Key Light");
            keyLightObject.transform.SetParent(lighting, false);
            keyLightObject.transform.rotation = Quaternion.LookRotation(
                (ExperienceForward + (Vector3.down * 1.20f) + (Vector3.right * 0.35f)).normalized,
                WorldUp);
            var keyLight = keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.1f;
            keyLight.color = new Color(0.86f, 0.92f, 1f);

            var dashboardLightObject = new GameObject("Dashboard Fill");
            dashboardLightObject.transform.SetParent(lighting, false);
            dashboardLightObject.transform.position = new Vector3(0f, 2.8f, 4.15f);
            var dashboardLight = dashboardLightObject.AddComponent<Light>();
            dashboardLight.type = LightType.Point;
            dashboardLight.range = 10f;
            dashboardLight.intensity = 4f;
            dashboardLight.color = new Color(0.20f, 0.55f, 0.80f);

            var roomFillObject = new GameObject("Room Fill");
            roomFillObject.transform.SetParent(lighting, false);
            roomFillObject.transform.position = new Vector3(0f, 2.8f, -1.5f);
            var roomFill = roomFillObject.AddComponent<Light>();
            roomFill.type = LightType.Point;
            roomFill.range = 12f;
            roomFill.intensity = 2.5f;
            roomFill.color = new Color(0.65f, 0.76f, 1f);
        }

        static void BuildXrFoundation(Transform parent)
        {
            var xrRoot = new GameObject("XR Foundation").transform;
            xrRoot.SetParent(parent, false);

            var spawnPoint = new GameObject("Player Spawn Point").transform;
            spawnPoint.SetParent(xrRoot, false);
            spawnPoint.SetPositionAndRotation(
                new Vector3(0f, FloorY, SpawnZ),
                Quaternion.LookRotation(
                    new Vector3(0f, FloorY, DashboardZ) -
                    new Vector3(0f, FloorY, SpawnZ),
                    WorldUp));
            ValidateAxis(
                "Player Spawn Point forward",
                spawnPoint.forward,
                ExperienceForward);

            var rig = InstantiatePrefab(
                XrOriginPath,
                xrRoot,
                spawnPoint.position,
                spawnPoint.rotation,
                "XR Origin (Financial Data Room)");

            ConfigureLocomotion(rig);
            ConfigurePlayerSpawn(rig, spawnPoint);

            if (rig.GetComponentInChildren<XRInteractionManager>(true) == null)
            {
                var managerObject = new GameObject("XR Interaction Manager");
                managerObject.transform.SetParent(xrRoot, false);
                managerObject.AddComponent<XRInteractionManager>();
            }

            if (rig.GetComponentInChildren<InputActionManager>(true) == null)
            {
                var inputObject = new GameObject("Input Action Manager");
                inputObject.transform.SetParent(xrRoot, false);
                var inputManager = inputObject.AddComponent<InputActionManager>();
                inputManager.actionAssets = new List<InputActionAsset>
                {
                    AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath),
                };
            }

            var simulator = InstantiatePrefab(
                SimulatorPath,
                xrRoot,
                Vector3.zero,
                Quaternion.identity,
                "XR Interaction Simulator (EditorOnly)");
            simulator.tag = "EditorOnly";
        }

        static void ConfigurePlayerSpawn(GameObject rig, Transform spawnPoint)
        {
            var characterController = rig.GetComponent<CharacterController>();
            var xrCamera = rig.GetComponentInChildren<Camera>(true);
            if (characterController == null || xrCamera == null)
                throw new InvalidOperationException(
                    "The XR Origin must contain both a CharacterController and an HMD Camera.");

            const float desiredCameraHeight = 1.65f;
            const float minimumCameraHeight = 1.40f;
            const float controllerHeight = 1.80f;
            const float floorClearance = 0.05f;

            characterController.height = controllerHeight;
            characterController.radius = Mathf.Max(characterController.radius, 0.22f);
            characterController.center = new Vector3(
                characterController.center.x,
                floorClearance + (controllerHeight * 0.5f),
                characterController.center.z);
            characterController.stepOffset = Mathf.Min(0.30f, controllerHeight * 0.25f);

            var currentCameraHeight = rig.transform.InverseTransformPoint(xrCamera.transform.position).y;
            var cameraOffset = xrCamera.transform.parent;
            if (cameraOffset == null)
                throw new InvalidOperationException("The XR Camera has no Camera Offset parent.");

            cameraOffset.localPosition +=
                Vector3.up * Mathf.Max(0f, desiredCameraHeight - currentCameraHeight);

            var controllerBottomOffset =
                characterController.center.y - (characterController.height * 0.5f);
            var rootPosition = spawnPoint.position;
            rootPosition.y = FloorY + floorClearance - controllerBottomOffset;
            rig.transform.SetPositionAndRotation(rootPosition, spawnPoint.rotation);

            var finalCameraHeight = xrCamera.transform.position.y - FloorY;
            if (finalCameraHeight < minimumCameraHeight)
                throw new InvalidOperationException(
                    "Calculated HMD spawn height is unsafe: " +
                    finalCameraHeight.ToString("0.00") + " m above the floor.");

            if (Physics.GetIgnoreLayerCollision(rig.layer, 0))
                throw new InvalidOperationException(
                    "The XR Origin layer does not collide with the generated floor layer.");
        }

        static void ConfigureLocomotion(GameObject rig)
        {
            var moveProvider = rig.GetComponentInChildren<DynamicMoveProvider>(true);
            if (moveProvider == null)
                throw new InvalidOperationException("The XR Origin prefab has no DynamicMoveProvider.");

            moveProvider.enabled = true;
            moveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HeadRelative;
            moveProvider.rightHandMovementDirection = DynamicMoveProvider.MovementDirection.HeadRelative;
            moveProvider.moveSpeed = 1.75f;
            moveProvider.rightHandMoveInput.inputActionReference = null;

            var snapTurn = rig.GetComponentInChildren<SnapTurnProvider>(true);
            if (snapTurn == null)
                throw new InvalidOperationException("The XR Origin prefab has no SnapTurnProvider.");

            snapTurn.enabled = true;
            snapTurn.turnAmount = 45f;
            snapTurn.enableTurnLeftRight = true;
            snapTurn.enableTurnAround = false;
            snapTurn.leftHandTurnInput.inputActionReference = null;

            foreach (var continuousTurn in rig.GetComponentsInChildren<ContinuousTurnProvider>(true))
                continuousTurn.enabled = false;

            // Teleportation providers and interactors are intentionally not disabled or altered.
        }

        static FinancialDashboardView BuildDashboard(Transform parent)
        {
            var dashboardRoot = new GameObject("Portfolio Stress Lab").transform;
            dashboardRoot.SetParent(parent, false);
            dashboardRoot.localPosition = new Vector3(0f, 0f, DashboardZ);
            dashboardRoot.localRotation = Quaternion.identity;

            var readableContent = CreateLayoutAnchor(
                "Dashboard Readable Content (-Z Front)",
                dashboardRoot,
                Vector3.zero,
                MapAuthoredFrameToWorld(
                    CanvasReadableLocalFront,
                    Vector3.up,
                    AudienceFacing,
                    WorldUp));

            var backingCorrection = CreateLayoutAnchor(
                "Dashboard Backing Axis Correction (+Z to -Z)",
                dashboardRoot,
                new Vector3(0f, 1.65f, 0.18f),
                MapAuthoredFrameToWorld(
                    WallVisualAuthoredLocalFront,
                    Vector3.up,
                    AudienceFacing,
                    WorldUp));
            var dashboardDisplay = InstantiatePrefabLocal(
                TelevisionPath,
                backingCorrection,
                Vector3.zero,
                Quaternion.identity,
                "Dashboard Display");
            ValidateTransformedAxis(
                "Dashboard backing front",
                dashboardDisplay.transform,
                WallVisualAuthoredLocalFront,
                AudienceFacing);

            var canvasObject = new GameObject(
                "Financial Dashboard",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(readableContent, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1600f, 900f);
            canvasRect.localPosition = new Vector3(0f, 1.90f, 0f);
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.0015f;
            ValidateReadableFrame("Financial Dashboard", canvasRect);

            var background = CreatePanel(
                "Dashboard Background",
                canvasRect,
                Vector2.zero,
                new Vector2(1600f, 900f),
                DashboardBackground);

            CreateText(
                "Title",
                background,
                "PORTFOLIO STRESS LAB",
                new Vector2(50f, -28f),
                new Vector2(930f, 70f),
                50f,
                AccentText,
                TextAlignmentOptions.Left);

            var scenarioChip = CreatePanel(
                "Active Scenario Chip",
                background,
                new Vector2(1180f, -30f),
                new Vector2(370f, 65f),
                new Color(0.06f, 0.30f, 0.24f, 0.98f));
            var scenario = CreateText(
                "Active Scenario",
                scenarioChip,
                "BASE CASE",
                new Vector2(20f, -10f),
                new Vector2(330f, 45f),
                30f,
                PrimaryText,
                TextAlignmentOptions.Center);

            var riskHorizon = CreateText(
                "Risk Horizon Summary",
                background,
                "BALANCED  |  Risk 50%  |  Horizon 5Y",
                new Vector2(50f, -100f),
                new Vector2(1500f, 42f),
                27f,
                MutedText,
                TextAlignmentOptions.Left);

            var portfolioValue = CreateKpiCard(
                "Portfolio Value Card",
                background,
                new Vector2(50f, -155f),
                "PORTFOLIO VALUE",
                "$1,000,000",
                AccentText);
            var expectedReturn = CreateKpiCard(
                "Expected Return Card",
                background,
                new Vector2(425f, -155f),
                "EXPECTED RETURN",
                "0.0%",
                PrimaryText);
            var volatility = CreateKpiCard(
                "Volatility Card",
                background,
                new Vector2(800f, -155f),
                "VOLATILITY",
                "0.0%",
                PrimaryText);
            var drawdown = CreateKpiCard(
                "Maximum Drawdown Card",
                background,
                new Vector2(1175f, -155f),
                "MAX DRAWDOWN",
                "0.0%",
                PrimaryText);

            var allocationPanel = CreatePanel(
                "Allocation Panel",
                background,
                new Vector2(50f, -325f),
                new Vector2(430f, 250f),
                new Color(0.025f, 0.065f, 0.10f, 0.98f));
            CreateText(
                "Allocation Heading",
                allocationPanel,
                "ALLOCATION",
                new Vector2(25f, -20f),
                new Vector2(380f, 40f),
                25f,
                MutedText,
                TextAlignmentOptions.Left);
            var equity = CreateText(
                "Equity Allocation",
                allocationPanel,
                "Equity  0%",
                new Vector2(25f, -70f),
                new Vector2(380f, 45f),
                34f,
                PrimaryText,
                TextAlignmentOptions.Left);
            var bonds = CreateText(
                "Bond Allocation",
                allocationPanel,
                "Bonds  0%",
                new Vector2(25f, -130f),
                new Vector2(380f, 45f),
                34f,
                PrimaryText,
                TextAlignmentOptions.Left);
            var cash = CreateText(
                "Cash Allocation",
                allocationPanel,
                "Cash  0%",
                new Vector2(25f, -190f),
                new Vector2(380f, 45f),
                34f,
                PrimaryText,
                TextAlignmentOptions.Left);

            var forecastPanel = CreatePanel(
                "Forecast Comparison Panel",
                background,
                new Vector2(500f, -325f),
                new Vector2(1050f, 250f),
                new Color(0.025f, 0.055f, 0.085f, 0.98f));
            CreateText(
                "Forecast Heading",
                forecastPanel,
                "BASELINE VS MARKET STRESS",
                new Vector2(25f, -18f),
                new Vector2(500f, 40f),
                25f,
                MutedText,
                TextAlignmentOptions.Left);
            var baselineLegend = CreateText(
                "Baseline Legend",
                forecastPanel,
                "BASE  $1,000,000",
                new Vector2(560f, -18f),
                new Vector2(220f, 40f),
                24f,
                new Color(0.20f, 0.90f, 0.65f),
                TextAlignmentOptions.Left);
            var stressLegend = CreateText(
                "Stress Legend",
                forecastPanel,
                "STRESS  $1,000,000",
                new Vector2(790f, -18f),
                new Vector2(235f, 40f),
                24f,
                new Color(1f, 0.35f, 0.30f),
                TextAlignmentOptions.Left);

            var explanationPanel = CreatePanel(
                "Scenario Explanation Panel",
                background,
                new Vector2(50f, -595f),
                new Vector2(1500f, 100f),
                new Color(0.035f, 0.075f, 0.105f, 0.98f));
            var explanation = CreateText(
                "Scenario Explanation",
                explanationPanel,
                string.Empty,
                new Vector2(25f, -18f),
                new Vector2(1450f, 65f),
                27f,
                PrimaryText,
                TextAlignmentOptions.TopLeft);

            var instructions = CreateText(
                "Persistent Instructions",
                background,
                string.Empty,
                new Vector2(50f, -710f),
                new Vector2(500f, 120f),
                25f,
                PrimaryText,
                TextAlignmentOptions.TopLeft);
            var status = CreateText(
                "Current Status",
                background,
                "CURRENT: Move with LEFT stick | Snap turn with RIGHT stick",
                new Vector2(580f, -710f),
                new Vector2(970f, 90f),
                27f,
                AccentText,
                TextAlignmentOptions.TopRight);

            var completionPanel = CreatePanel(
                "Analysis Complete Panel",
                background,
                new Vector2(580f, -700f),
                new Vector2(970f, 125f),
                new Color(0.04f, 0.20f, 0.18f, 0.98f));
            var completionText = CreateText(
                "Analysis Complete Summary",
                completionPanel,
                string.Empty,
                new Vector2(25f, -14f),
                new Vector2(920f, 98f),
                26f,
                PrimaryText,
                TextAlignmentOptions.TopLeft);
            completionPanel.gameObject.SetActive(false);

            CreateText(
                "Illustrative Footer",
                background,
                "ILLUSTRATIVE ONLY — NOT FINANCIAL ADVICE",
                new Vector2(50f, -852f),
                new Vector2(1500f, 32f),
                20f,
                MutedText,
                TextAlignmentOptions.Center);

            var baselineLine = CreateForecastLine(
                "Baseline Forecast Line",
                readableContent,
                new Vector3(-0.36f, 1.66f, -0.025f));
            var stressLine = CreateForecastLine(
                "Stress Forecast Line",
                readableContent,
                new Vector3(-0.36f, 1.66f, -0.03f));

            var view = dashboardRoot.gameObject.AddComponent<FinancialDashboardView>();
            view.Configure(
                portfolioValue,
                expectedReturn,
                volatility,
                drawdown,
                equity,
                bonds,
                cash,
                scenario,
                scenarioChip.GetComponent<Image>(),
                riskHorizon,
                explanation,
                baselineLegend,
                stressLegend,
                baselineLine,
                stressLine,
                completionPanel.gameObject,
                completionText);

            dashboardRoot.gameObject.AddComponent<FinancialRoomController>().Configure(
                view,
                instructions,
                status);

            return view;
        }

        static TextMeshProUGUI CreateKpiCard(
            string name,
            RectTransform parent,
            Vector2 anchoredPosition,
            string label,
            string initialValue,
            Color valueColor)
        {
            var card = CreatePanel(
                name,
                parent,
                anchoredPosition,
                new Vector2(340f, 145f),
                new Color(0.028f, 0.075f, 0.115f, 0.98f));
            CreateText(
                name + " Label",
                card,
                label,
                new Vector2(20f, -17f),
                new Vector2(300f, 32f),
                22f,
                MutedText,
                TextAlignmentOptions.Left);
            return CreateText(
                name + " Value",
                card,
                initialValue,
                new Vector2(20f, -57f),
                new Vector2(300f, 65f),
                40f,
                valueColor,
                TextAlignmentOptions.Left);
        }

        static LineRenderer CreateForecastLine(
            string name,
            Transform parent,
            Vector3 localPosition)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            lineObject.transform.localPosition = localPosition;
            lineObject.transform.localRotation = Quaternion.identity;

            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            line.alignment = LineAlignment.View;
            line.widthMultiplier = 0.016f;
            line.numCornerVertices = 3;
            line.numCapVertices = 3;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ScreenMaterialPath);
            ValidateReadableFrame(name, lineObject.transform);
            return line;
        }

        static FinancialRoomController BuildControlsAndController(
            Transform parent,
            FinancialDashboardView dashboard)
        {
            var controller = dashboard.GetComponent<FinancialRoomController>();
            if (controller == null)
                throw new InvalidOperationException("The dashboard controller could not be created.");

            var controlsRoot = new GameObject("Physical Controls").transform;
            controlsRoot.SetParent(parent, false);
            controlsRoot.localPosition = new Vector3(0f, 0f, ConsoleZ);
            controlsRoot.localRotation = Quaternion.identity;

            var tabletopRotation = MapAuthoredFrameToWorld(
                TabletopVisualAuthoredLocalFront,
                Vector3.up,
                ExperienceForward,
                WorldUp);
            var wallVisualRotation = MapAuthoredFrameToWorld(
                WallVisualAuthoredLocalFront,
                Vector3.up,
                AudienceFacing,
                WorldUp);

            var riskAnchor = CreateLayoutAnchor(
                "Risk Control",
                controlsRoot,
                new Vector3(-0.70f, 0.92f, 0f),
                Quaternion.identity);
            var knob = InstantiatePrefabLocal(
                KnobVisualPrefabPath,
                riskAnchor,
                Vector3.zero,
                tabletopRotation,
                "Risk Knob Visual");
            ValidateTransformedAxis(
                "Risk knob top",
                knob.transform,
                Vector3.up,
                WorldUp);
            var knobControl = ConfigureKnob(knob);

            var horizonAnchor = CreateLayoutAnchor(
                "Horizon Control",
                controlsRoot,
                new Vector3(0f, 0.92f, 0f),
                Quaternion.identity);
            var horizonCorrection = CreateLayoutAnchor(
                "Visual Axis Correction (+Z to -Z)",
                horizonAnchor,
                Vector3.zero,
                wallVisualRotation);
            var slider = InstantiatePrefabLocal(
                SliderVisualPrefabPath,
                horizonCorrection,
                Vector3.zero,
                Quaternion.identity,
                "Investment Horizon Slider Visual");
            ValidateTransformedAxis(
                "Investment horizon slider front",
                slider.transform,
                WallVisualAuthoredLocalFront,
                AudienceFacing);
            var sliderControl = ConfigureSlider(slider);

            var scenarioAnchor = CreateLayoutAnchor(
                "Scenario Control",
                controlsRoot,
                new Vector3(0.70f, 0.92f, 0f),
                Quaternion.identity);
            var scenarioCorrection = CreateLayoutAnchor(
                "Visual Axis Correction (+Z to -Z)",
                scenarioAnchor,
                Vector3.zero,
                wallVisualRotation);
            var lever = InstantiatePrefabLocal(
                LeverVisualPrefabPath,
                scenarioCorrection,
                Vector3.zero,
                Quaternion.identity,
                "Base Stress Lever Visual");
            ValidateTransformedAxis(
                "Base/Stress lever front",
                lever.transform,
                WallVisualAuthoredLocalFront,
                AudienceFacing);
            var leverControl = ConfigureLever(lever);

            var runAnchor = CreateLayoutAnchor(
                "Run Analysis Control",
                controlsRoot,
                new Vector3(-0.25f, 0.92f, -0.53f),
                Quaternion.identity);
            var runButtonObject = InstantiatePrefabLocal(
                RunButtonVisualPrefabPath,
                runAnchor,
                Vector3.zero,
                tabletopRotation,
                "Run Analysis Button Visual");
            ValidateTransformedAxis(
                "Run Analysis button top",
                runButtonObject.transform,
                Vector3.up,
                WorldUp);

            var resetAnchor = CreateLayoutAnchor(
                "Reset Control",
                controlsRoot,
                new Vector3(0.25f, 0.92f, -0.53f),
                Quaternion.identity);
            var resetButtonObject = InstantiatePrefabLocal(
                ResetButtonVisualPrefabPath,
                resetAnchor,
                Vector3.zero,
                tabletopRotation,
                "Reset Button Visual");
            ValidateTransformedAxis(
                "Reset button top",
                resetButtonObject.transform,
                Vector3.up,
                WorldUp);

            var runButton = ConfigureButton(runButtonObject);
            var resetButton = ConfigureButton(resetButtonObject);

            UnityEventTools.AddPersistentListener(knobControl.ValueChanged, controller.SetRisk);
            UnityEventTools.AddPersistentListener(sliderControl.ValueChanged, controller.SetHorizon);
            UnityEventTools.AddPersistentListener(leverControl.ValueChanged, controller.SetScenario);
            UnityEventTools.AddPersistentListener(runButton.Pressed, controller.RunAnalysis);
            UnityEventTools.AddPersistentListener(resetButton.Pressed, controller.ResetDemoState);
            controller.ConfigureControls(
                knobControl,
                sliderControl,
                leverControl,
                runButton,
                resetButton);

            EditorUtility.SetDirty(knobControl);
            EditorUtility.SetDirty(sliderControl);
            EditorUtility.SetDirty(leverControl);
            EditorUtility.SetDirty(runButton);
            EditorUtility.SetDirty(resetButton);
            EditorUtility.SetDirty(controller);

            BuildControlLabels(controlsRoot);
            return controller;
        }

        static FinancialPhysicalControl ConfigureKnob(GameObject visualInstance)
        {
            var sourceCollider = visualInstance.GetComponent<Collider>();
            if (sourceCollider == null)
                throw new InvalidOperationException("The verified knob visual has no root Collider.");

            // The authored collider is only about 3.5 cm wide after prefab scaling, which is
            // unnecessarily difficult to acquire with a Quest ray or direct interactor.
            // Disable it only on this generated instance and use a larger invisible child volume.
            sourceCollider.enabled = false;
            var interactionVolume = new GameObject("Risk Knob Interaction Volume");
            interactionVolume.layer = visualInstance.layer;
            interactionVolume.transform.SetParent(visualInstance.transform, false);
            interactionVolume.transform.localPosition = Vector3.zero;
            interactionVolume.transform.localRotation = Quaternion.identity;
            interactionVolume.transform.localScale = Vector3.one;
            var interactionCollider = interactionVolume.AddComponent<BoxCollider>();
            interactionCollider.center = new Vector3(0f, 0.03f, 0f);
            interactionCollider.size = new Vector3(0.12f, 0.08f, 0.12f);

            var control = visualInstance.AddComponent<FinancialPhysicalControl>();
            control.Configure(
                FinancialPhysicalControl.ControlMode.Knob,
                visualInstance.transform,
                interactionCollider,
                visualInstance.GetComponentsInChildren<Renderer>(true),
                0.5f,
                knobSensitivity: 1.35f);
            ConfigurePoke(control, interactionCollider, PokeAxis.NegativeY);
            return control;
        }

        static FinancialPhysicalControl ConfigureSlider(GameObject visualInstance)
        {
            var handle = visualInstance.transform.Find("Dimmer_Handle");
            if (handle == null)
                throw new InvalidOperationException(
                    "The verified slider visual has no \"Dimmer_Handle\" child.");

            var handleCollider = handle.GetComponent<Collider>();
            if (handleCollider == null)
                throw new InvalidOperationException("The slider handle has no Collider.");

            var start = CreateSliderMarker(
                "Horizon Start (1 Year)",
                visualInstance.transform,
                handle.localPosition,
                -0.060f);
            var end = CreateSliderMarker(
                "Horizon End (10 Years)",
                visualInstance.transform,
                handle.localPosition,
                0.060f);

            var control = visualInstance.AddComponent<FinancialPhysicalControl>();
            control.Configure(
                FinancialPhysicalControl.ControlMode.Slider,
                handle,
                handleCollider,
                visualInstance.GetComponentsInChildren<Renderer>(true),
                4f / 9f,
                start,
                end);
            ConfigurePoke(control, handleCollider, PokeAxis.Z);
            return control;
        }

        static Transform CreateSliderMarker(
            string name,
            Transform parent,
            Vector3 handleLocalPosition,
            float localY)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.localPosition = new Vector3(
                handleLocalPosition.x,
                localY,
                handleLocalPosition.z);
            return marker;
        }

        static FinancialPhysicalControl ConfigureLever(GameObject visualInstance)
        {
            var handle = visualInstance.transform.Find("Lever_Switch");
            if (handle == null)
                throw new InvalidOperationException(
                    "The verified lever visual has no \"Lever_Switch\" child.");

            var handleCollider = handle.GetComponent<Collider>();
            if (handleCollider == null)
                throw new InvalidOperationException("The lever switch has no Collider.");

            var control = visualInstance.AddComponent<FinancialPhysicalControl>();
            control.Configure(
                FinancialPhysicalControl.ControlMode.Lever,
                handle,
                handleCollider,
                visualInstance.GetComponentsInChildren<Renderer>(true),
                0f);
            ConfigurePoke(control, handleCollider, PokeAxis.Z);
            return control;
        }

        static FinancialActionButton ConfigureButton(GameObject visualInstance)
        {
            var collider = visualInstance.GetComponent<Collider>();
            if (collider == null)
                throw new InvalidOperationException("The verified button visual has no root Collider.");

            var control = visualInstance.AddComponent<FinancialActionButton>();
            control.Configure(
                visualInstance.transform,
                collider,
                visualInstance.GetComponentsInChildren<Renderer>(true),
                Vector3.down,
                0.018f);
            ConfigurePoke(control, collider, PokeAxis.NegativeY);
            return control;
        }

        static void ConfigurePoke(
            UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable,
            Collider collider,
            PokeAxis pokeAxis)
        {
            var pokeFilter = interactable.gameObject.AddComponent<XRPokeFilter>();
            pokeFilter.pokeInteractable = interactable;
            pokeFilter.pokeCollider = collider;
            pokeFilter.pokeConfiguration.Value.pokeDirection = pokeAxis;
        }

        static void BuildControlLabels(Transform parent)
        {
            var readableContent = CreateLayoutAnchor(
                "Control Labels Readable Content (-Z Front)",
                parent,
                Vector3.zero,
                MapAuthoredFrameToWorld(
                    CanvasReadableLocalFront,
                    Vector3.up,
                    AudienceFacing,
                    WorldUp));

            var labelsObject = new GameObject(
                "Control Labels",
                typeof(RectTransform),
                typeof(Canvas));
            labelsObject.transform.SetParent(readableContent, false);

            var canvas = labelsObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = labelsObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1800f, 450f);
            rect.localPosition = new Vector3(0f, 1.28f, -0.13f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.001f;
            ValidateReadableFrame("Control Labels", rect);

            CreateText(
                "Control Names",
                rect,
                "RISK                         INVESTMENT HORIZON                         BASE / STRESS",
                new Vector2(50f, -40f),
                new Vector2(1700f, 90f),
                38f,
                PrimaryText,
                TextAlignmentOptions.Center);
            CreateText(
                "Button Names",
                rect,
                "RUN ANALYSIS                                      RESET",
                new Vector2(400f, -300f),
                new Vector2(1000f, 90f),
                36f,
                AccentText,
                TextAlignmentOptions.Center);
        }

        static Transform CreateLayoutAnchor(
            string name,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = localPosition;
            anchor.localRotation = localRotation;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        static Quaternion MapAuthoredFrameToWorld(
            Vector3 authoredLocalFront,
            Vector3 authoredLocalUp,
            Vector3 desiredWorldFront,
            Vector3 desiredWorldUp)
        {
            ValidateFrame("authored", authoredLocalFront, authoredLocalUp);
            ValidateFrame("desired world", desiredWorldFront, desiredWorldUp);

            var authoredFrame = Quaternion.LookRotation(
                authoredLocalFront.normalized,
                authoredLocalUp.normalized);
            var desiredFrame = Quaternion.LookRotation(
                desiredWorldFront.normalized,
                desiredWorldUp.normalized);
            return desiredFrame * Quaternion.Inverse(authoredFrame);
        }

        static void ValidateFrame(string label, Vector3 front, Vector3 up)
        {
            if (front.sqrMagnitude <= Mathf.Epsilon)
                throw new InvalidOperationException(label + " front axis cannot be zero.");

            if (up.sqrMagnitude <= Mathf.Epsilon)
                throw new InvalidOperationException(label + " up axis cannot be zero.");

            if (Mathf.Abs(Vector3.Dot(front.normalized, up.normalized)) > 0.001f)
                throw new InvalidOperationException(
                    label + " front and up axes must be perpendicular.");
        }

        static void ValidateReadableFrame(string label, Transform transform)
        {
            ValidateTransformedAxis(
                label + " readable front",
                transform,
                CanvasReadableLocalFront,
                AudienceFacing);
            ValidateTransformedAxis(
                label + " right",
                transform,
                Vector3.right,
                Vector3.right);
            ValidateTransformedAxis(
                label + " up",
                transform,
                Vector3.up,
                WorldUp);
        }

        static void ValidateTransformedAxis(
            string label,
            Transform transform,
            Vector3 localAxis,
            Vector3 expectedWorldAxis)
        {
            if (transform == null)
                throw new InvalidOperationException(label + " has no transform.");

            ValidateAxis(
                label,
                transform.TransformDirection(localAxis),
                expectedWorldAxis);
        }

        static void ValidateAxis(
            string label,
            Vector3 actualWorldAxis,
            Vector3 expectedWorldAxis)
        {
            if (actualWorldAxis.sqrMagnitude <= Mathf.Epsilon ||
                expectedWorldAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                throw new InvalidOperationException(label + " cannot validate a zero axis.");
            }

            var alignment = Vector3.Dot(
                actualWorldAxis.normalized,
                expectedWorldAxis.normalized);
            if (alignment < 0.999f)
            {
                throw new InvalidOperationException(
                    label + " is misaligned. Expected " +
                    expectedWorldAxis.normalized + ", but found " +
                    actualWorldAxis.normalized + ".");
            }
        }

        static GameObject InstantiatePrefab(
            string assetPath,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            string instanceName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate prefab: " + assetPath);

            instance.name = instanceName;
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        static GameObject InstantiatePrefabLocal(
            string assetPath,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            string instanceName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate prefab: " + assetPath);

            instance.name = instanceName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            return instance;
        }

        static RectTransform CreatePanel(
            string name,
            RectTransform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var panelObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var rect = panelObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = panelObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static TextMeshProUGUI CreateText(
            string name,
            RectTransform parent,
            string content,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            var rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }
    }
}
