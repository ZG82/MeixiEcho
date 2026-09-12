using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace MeixiEcho
{
    /// <summary>
    /// Chapter three's authored environment. Stable architecture and furniture are
    /// generated once; ChapterThreeGameDirector owns the low-allocation game loop.
    /// </summary>
    public sealed partial class ChapterThreeHomestayRuntime : MonoBehaviour
    {
        private const string EnvironmentName = "ChapterThree_Homestay_Environment";
        private const float HomestayScale = 1.12f;
        private Transform root;
        private Material oldWood;
        private Material deepWood;
        private Material plaster;
        private Material floorWood;
        private Material slate;
        private Material linen;
        private Material curtain;
        private Material brass;
        private Material glass;
        private Material screen;
        private Material warmGlow;
        private Material coolGlow;
        private Material windowDaylight;
        private Material leaf;
        private Font worldFont;

        private void Awake()
        {
            GameObject existing = GameObject.Find(EnvironmentName);
            if (existing != null)
            {
                // The shipped chapter scene already contains a baked environment.
                // Apply runtime-only facade additions to that environment as well,
                // otherwise players would never see the authored detail pass.
                root = existing.transform;
                Vector3 savedScale = root.localScale;
                try
                {
                    // Older baked scenes did not contain the marker objects even
                    // though the detail geometry was already present. Re-adding the
                    // pass at runtime created perfectly coplanar duplicates and made
                    // walls, canopies and props flicker. Detect authored children as
                    // a backwards-compatible guard as well as using the markers.
                    bool needsFacadeDetails = root.Find("ChapterThree_RuntimeFacadeDetails") == null &&
                                              root.Find("GuestDoorRainCanopy") == null;
                    bool needsRoomDetails = root.Find("ChapterThree_RuntimeRoomDetails") == null &&
                                            root.Find("LuggageCase") == null;
                    if (needsFacadeDetails || needsRoomDetails)
                    {
                        root.localScale = Vector3.one;
                        BuildMaterials();
                        NormalizeRoomLayout();
                        if (needsRoomDetails)
                        {
                            BuildRoomDetailPass();
                            GameObject roomMarker = new("ChapterThree_RuntimeRoomDetails");
                            roomMarker.transform.SetParent(root, false);
                        }
                        if (needsFacadeDetails)
                        {
                            BuildFacadeDetailPass();
                            GameObject facadeMarker = new("ChapterThree_RuntimeFacadeDetails");
                            facadeMarker.transform.SetParent(root, false);
                        }
                    }
                    NormalizeHomestayTitles();
                }
                finally
                {
                    root.localScale = savedScale;
                }
                return;
            }
            BuildPreview();
        }

        private void NormalizeHomestayTitles()
        {
            if (root == null) return;
            TextMesh[] labels = root.GetComponentsInChildren<TextMesh>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMesh label = labels[i];
                if (label == null) continue;
                string value = label.text == null ? string.Empty : label.text.Trim();
                if (value == "梅溪声暖民宿" || value == "第三章 · 声暖乡野" || value == "第三章·声暖乡野")
                    label.text = "民宿";
            }
        }

        /// <summary>Editor validation hook. Rebuilds the visual slice with no gameplay state.</summary>
        public void BuildPreview()
        {
            GameObject existing = GameObject.Find(EnvironmentName);
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing);
                else DestroyImmediate(existing);
            }
            BuildMaterials();
            BuildScene();
            BuildFirstPersonView();
            if (Application.isPlaying) StaticBatchingUtility.Combine(root.gameObject);
            root.gameObject.AddComponent<ChapterThreeGameDirector>();
            root.gameObject.AddComponent<ChapterThreeAtmosphereRuntime>();
        }

        private void BuildMaterials()
        {
            Texture2D architectureAtlas = Resources.Load<Texture2D>("ChapterThree/ArchitectureSurfaceAtlas");
            Texture2D courtyardAtlas = Resources.Load<Texture2D>("ChapterThree/CourtyardSurfaceAtlas");
            Texture2D stairAtlas = Resources.Load<Texture2D>("ChapterThree/StairSurfaceAtlas");
            Texture2D decorAtlas = Resources.Load<Texture2D>("ChapterThree/InteriorDecorAtlas");
            oldWood = AtlasMaterial("Chapter3_AgedWood", architectureAtlas, new Vector2(.5f, .5f), new Color(.82f, .73f, .64f), .30f);
            deepWood = AtlasMaterial("Chapter3_DeepWood", architectureAtlas, new Vector2(.5f, .5f), new Color(.50f, .39f, .30f), .40f);
            plaster = AtlasMaterial("Chapter3_WarmPlaster", architectureAtlas, new Vector2(0f, .5f), new Color(.96f, .91f, .82f), .13f);
            floorWood = AtlasMaterial("Chapter3_FloorWood", stairAtlas, new Vector2(0f, .5f), new Color(.79f, .67f, .55f), .28f);
            slate = AtlasMaterial("Chapter3_Slate", courtyardAtlas, new Vector2(0f, .5f), new Color(.79f, .83f, .82f), .18f);
            linen = GridAtlasMaterial("Chapter3_Linen", decorAtlas, 1, 2, 3, 3, new Color(.96f, .94f, .89f), .025f);
            curtain = GridAtlasMaterial("Chapter3_Curtain", decorAtlas, 0, 1, 3, 3, new Color(.72f, .80f, .86f), .03f);
            brass = AtlasMaterial("Chapter3_Brass", stairAtlas, new Vector2(.5f, 0f), new Color(.76f, .67f, .50f), .66f);
            // Low-alpha transparent glass lets the courtyard and sky remain
            // legible from inside instead of reading as a black/blue panel.
            glass = Material("Chapter3_WindowGlass", new Color(.68f, .87f, .92f, .075f), .74f, true);
            // Video and poster surfaces must remain visible independent of room
            // lighting. Lit materials made both surfaces appear black in dim rooms.
            screen = UnlitMaterial("Chapter3_TvScreen", new Color(.008f, .01f, .012f));
            warmGlow = Emissive("Chapter3_WarmLight", new Color(1f, .62f, .27f), 3.2f);
            coolGlow = Emissive("Chapter3_CoolLight", new Color(.09f, .68f, .88f), 2.8f);
            windowDaylight = Material("Chapter3_WindowDaylight", new Color(.65f, .82f, .88f, .018f), .12f, true);
            leaf = Material("Chapter3_Leaves", new Color(.11f, .31f, .16f), .08f);
            worldFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 64);
            if (worldFont != null)
            {
                Shader depthShader = Shader.Find("MeixiEcho/WorldTextDepth");
                worldTextMaterial = depthShader != null
                    ? new Material(depthShader) { name = "Chapter3_WorldTextDepth" }
                    : new Material(worldFont.material) { name = "Chapter3_WorldTextDepth_Fallback" };
                worldTextMaterial.mainTexture = worldFont.material.mainTexture;
                if (worldTextMaterial.HasProperty("_ZTest")) worldTextMaterial.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                worldTextMaterial.renderQueue = (int)RenderQueue.AlphaTest + 8;
            }
            BuildRefinedMaterials();

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.07f, .085f, .095f);
            RenderSettings.fogDensity = .0105f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.42f, .48f, .55f);
            RenderSettings.ambientEquatorColor = new Color(.30f, .29f, .27f);
            RenderSettings.ambientGroundColor = new Color(.18f, .17f, .16f);
            RenderSettings.ambientIntensity = 1.08f;
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material sky = new(skyShader) { name = "Chapter3_ProceduralSky" };
                sky.SetColor("_SkyTint", new Color(.31f, .43f, .60f));
                sky.SetColor("_GroundColor", new Color(.13f, .10f, .08f));
                sky.SetFloat("_Exposure", 1.06f);
                sky.SetFloat("_AtmosphereThickness", 1.05f);
                sky.SetFloat("_SunSize", .025f);
                RenderSettings.skybox = sky;
            }
        }

        private void BuildScene()
        {
            root = new GameObject(EnvironmentName).transform;
            BuildRefinedOutdoorEnvironment();
            BuildTwoStoreyHomestay();
            BuildRefinedCourtyardDetails();
            BuildLightingAndAtmosphere();
            ChapterThreeCraftedFurniture.Apply(root);
            // Persist these markers into the baked scene. Their absence previously
            // caused Awake to generate a second, perfectly overlapping detail pass.
            GameObject roomMarker = new("ChapterThree_RuntimeRoomDetails");
            roomMarker.transform.SetParent(root, false);
            GameObject facadeMarker = new("ChapterThree_RuntimeFacadeDetails");
            facadeMarker.transform.SetParent(root, false);
            // Apply scaling after world-space authoring so every room and exterior
            // detail grows around the same origin.
            root.localScale = Vector3.one * HomestayScale;
        }

        private void BuildCourtyardAndFacade()
        {
            // Split the large courtyard mesh so vertex-lit accent lamps cannot paint
            // a single giant cyan triangle across the entire ground plane.
            const int groundColumns = 6;
            const int groundRows = 5;
            float groundTileX = 23f / groundColumns;
            float groundTileZ = 18f / groundRows;
            for (int x = 0; x < groundColumns; x++)
            for (int z = 0; z < groundRows; z++)
                Box("CourtyardGroundTile", new Vector3(-11.5f + groundTileX * (x + .5f), -.28f, -11.2f + groundTileZ * (z + .5f)),
                    new Vector3(groundTileX + .025f, .56f, groundTileZ + .025f), plaster, true);
            Box("EntryPath", new Vector3(0f, .02f, -6.8f), new Vector3(3.35f, .08f, 15f), slate, true);
            for (int i = 0; i < 18; i++)
            {
                GameObject stone = Box("IrregularPathStone", new Vector3(Mathf.Sin(i * 1.73f) * .17f, .085f, -13.6f + i * .82f),
                    new Vector3(2.5f + (i % 3) * .18f, .12f, .65f), slate, false);
                stone.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(i * .9f) * 2.5f, 0f);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Box("CourtyardSideWall", new Vector3(side * 10.8f, 1.62f, -2.0f), new Vector3(.48f, 3.25f, 18f), plaster, true);
                for (int i = 0; i < 9; i++)
                {
                    float z = -9.6f + i * 1.9f;
                    Box("FencePost", new Vector3(side * 10.18f, 1.18f, z), new Vector3(.14f, 2.36f, .14f), deepWood, true);
                    Box("FenceRail", new Vector3(side * 10.18f, .86f, z + .78f), new Vector3(.11f, .11f, 1.65f), deepWood, true);
                }
            }
            CreateTree(new Vector3(-7.35f, 0f, -3.9f), 1.18f);
            CreateTree(new Vector3(7.8f, 0f, -6.15f), .94f);
            CreateStoneTable(new Vector3(5.4f, 0f, -7.3f));
            CreateLantern(new Vector3(-2.55f, 2.72f, 4.55f));
            CreateLantern(new Vector3(2.55f, 2.72f, 4.55f));

            Box("PorchDeck", new Vector3(0f, .10f, 4.65f), new Vector3(13.7f, .20f, 1.95f), floorWood, true);
            Box("FacadeLower", new Vector3(0f, 1.10f, 5.65f), new Vector3(12.4f, 2.20f, .42f), deepWood, true);
            Box("FacadeUpper", new Vector3(0f, 3.06f, 5.65f), new Vector3(12.4f, 1.72f, .42f), plaster, true);
            for (int i = -5; i <= 5; i++) Box("FacadeVerticalTimber", new Vector3(i * 1.08f, 2.08f, 5.36f), new Vector3(.16f, 3.7f, .24f), oldWood, false);
            for (int i = -5; i <= 5; i++) Box("PorchSupport", new Vector3(i * 1.08f, 3.72f, 4.86f), new Vector3(.12f, 2.18f, .12f), deepWood, true);
            Box("PorchHeader", new Vector3(0f, 4.72f, 4.86f), new Vector3(12.1f, .22f, .28f), deepWood, true);
            Box("RoofSlab", new Vector3(0f, 5.08f, 5.85f), new Vector3(14.6f, .48f, 3.72f), deepWood, true).transform.rotation = Quaternion.Euler(7f, 0f, 0f);
            Box("DoorFrameLeft", new Vector3(-1.07f, 1.65f, 4.93f), new Vector3(.18f, 3.3f, .36f), oldWood, true);
            Box("DoorFrameRight", new Vector3(1.07f, 1.65f, 4.93f), new Vector3(.18f, 3.3f, .36f), oldWood, true);
            Box("DoorFrameTop", new Vector3(0f, 3.23f, 4.93f), new Vector3(2.3f, .18f, .36f), oldWood, true);
            CreateWorldText("民宿", new Vector3(0f, 4.12f, 4.59f), .088f, new Color(1f, .74f, .33f));
        }

        private void BuildRoomShell()
        {
            Box("RoomFloor", new Vector3(0f, 0f, 10f), new Vector3(11.2f, .18f, 9.2f), floorWood, true);
            Box("BackWallLeft", new Vector3(-3.35f, 2.05f, 14.46f), new Vector3(4.5f, 4.10f, .34f), plaster, true);
            Box("BackWallRight", new Vector3(3.35f, 2.05f, 14.46f), new Vector3(4.5f, 4.10f, .34f), plaster, true);
            Box("BackDoorHeader", new Vector3(0f, 3.65f, 14.46f), new Vector3(2.2f, .90f, .34f), deepWood, true);
            Box("LeftWall", new Vector3(-5.45f, 2.05f, 10f), new Vector3(.34f, 4.10f, 9.2f), deepWood, true);
            Box("RightWall", new Vector3(5.45f, 2.05f, 10f), new Vector3(.34f, 4.10f, 9.2f), deepWood, true);
            Box("FrontWallLeft", new Vector3(-3.85f, 2.05f, 5.54f), new Vector3(3.2f, 4.10f, .34f), plaster, true);
            Box("FrontWallRight", new Vector3(3.85f, 2.05f, 5.54f), new Vector3(3.2f, 4.10f, .34f), plaster, true);
            Box("Ceiling", new Vector3(0f, 4.12f, 10f), new Vector3(11.2f, .18f, 9.2f), oldWood, true);
            for (int i = -4; i <= 4; i++) Box("CeilingBeam", new Vector3(i * 1.22f, 3.97f, 10f), new Vector3(.14f, .28f, 8.85f), deepWood, false);
            for (int i = 0; i < 8; i++) Box("FloorBoardSeam", new Vector3(-5.08f + i * 1.45f, .105f, 10f), new Vector3(.024f, .02f, 8.88f), deepWood, false);
            Box("WovenRug", new Vector3(.55f, .125f, 9.48f), new Vector3(3.45f, .05f, 2.05f), curtain, false);
            for (int i = 0; i < 5; i++) Box("RugStripe", new Vector3(.55f, .158f, 8.78f + i * .35f), new Vector3(3.1f, .014f, .06f), brass, false);
        }

        private void BuildBedZone()
        {
            Box("BedFrame", new Vector3(2.62f, .49f, 10.18f), new Vector3(4.30f, .72f, 2.55f), deepWood, true);
            Box("Mattress", new Vector3(2.62f, .94f, 10.18f), new Vector3(4.02f, .34f, 2.32f), linen, false);
            Box("Quilt", new Vector3(2.62f, 1.13f, 9.60f), new Vector3(3.70f, .18f, 1.10f), curtain, false);
            Box("QuiltFold", new Vector3(2.62f, 1.24f, 9.00f), new Vector3(3.68f, .12f, .28f), linen, false);
            Box("PillowA", new Vector3(1.72f, 1.18f, 11.03f), new Vector3(1.36f, .22f, .62f), linen, false);
            Box("PillowB", new Vector3(3.42f, 1.18f, 11.03f), new Vector3(1.36f, .22f, .62f), linen, false);
            Box("Headboard", new Vector3(2.62f, 1.89f, 11.47f), new Vector3(4.30f, 2.62f, .18f), oldWood, true);
            for (int i = 0; i < 7; i++) Box("HeadboardSlat", new Vector3(.78f + i * .61f, 1.9f, 11.32f), new Vector3(.08f, 2.34f, .08f), deepWood, false);
            Box("BedsideTable", new Vector3(4.78f, .63f, 11.0f), new Vector3(.78f, 1.10f, .82f), deepWood, true);
            Box("BedsideDrawer", new Vector3(4.78f, .72f, 10.56f), new Vector3(.52f, .26f, .06f), oldWood, false);
            Sphere("BedsideKnob", new Vector3(4.78f, .72f, 10.50f), Vector3.one * .075f, brass);
            Cylinder("CeramicCup", new Vector3(4.62f, 1.26f, 11.05f), new Vector3(.12f, .18f, .12f), linen);
            Box("MossyBedsheetTrace", new Vector3(3.66f, 1.255f, 9.45f), new Vector3(.45f, .018f, .16f), leaf, false);
        }

        private void BuildDeskZone()
        {
            Box("DeskTop", new Vector3(-3.55f, 1.15f, 7.78f), new Vector3(3.15f, .18f, 1.04f), oldWood, true);
            for (int side = -1; side <= 1; side += 2) Box("DeskLeg", new Vector3(-3.55f + side * 1.25f, .56f, 7.78f), new Vector3(.18f, 1.2f, .82f), deepWood, true);
            Box("DeskDrawerStack", new Vector3(-2.34f, .62f, 7.78f), new Vector3(.64f, .94f, .84f), deepWood, true);
            for (int i = 0; i < 3; i++)
            {
                Box("DeskDrawerFace", new Vector3(-2.34f, .36f + i * .31f, 7.31f), new Vector3(.48f, .17f, .06f), oldWood, false);
                Sphere("DeskDrawerKnob", new Vector3(-2.34f, .36f + i * .31f, 7.24f), Vector3.one * .055f, brass);
            }
            Box("DeskStoolSeat", new Vector3(-3.55f, .47f, 6.46f), new Vector3(1.05f, .16f, .90f), curtain, true);
            for (int side = -1; side <= 1; side += 2) Box("StoolLeg", new Vector3(-3.55f + side * .36f, .24f, 6.46f), new Vector3(.08f, .48f, .08f), deepWood, true);
            Box("Notebook", new Vector3(-4.05f, 1.28f, 7.79f), new Vector3(.84f, .055f, .60f), linen, false).transform.rotation = Quaternion.Euler(0f, -18f, 0f);
            Box("NotebookSpine", new Vector3(-4.05f, 1.318f, 7.49f), new Vector3(.85f, .028f, .035f), deepWood, false).transform.rotation = Quaternion.Euler(0f, -18f, 0f);
            Box("RadioBody", new Vector3(-3.00f, 1.37f, 7.78f), new Vector3(.56f, .30f, .36f), screen, false);
            Box("RadioPanel", new Vector3(-3.00f, 1.37f, 7.56f), new Vector3(.38f, .14f, .025f), brass, false);
            Sphere("RadioDial", new Vector3(-2.76f, 1.37f, 7.52f), Vector3.one * .075f, brass);
            CreateDeskLamp(new Vector3(-2.78f, 1.52f, 7.86f));
        }

        private void BuildTelevisionWall()
        {
            Box("TvCabinet", new Vector3(-5.03f, .68f, 9.45f), new Vector3(.42f, 1.36f, 2.24f), deepWood, true);
            for (int i = 0; i < 3; i++) Box("TvCabinetShelf", new Vector3(-5.28f, .42f + i * .38f, 9.45f), new Vector3(.06f, .045f, 1.88f), oldWood, false);
            Box("TvScreen", new Vector3(-5.26f, 2.15f, 9.45f), new Vector3(.10f, 1.50f, 2.58f), screen, false);
            Box("TvStand", new Vector3(-5.15f, 1.30f, 9.45f), new Vector3(.18f, .18f, 1.15f), deepWood, false);
            Box("ScreenScanLine", new Vector3(-5.315f, 1.54f, 9.45f), new Vector3(.018f, .025f, 1.92f), coolGlow, false);
            Box("AirConditioner", new Vector3(-5.02f, 3.42f, 10.82f), new Vector3(.30f, .50f, 2.28f), plaster, false);
            for (int i = 0; i < 6; i++) Box("AcVent", new Vector3(-5.19f, 3.42f, 10.1f + i * .28f), new Vector3(.025f, .07f, .06f), deepWood, false);
            CreateFrame(new Vector3(-5.00f, 2.46f, 11.95f), "民宿守则");
            CreateFrame(new Vector3(-5.00f, 2.46f, 7.05f), "山路留声");
        }

        private void BuildWardrobeAndWindow()
        {
            Box("WardrobeBody", new Vector3(3.92f, 2.05f, 13.68f), new Vector3(2.74f, 3.82f, .64f), deepWood, true);
            Box("WardrobeDoorA", new Vector3(3.24f, 2.05f, 13.29f), new Vector3(1.24f, 3.55f, .10f), oldWood, true);
            Box("WardrobeDoorB", new Vector3(4.57f, 2.05f, 13.29f), new Vector3(1.24f, 3.55f, .10f), oldWood, true);
            for (int i = 0; i < 3; i++)
            {
                Box("WardrobePanelA", new Vector3(3.24f, .92f + i * 1.1f, 13.22f), new Vector3(.92f, .60f, .035f), deepWood, false);
                Box("WardrobePanelB", new Vector3(4.57f, .92f + i * 1.1f, 13.22f), new Vector3(.92f, .60f, .035f), deepWood, false);
            }
            Sphere("WardrobeKnobA", new Vector3(3.70f, 2.05f, 13.18f), Vector3.one * .085f, brass);
            Sphere("WardrobeKnobB", new Vector3(4.10f, 2.05f, 13.18f), Vector3.one * .085f, brass);

            Vector3 window = new(-3.32f, 2.38f, 14.22f);
            Box("WindowGlass", window, new Vector3(1.82f, 2.24f, .08f), glass, false);
            Box("WindowTop", window + Vector3.up * 1.14f, new Vector3(2.16f, .14f, .18f), oldWood, false);
            Box("WindowBottom", window + Vector3.down * 1.14f, new Vector3(2.16f, .14f, .18f), oldWood, false);
            Box("WindowLeft", window + Vector3.left * 1.02f, new Vector3(.14f, 2.36f, .18f), oldWood, false);
            Box("WindowRight", window + Vector3.right * 1.02f, new Vector3(.14f, 2.36f, .18f), oldWood, false);
            Box("WindowMullion", window, new Vector3(.10f, 2.22f, .20f), oldWood, false);
            Box("CurtainLeft", window + new Vector3(-1.18f, 0f, -.10f), new Vector3(.32f, 2.62f, .16f), curtain, false);
            Box("CurtainRight", window + new Vector3(1.18f, 0f, -.10f), new Vector3(.32f, 2.62f, .16f), curtain, false);
            Box("WindowSill", new Vector3(-3.32f, 1.20f, 13.98f), new Vector3(2.22f, .12f, .34f), oldWood, false);
        }

        private void BuildExplorationWing()
        {
            Box("GuestCorridorFloor", new Vector3(0f, 0f, 18.45f), new Vector3(3.7f, .18f, 8.05f), floorWood, true);
            Box("GuestCorridorCeiling", new Vector3(0f, 4.08f, 18.45f), new Vector3(3.7f, .18f, 8.05f), deepWood, true);
            Box("GuestCorridorEndWall", new Vector3(0f, 2.02f, 22.45f), new Vector3(3.7f, 4.04f, .28f), plaster, true);
            for (int i = 0; i < 7; i++)
                Box("GuestCorridorBeam", new Vector3(0f, 3.88f, 15.0f + i * 1.15f), new Vector3(3.62f, .18f, .12f), oldWood, false);

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 4.05f;
                Box("GuestWingOuterWall", new Vector3(side * 6.05f, 2.02f, 18.45f), new Vector3(.30f, 4.04f, 8.05f), deepWood, true);
                Box("GuestWingFloor", new Vector3(x, 0f, 18.45f), new Vector3(4.2f, .18f, 8.05f), floorWood, true);
                Box("GuestWingCeiling", new Vector3(x, 4.08f, 18.45f), new Vector3(4.2f, .18f, 8.05f), oldWood, true);
                Box("GuestWingFrontWall", new Vector3(x, 2.02f, 14.62f), new Vector3(4.2f, 4.04f, .25f), plaster, true);
                Box("GuestWingMiddleWall", new Vector3(x, 2.02f, 18.45f), new Vector3(4.2f, 4.04f, .20f), plaster, true);
                Box("GuestWingBackWall", new Vector3(x, 2.02f, 22.28f), new Vector3(4.2f, 4.04f, .25f), plaster, true);
                for (int room = 0; room < 2; room++)
                {
                    float z = room == 0 ? 16.15f : 20.25f;
                    Box("GuestBedFrame", new Vector3(x + side * .55f, .42f, z), new Vector3(2.35f, .58f, 1.35f), deepWood, true);
                    Box("GuestMattress", new Vector3(x + side * .55f, .78f, z), new Vector3(2.18f, .22f, 1.22f), linen, false);
                    Box("GuestDesk", new Vector3(x - side * 1.22f, .82f, z + .72f), new Vector3(.78f, .10f, 1.05f), oldWood, true);
                    Box("GuestDoorPost", new Vector3(side * 1.91f, 1.72f, z - .72f), new Vector3(.16f, 3.44f, .16f), oldWood, true);
                    CreateWorldText($"20{room * 2 + (side > 0 ? 2 : 1)}", new Vector3(side * 1.82f, 2.55f, z - .72f), .026f,
                        new Color(.88f, .72f, .43f), Quaternion.Euler(0f, side > 0 ? 90f : -90f, 0f));
                }
            }
            Box("ArchiveTable", new Vector3(0f, .72f, 21.05f), new Vector3(2.25f, 1.12f, 1.10f), oldWood, true);
            CreateWorldText("实践档案", new Vector3(0f, 2.72f, 22.24f), .040f, new Color(1f, .70f, .30f));
            CreatePointLight(new Vector3(0f, 3.35f, 16.2f), new Color(.23f, .72f, .78f), .72f, 4.4f);
            CreatePointLight(new Vector3(0f, 3.35f, 20.6f), new Color(1f, .43f, .18f), .88f, 4.8f);
        }

        private void BuildSmallDetails()
        {
            CreatePlant(new Vector3(4.62f, .80f, 7.86f));
            CreateKettle(new Vector3(4.04f, 1.19f, 7.72f));
            for (int i = 0; i < 4; i++) Box("BookStack", new Vector3(-4.88f, .44f + i * .16f, 10.62f), new Vector3(.30f, .12f, .64f - i * .07f), i % 2 == 0 ? oldWood : linen, false);
            for (int i = 0; i < 5; i++)
            {
                float z = 8.26f + i * .58f;
                Sphere("WallSoundLamp", new Vector3(-5.15f, 2.02f, z), Vector3.one * .15f, i == 2 ? warmGlow : brass);
                CreatePointLight(new Vector3(-4.98f, 2.02f, z), i == 2 ? new Color(1f, .32f, .08f) : new Color(.10f, .55f, .70f), .45f, 1.55f);
            }
            CreateWorldText("ROOM 204", new Vector3(-3.0f, 2.72f, 5.34f), .042f, new Color(.86f, .74f, .54f));
            Box("WallNotice", new Vector3(1.76f, 2.20f, 14.17f), new Vector3(1.12f, 1.02f, .045f), linen, false);
            Box("WallNoticeTape", new Vector3(1.76f, 2.72f, 14.12f), new Vector3(.38f, .07f, .018f), brass, false);
            CreateDustMotes();
        }

        private void BuildLightingAndAtmosphere()
        {
            GameObject moonObject = new("Moonlight"); moonObject.transform.SetParent(root, false);
            Light moon = moonObject.AddComponent<Light>(); moon.type = LightType.Directional; moon.color = new Color(.19f, .33f, .44f); moon.intensity = .52f; moon.shadows = LightShadows.Soft; moonObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            CreatePointLight(new Vector3(0f, 3.65f, 8.45f), new Color(1f, .42f, .19f), 1.75f, 8f);
            CreatePointLight(new Vector3(2.75f, 1.8f, 10.5f), new Color(1f, .54f, .25f), 1.1f, 4.1f);
            CreatePointLight(new Vector3(-3.8f, 2.5f, 5.15f), new Color(.12f, .48f, .68f), .34f, 4.2f);
        }

        private void BuildFirstPersonView()
        {
            GameObject player = new("ChapterThree_FirstPersonWalker");
            player.transform.SetParent(root, false); player.transform.localPosition = new Vector3(0f, .1f, -7.5f);
            CharacterController capsule = player.AddComponent<CharacterController>(); capsule.height = 1.78f; capsule.radius = .32f; capsule.center = new Vector3(0f, .89f, 0f);
            ChapterThreeFirstPersonWalkthrough walkthrough = player.AddComponent<ChapterThreeFirstPersonWalkthrough>(); walkthrough.Bind(capsule);
            GameObject cameraObject = new("FirstPersonCamera"); cameraObject.transform.SetParent(player.transform, false); cameraObject.transform.localPosition = new Vector3(0f, 1.58f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera"; camera.fieldOfView = 68f; camera.nearClipPlane = .035f; camera.farClipPlane = 180f; cameraObject.AddComponent<AudioListener>();
        }

        private GameObject Box(string name, Vector3 position, Vector3 scale, Material material, bool collider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name; box.transform.SetParent(root, false); box.transform.position = position; box.transform.localScale = scale;
            Renderer renderer = box.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            if (!collider) RemoveCollider(box.GetComponent<Collider>());
            return box;
        }

        private GameObject Sphere(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = name; sphere.transform.SetParent(root, false); sphere.transform.position = position; sphere.transform.localScale = scale; sphere.GetComponent<Renderer>().sharedMaterial = material; RemoveCollider(sphere.GetComponent<Collider>()); return sphere;
        }

        private GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cylinder.name = name; cylinder.transform.SetParent(root, false); cylinder.transform.position = position; cylinder.transform.localScale = scale; cylinder.GetComponent<Renderer>().sharedMaterial = material; RemoveCollider(cylinder.GetComponent<Collider>()); return cylinder;
        }

        private static void RemoveCollider(Collider collider)
        {
            if (collider == null) return;
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }

        private void CreateLantern(Vector3 position)
        {
            Box("LanternPost", position + Vector3.down * 1.0f, new Vector3(.10f, 2.2f, .10f), deepWood, true);
            Sphere("LanternGlow", position, new Vector3(.32f, .42f, .32f), warmGlow);
            CreatePointLight(position, new Color(1f, .34f, .10f), 1.55f, 4.4f);
        }

        private void CreateTree(Vector3 position, float scale)
        {
            Cylinder("TreeTrunk", position + Vector3.up * 1.5f, new Vector3(.38f, 3f, .38f) * scale, deepWood);
            for (int i = 0; i < 7; i++) Sphere("TreeCanopy", position + Vector3.up * (3.1f + (i % 2) * .52f) + new Vector3(Mathf.Sin(i * 1.7f), 0f, Mathf.Cos(i * 1.4f)) * .65f, Vector3.one * (1.18f * scale), leaf);
        }

        private void CreateStoneTable(Vector3 position)
        {
            Cylinder("StoneTableBase", position + Vector3.up * .36f, new Vector3(1.2f, .72f, 1.2f), plaster);
            Cylinder("StoneTableTop", position + Vector3.up * .78f, new Vector3(2.15f, .18f, 2.15f), slate);
            for (int i = 0; i < 3; i++) Cylinder("StoneStool", position + new Vector3(Mathf.Sin(i * 2.1f) * 1.7f, .32f, Mathf.Cos(i * 2.1f) * 1.7f), new Vector3(.48f, .64f, .48f), slate);
        }

        private void CreateDeskLamp(Vector3 position)
        {
            Cylinder("DeskLampStem", position + Vector3.up * .30f, new Vector3(.07f, .60f, .07f), brass);
            Sphere("DeskLampShade", position + Vector3.up * .64f, new Vector3(.48f, .28f, .48f), warmGlow);
            CreatePointLight(position + Vector3.up * .60f, new Color(1f, .36f, .12f), 1.65f, 3.4f);
        }

        private void CreatePlant(Vector3 position)
        {
            Cylinder("PlantPot", position, new Vector3(.45f, .55f, .45f), plaster);
            for (int i = 0; i < 7; i++) Sphere("PlantLeaf", position + Vector3.up * (.58f + i * .08f) + new Vector3(Mathf.Sin(i * 1.4f) * .3f, 0f, Mathf.Cos(i * 1.1f) * .3f), new Vector3(.12f, .6f, .25f), leaf);
        }

        private void CreateKettle(Vector3 position)
        {
            Sphere("KettleBody", position, new Vector3(.54f, .42f, .54f), brass);
            Cylinder("KettleNeck", position + Vector3.forward * .42f + Vector3.up * .08f, new Vector3(.18f, .16f, .18f), brass);
            Cylinder("TeaCupA", position + new Vector3(.55f, .06f, 0f), new Vector3(.12f, .18f, .12f), linen);
            Cylinder("TeaCupB", position + new Vector3(.78f, .06f, .20f), new Vector3(.12f, .18f, .12f), linen);
        }

        private void CreateFrame(Vector3 position, string label)
        {
            Box("PictureFrame", position + Vector3.forward * .04f, new Vector3(.12f, 1.16f, 1.46f), oldWood, false);
            Box("PicturePaper", position, new Vector3(.08f, .94f, 1.24f), linen, false);
            CreateWorldText(label, position + Vector3.right * .07f, .023f, new Color(.25f, .16f, .09f), Quaternion.Euler(0f, -90f, 0f));
        }

        private void CreateDustMotes()
        {
            GameObject dust = new("DustMotes"); dust.transform.SetParent(root, false);
            ParticleSystem particles = dust.AddComponent<ParticleSystem>();
            var main = particles.main; main.loop = true; main.startLifetime = 7f; main.startSpeed = .08f; main.startSize = .025f; main.startColor = new Color(1f, .78f, .48f, .22f); main.maxParticles = 70;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(8f, 3f, 7f);
            var emission = particles.emission; emission.rateOverTime = 10f;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = warmGlow;
        }

        private void CreateWorldText(string value, Vector3 position, float size, Color color)
        {
            CreateWorldText(value, position, size, color, Quaternion.identity);
        }

        private void CreateWorldText(string value, Vector3 position, float size, Color color, Quaternion rotation)
        {
            GameObject objectText = new("WorldText_" + value); objectText.transform.SetParent(root, false); objectText.transform.position = position; objectText.transform.rotation = rotation;
            Font font = worldFont ?? Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 64);
            TextMesh text = objectText.AddComponent<TextMesh>(); text.text = value; text.font = font; text.fontSize = 64; text.characterSize = size; text.color = color; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            MeshRenderer renderer = objectText.GetComponent<MeshRenderer>();
            if (renderer != null && font != null)
            {
                renderer.sharedMaterial = worldTextMaterial != null ? worldTextMaterial : font.material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = 16;
            }
        }

        private void CreatePointLight(Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = new(position.z < BuildingFront ? "Chapter3_ExteriorLight" : "Chapter3_InteriorLight"); lightObject.transform.SetParent(root, false); lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
        }

        private static Material Material(string name, Color color, float smoothness, bool transparent = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (transparent)
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
                if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
                if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            return material;
        }

        private static Material AtlasMaterial(string name, Texture2D atlas, Vector2 offset, Color tint, float smoothness)
        {
            Material material = Material(name, tint, smoothness);
            if (atlas == null || !material.HasProperty("_BaseMap")) return material;
            material.SetTexture("_BaseMap", atlas);
            material.SetTextureScale("_BaseMap", new Vector2(.5f, .5f));
            material.SetTextureOffset("_BaseMap", offset);
            return material;
        }

        private static Material GridAtlasMaterial(string name, Texture2D atlas, int column, int row,
            int columns, int rows, Color tint, float smoothness)
        {
            Material material = Material(name, tint, smoothness);
            if (atlas == null || !material.HasProperty("_BaseMap")) return material;
            float cellWidth = 1f / Mathf.Max(1, columns);
            float cellHeight = 1f / Mathf.Max(1, rows);
            material.SetTexture("_BaseMap", atlas);
            material.SetTextureScale("_BaseMap", new Vector2(cellWidth, cellHeight));
            material.SetTextureOffset("_BaseMap", new Vector2(column * cellWidth, (rows - 1 - row) * cellHeight));
            return material;
        }

        private static Material Emissive(string name, Color color, float intensity)
        {
            Material material = Material(name, color, .5f);
            if (material.HasProperty("_EmissionColor")) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * intensity); }
            return material;
        }

        private static Material UnlitMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            Material material = new(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }

        private static Material UnlitTextureMaterial(string name, Texture texture, Color tint)
        {
            Material material = UnlitMaterial(name, tint);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            return material;
        }
    }

}
