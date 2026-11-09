using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace InfectionShot.Editor
{
    /// <summary>Builds the whole prototype (config, materials, scene) from code, so it can be regenerated with one click.</summary>
    public static class SceneBuilder
    {
        private const string Root = "Assets/InfectionShot";
        private const string ContentDir = Root + "/Content";
        private const string ScenePath = Root + "/Scenes/Main.unity";
        private const string ConfigPath = ContentDir + "/GameConfig.asset";

        // Route: lower-left to upper-right on the X/Z plane.
        private static readonly Vector3 RouteStart = new Vector3(-3.2f, 0f, -7.6f);
        private static readonly Vector3 DoorPos = new Vector3(3.2f, 0f, 7.6f);

        private struct Spawn
        {
            public Vector3 Position;
            public int Group;
            public bool Blocking;
            public float Height;
        }

        private sealed class Mats
        {
            public Material Ground, Lane, Player, Shot, White, Door, Line, BlastLine, Particle;
            public Material[] Green;
        }

        [MenuItem("Tools/Infection Shot/Rebuild Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(ContentDir);
            Directory.CreateDirectory(Root + "/Scenes");

            // Create the scene first: NewScene unloads unreferenced assets, so assets are created and loaded after it.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameConfig config = LoadOrCreateConfig();
            Mats mats = CreateMaterials();

            var path = new PathModel(RouteStart, DoorPos);
            List<Spawn> spawns = PlanObstacles(path, config);
            Audit(spawns, path, config);

            Camera cam = BuildCamera();
            BuildLight();
            BuildGround(mats);

            Transform start = new GameObject("RouteStart").transform;
            start.position = RouteStart;

            LaneView lane = BuildLane(mats, path);
            PlayerView player = BuildPlayer(mats, config, path);
            ShotView shot = BuildShot(mats);
            DoorView door = BuildDoor(mats, path, out Transform doorRoot);
            FxView fx = BuildFx(mats);
            FieldView field = BuildField(mats, spawns);
            BuildHud(out HudView hud);
            PointerInputView pointer = new GameObject("PointerInput").AddComponent<PointerInputView>();
            SetRef(pointer, "viewCamera", cam);

            var contextRoot = new GameObject("SceneContext");
            var sceneContext = contextRoot.AddComponent<SceneContext>();
            var installer = contextRoot.AddComponent<GameInstaller>();
            SetRef(installer, "config", config);
            SetRef(installer, "routeStart", start);
            SetRef(installer, "door", doorRoot);
            SetRef(installer, "fieldView", field);
            SetRef(installer, "playerView", player);
            SetRef(installer, "shotView", shot);
            SetRef(installer, "laneView", lane);
            SetRef(installer, "doorView", door);
            SetRef(installer, "fxView", fx);
            SetRef(installer, "hudView", hud);
            SetRef(installer, "pointerView", pointer);
            sceneContext.Installers = new MonoInstaller[] { installer };

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuild();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[InfectionShot] Scene rebuilt: " + ScenePath);
        }

        // ------------------------------------------------------------------ level

        private static List<Spawn> PlanObstacles(PathModel path, GameConfig config)
        {
            var list = new List<Spawn>();
            Vector3 f = path.Direction;
            Vector3 right = new Vector3(f.z, 0f, -f.x);

            // Four gates along the route: isolated, cluster of 3, isolated, cluster of 4.
            AddGate(list, path, right, 0, 3.8f, new Vector2[0]);
            // Lateral offsets stay below the smallest shot's reach (shot radius + obstacle radius), so even a tap can touch every member.
            AddGate(list, path, right, 1, 7.0f, new[] { new Vector2(-0.4f, 0.6f), new Vector2(0.4f, 0.6f) });
            AddGate(list, path, right, 2, 10.0f, new Vector2[0]);
            AddGate(list, path, right, 3, 12.6f, new[] { new Vector2(-0.4f, 0.6f), new Vector2(0.4f, 0.6f), new Vector2(0f, 1.2f) });

            // Scenery: a dense field around the lane. These can still be infected, but never block a shot.
            var rng = new System.Random(7);
            int gateCount = list.Count;
            for (float x = -7.2f; x <= 7.2f; x += 0.9f)
            {
                for (float z = -12.6f; z <= 13.5f; z += 0.9f)
                {
                    var p = new Vector3(x + (float)(rng.NextDouble() - 0.5) * 0.6f, 0f, z + (float)(rng.NextDouble() - 0.5) * 0.6f);
                    float lateral = Mathf.Abs(Vector3.Dot(p - path.Start, right));
                    if (lateral < 1.6f) continue;
                    if (Flat(p, path.Start) < 2.4f || Flat(p, path.End) < 2.6f) continue;

                    bool crowded = false;
                    for (int i = 0; i < gateCount && !crowded; i++) crowded = Flat(p, list[i].Position) < 0.8f;
                    if (crowded) continue;

                    list.Add(new Spawn { Position = p, Group = -1, Blocking = false, Height = 0.45f + (float)rng.NextDouble() * 0.3f });
                }
            }

            return list;
        }

        private static void AddGate(List<Spawn> list, PathModel path, Vector3 right, int group, float distance, Vector2[] offsets)
        {
            Vector3 key = path.PointAt(distance);
            list.Add(new Spawn { Position = key, Group = group, Blocking = true, Height = 0.55f });
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 p = key + right * offsets[i].x + path.Direction * offsets[i].y;
                list.Add(new Spawn { Position = p, Group = group, Blocking = true, Height = 0.55f });
            }
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Fails the build if the economical clearing plan breaks the 20% reserve required by the task.</summary>
        private static void Audit(List<Spawn> spawns, PathModel path, GameConfig config)
        {
            var models = new List<ObstacleModel>();
            for (int i = 0; i < spawns.Count; i++)
                models.Add(new ObstacleModel(i, spawns[i].Position, config.obstacleRadius, spawns[i].Group, spawns[i].Blocking));

            var field = new FieldModel(models);
            float planned = field.EstimateRemainingMass(path, config, config.SpendableMass);
            float allowed = config.SpendableMass * config.maxPlannedShare;
            float reserve = (config.SpendableMass - planned) / config.SpendableMass;
            Debug.Log("[InfectionShot] Planned clear cost " + planned.ToString("0.000") + " of spendable " + config.SpendableMass.ToString("0.000")
                + " (allowed " + allowed.ToString("0.000") + "), reserve " + (reserve * 100f).ToString("0.0") + "%");
            if (float.IsInfinity(planned) || planned > allowed)
                throw new InvalidOperationException("Level violates the mass reserve: planned " + planned + " > allowed " + allowed);
        }

        // ------------------------------------------------------------------ assets

        private static GameConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static Mats CreateMaterials()
        {
            Texture2D grid = CreateGridTexture();
            var m = new Mats
            {
                Ground = Standard("Ground", new Color(1f, 1f, 1f), 0.05f),
                Lane = Flat("Lane", new Color(0.94f, 0.33f, 0.5f)),
                Player = Standard("Player", new Color(1f, 0.6f, 0.1f), 0.5f),
                Shot = Standard("Shot", new Color(1f, 0.78f, 0.5f), 0.5f),
                White = Standard("White", new Color(1f, 1f, 1f), 0.4f),
                Door = Standard("Door", new Color(1f, 0.58f, 0.12f), 0.35f),
                Line = Sprite("RingLine", new Color(1f, 1f, 1f, 0.85f)),
                BlastLine = Sprite("BlastLine", new Color(1f, 0.82f, 0.3f, 1f)),
                Particle = Sprite("Particle", Color.white),
                Green = new[]
                {
                    Standard("Green1", new Color(0.27f, 0.5f, 0.2f), 0.25f),
                    Standard("Green2", new Color(0.32f, 0.58f, 0.23f), 0.25f),
                    Standard("Green3", new Color(0.22f, 0.43f, 0.18f), 0.25f)
                }
            };
            m.Ground.mainTexture = grid;
            m.Ground.mainTextureScale = new Vector2(40f, 40f);
            EditorUtility.SetDirty(m.Ground);
            return m;
        }

        private static Texture2D CreateGridTexture()
        {
            const int size = 64;
            string file = ContentDir + "/Grid.png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color back = new Color(0.62f, 0.66f, 0.55f);
            Color line = new Color(0.48f, 0.52f, 0.42f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, (x < 2 || y < 2) ? line : back);
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(file);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(file);
        }

        private static Material Standard(string name, Color color, float smoothness)
        {
            Material m = LoadOrCreate(name, "Standard");
            m.color = color;
            m.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Flat(string name, Color color)
        {
            Material m = LoadOrCreate(name, "Unlit/Color");
            m.color = color;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Sprite(string name, Color color)
        {
            Material m = LoadOrCreate(name, "Sprites/Default");
            m.color = color;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material LoadOrCreate(string name, string shaderName)
        {
            string path = ContentDir + "/" + name + ".mat";
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Shader not found: " + shaderName);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = shader;

            return m;
        }

        // ------------------------------------------------------------------ world

        private static Camera BuildCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.4f, 0.44f, 0.36f);
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 80f;
            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(0f, 31f, -9.5f);
            go.transform.rotation = Quaternion.Euler(72f, 0f, 0f);
            return cam;
        }

        private static void BuildLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            go.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.66f);
        }

        private static void BuildGround(Mats mats)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.GetComponent<MeshRenderer>().sharedMaterial = mats.Ground;
        }

        private static LaneView BuildLane(Mats mats, PathModel path)
        {
            GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lane.name = "Lane";
            Object.DestroyImmediate(lane.GetComponent<Collider>());
            var renderer = lane.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mats.Lane;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var view = lane.AddComponent<LaneView>();
            view.SetSegment(path.Start, path.End);
            view.SetHalfWidth(1.05f, true);
            return view;
        }

        private static PlayerView BuildPlayer(Mats mats, GameConfig config, PathModel path)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Player";
            Object.DestroyImmediate(ball.GetComponent<Collider>());
            ball.GetComponent<MeshRenderer>().sharedMaterial = mats.Player;
            float r = config.PlayerRadius(config.initialMass);
            ball.transform.localScale = Vector3.one * (r * 2f);
            ball.transform.position = new Vector3(path.Start.x, r, path.Start.z);
            return ball.AddComponent<PlayerView>();
        }

        private static ShotView BuildShot(Mats mats)
        {
            var root = new GameObject("Shot");
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball";
            ball.transform.SetParent(root.transform, false);
            Object.DestroyImmediate(ball.GetComponent<Collider>());
            ball.GetComponent<MeshRenderer>().sharedMaterial = mats.Shot;

            // A small white dot on the ball, like the reference picture.
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "Dot";
            dot.transform.SetParent(ball.transform, false);
            dot.transform.localPosition = new Vector3(0f, 0.28f, -0.2f);
            dot.transform.localScale = Vector3.one * 0.3f;
            Object.DestroyImmediate(dot.GetComponent<Collider>());
            dot.GetComponent<MeshRenderer>().sharedMaterial = mats.White;

            LineRenderer ring = NewRing(root.transform, "RadiusRing", mats.Line, 0.07f);
            LineRenderer blast = NewRing(root.transform, "BlastRing", mats.BlastLine, 0.16f);

            ball.SetActive(false);
            var view = root.AddComponent<ShotView>();
            SetRef(view, "ball", ball.transform);
            SetRef(view, "radiusRing", ring);
            SetRef(view, "blastRing", blast);
            return view;
        }

        private static LineRenderer NewRing(Transform parent, string name, Material material, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.loop = true;
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static DoorView BuildDoor(Mats mats, PathModel path, out Transform root)
        {
            var go = new GameObject("Door");
            go.transform.position = path.End;
            go.transform.rotation = Quaternion.LookRotation(path.Direction, Vector3.up);
            root = go.transform;

            Block(go.transform, "PostLeft", mats.Door, new Vector3(-1.55f, 0.95f, 0f), new Vector3(0.4f, 1.9f, 0.5f));
            Block(go.transform, "PostRight", mats.Door, new Vector3(1.55f, 0.95f, 0f), new Vector3(0.4f, 1.9f, 0.5f));
            Block(go.transform, "Beam", mats.Door, new Vector3(0f, 1.95f, 0f), new Vector3(3.5f, 0.4f, 0.5f));
            Transform left = Block(go.transform, "PanelLeft", mats.Door, new Vector3(-0.65f, 0.8f, 0f), new Vector3(1.3f, 1.6f, 0.3f));
            Transform right = Block(go.transform, "PanelRight", mats.Door, new Vector3(0.65f, 0.8f, 0f), new Vector3(1.3f, 1.6f, 0.3f));

            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "Beacon";
            beacon.transform.SetParent(go.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            beacon.transform.localScale = Vector3.one * 0.6f;
            Object.DestroyImmediate(beacon.GetComponent<Collider>());
            beacon.GetComponent<MeshRenderer>().sharedMaterial = mats.White;

            var view = go.AddComponent<DoorView>();
            SetRef(view, "leftPanel", left);
            SetRef(view, "rightPanel", right);
            return view;
        }

        private static Transform Block(Transform parent, string name, Material material, Vector3 localPosition, Vector3 scale)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = scale;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube.transform;
        }

        private static FxView BuildFx(Mats mats)
        {
            var go = new GameObject("ObstacleFx");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.gravityModifier = 1.4f;
            main.maxParticles = 1500;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mats.Particle;
            ps.Play();

            var view = go.AddComponent<FxView>();
            SetRef(view, "particles", ps);
            return view;
        }

        private static FieldView BuildField(Mats mats, List<Spawn> spawns)
        {
            var root = new GameObject("Field");
            var views = new List<Object>(spawns.Count);
            for (int i = 0; i < spawns.Count; i++)
            {
                Spawn s = spawns[i];
                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cap.name = (s.Blocking ? "Gate" + s.Group + "_" : "Scenery_") + i;
                cap.transform.SetParent(root.transform, false);
                cap.transform.localScale = new Vector3(0.6f, s.Height, 0.6f);
                cap.transform.position = new Vector3(s.Position.x, s.Height, s.Position.z);
                Object.DestroyImmediate(cap.GetComponent<Collider>());
                cap.GetComponent<MeshRenderer>().sharedMaterial = mats.Green[i % mats.Green.Length];

                var view = cap.AddComponent<ObstacleView>();
                var so = new SerializedObject(view);
                so.FindProperty("group").intValue = s.Group;
                so.FindProperty("blocking").boolValue = s.Blocking;
                so.ApplyModifiedPropertiesWithoutUndo();
                views.Add(view);
            }

            var field = root.AddComponent<FieldView>();
            var fieldSo = new SerializedObject(field);
            SerializedProperty array = fieldSo.FindProperty("obstacles");
            array.arraySize = views.Count;
            for (int i = 0; i < views.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            fieldSo.ApplyModifiedPropertiesWithoutUndo();
            return field;
        }

        // ------------------------------------------------------------------ HUD

        private static void BuildHud(out HudView hud)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            // Mass bar with the critical zone
            RectTransform bar = Rect("MassBar", canvasGo.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(-200f, 44f));
            Fill(bar, "Back", new Color(0f, 0f, 0f, 0.5f));
            RectTransform playerFill = Fill(bar, "PlayerFill", new Color(1f, 0.6f, 0.1f));
            RectTransform shotFill = Fill(bar, "ShotFill", new Color(1f, 0.82f, 0.55f));
            RectTransform critical = Fill(bar, "CriticalZone", new Color(0.9f, 0.15f, 0.15f, 0.75f));
            Label("BarLabel", canvasGo.transform, font, "MASS", 34, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(0f, 44f));

            Text status = Label("Status", canvasGo.transform, font, "Ready", 52, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(0f, 70f));

            // Hint
            RectTransform hintRect = Rect("Hint", canvasGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 120f));
            Fill(hintRect, "Back", new Color(0f, 0f, 0f, 0.45f));
            StretchLabel("Text", hintRect, font, "Hold the ball, release to fire", 50, TextAnchor.MiddleCenter, Color.white);

            // Result panel
            RectTransform panel = Rect("ResultPanel", canvasGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            Fill(panel, "Dim", new Color(0f, 0f, 0f, 0.6f));
            Text title = Label("Title", panel, font, "Title", 92, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(0f, 130f));
            Text detail = Label("Detail", panel, font, "Detail", 46, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.9f),
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(-120f, 160f));
            RectTransform buttonRect = Rect("RestartButton", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(520f, 140f));
            Fill(buttonRect, "Back", new Color(1f, 0.6f, 0.1f));
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonRect.GetComponentInChildren<Image>();
            StretchLabel("Label", buttonRect, font, "Restart", 60, TextAnchor.MiddleCenter, Color.white);
            panel.gameObject.SetActive(false);

            hud = canvasGo.AddComponent<HudView>();
            SetRef(hud, "playerFill", playerFill);
            SetRef(hud, "shotFill", shotFill);
            SetRef(hud, "criticalZone", critical);
            SetRef(hud, "status", status);
            SetRef(hud, "hint", hintRect.gameObject);
            SetRef(hud, "resultPanel", panel.gameObject);
            SetRef(hud, "resultTitle", title);
            SetRef(hud, "resultDetail", detail);
            SetRef(hud, "restartButton", button);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        private static RectTransform Fill(Transform parent, string name, Color color)
        {
            RectTransform rt = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.gameObject.AddComponent<Image>().color = color;
            return rt;
        }

        private static Text Label(string name, Transform parent, Font font, string text, int size, TextAnchor anchor, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 sizeDelta)
        {
            RectTransform rt = Rect(name, parent, anchorMin, anchorMax, pivot, position, sizeDelta);
            return StyleText(rt, font, text, size, anchor, color);
        }

        private static Text StretchLabel(string name, Transform parent, Font font, string text, int size, TextAnchor anchor, Color color)
        {
            RectTransform rt = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return StyleText(rt, font, text, size, anchor, color);
        }

        private static Text StyleText(RectTransform rt, Font font, string text, int size, TextAnchor anchor, Color color)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.raycastTarget = false;
            return t;
        }

        // ------------------------------------------------------------------ helpers

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) throw new InvalidOperationException(target.GetType().Name + " has no serialized field '" + field + "'.");
            if (value == null) throw new InvalidOperationException("Null value for " + target.GetType().Name + "." + field);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterInBuild()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) scenes.Add(new EditorBuildSettingsScene(s.path, false));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
