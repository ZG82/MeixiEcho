using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MeixiEcho
{
    /// <summary>Creates the chapter-two theatre after its tiny bootstrap scene has loaded.</summary>
    public static class TheatreRuntimeEnvironment
    {
        private const string MarkerName = "Theatre_Set_RuntimeGenerated";

        public static void Ensure(Transform host)
        {
            if (host.Find(MarkerName) != null) return;
            Transform root = new GameObject(MarkerName).transform;
            root.SetParent(host, false);

            Material cream = Mat("AgedLimePlaster", new Color(.78f, .70f, .57f), .12f);
            Material wood = Mat("DarkRedWood", new Color(.36f, .085f, .038f), .34f);
            Material lightWood = Mat("InstrumentSoundboard", new Color(.56f, .30f, .12f), .30f);
            Material floor = Mat("PolishedWood", new Color(.39f, .16f, .066f), .38f);
            Material bronze = Mat("AgedBronze", new Color(.42f, .265f, .075f), .58f, .82f);
            Material patina = Mat("BronzePatina", new Color(.075f, .22f, .19f), .32f, .65f);
            Material iron = Mat("BlackIron", new Color(.115f, .085f, .066f), .42f, .72f);
            Material curtain = Mat("CreamValance", new Color(.78f, .65f, .41f), .18f);
            Material carpet = Mat("AudienceCarpet", new Color(.070f, .205f, .135f), .22f);
            Material red = Mat("TheatreRed", new Color(.42f, .045f, .028f), .30f);
            Material ivory = Mat("DrumHide", new Color(.76f, .64f, .43f), .24f);
            Material paper = Mat("AgedScriptPaper", new Color(.72f, .60f, .39f), .10f);
            Material porcelain = Mat("OldPorcelain", new Color(.70f, .74f, .68f), .52f);
            Material fadedBlue = Mat("FadedBackstageBlue", new Color(.12f, .23f, .24f), .18f);
            Material costume = Mat("StoredCostumeFabric", new Color(.28f, .055f, .045f), .14f);
            Material stringGlow = EmissiveMat("InstrumentStrings", new Color(1f, .58f, .16f), 1.25f);
            ApplyMaterialTexture(cream, SurfaceStyle.Plaster);
            ApplyMaterialTexture(wood, SurfaceStyle.Wood);
            ApplyMaterialTexture(lightWood, SurfaceStyle.Wood);
            ApplyMaterialTexture(floor, SurfaceStyle.Wood);
            ApplyMaterialTexture(curtain, SurfaceStyle.Fabric);
            ApplyMaterialTexture(carpet, SurfaceStyle.Fabric);
            ApplyMaterialTexture(red, SurfaceStyle.Wood);
            ApplyMaterialTexture(ivory, SurfaceStyle.Fabric);
            ApplyMaterialTexture(fadedBlue, SurfaceStyle.Plaster);
            ApplyMaterialTexture(costume, SurfaceStyle.Fabric);

            Box("HallFloor", root, new Vector3(0, -.12f, 0), new Vector3(20, .24f, 27), floor, true);
            Box("AudienceCarpet", root, new Vector3(0, .015f, -6.2f), new Vector3(17.4f, .045f, 14.8f), carpet, false);
            Box("Stage", root, new Vector3(0, .55f, 8.65f), new Vector3(16.4f, 1.1f, 7.5f), floor, true);
            Box("StageApron", root, new Vector3(0, .22f, 5.03f), new Vector3(16.5f, .45f, .42f), wood, true);
            for (int i = 0; i < 5; i++)
            {
                float h = .22f * (i + 1);
                Box("CentralStep_" + i, root, new Vector3(0, h * .5f, 3.70f + i * .31f), new Vector3(4.7f, h, .43f), floor, true);
            }
            // Segmented walls leave four human-scale side doors and two stage doors.
            // The adjoining rooms are built below, so every opening leads to real space.
            BuildHallSideWall(root, -1, cream);
            BuildHallSideWall(root, 1, cream);
            BuildStageRearWall(root, cream);
            Box("Ceiling", root, new Vector3(0, 8.6f, 0), new Vector3(20, .25f, 25), cream, false);
            // Enclose the audience hall while preserving a centered timber doorway.
            Box("FrontWallLeft", root, new Vector3(-5.9f, 4.3f, -13.35f), new Vector3(8.2f, 8.6f, .34f), cream, true);
            Box("FrontWallRight", root, new Vector3(5.9f, 4.3f, -13.35f), new Vector3(8.2f, 8.6f, .34f), cream, true);
            Box("FrontWallLintel", root, new Vector3(0, 6.7f, -13.35f), new Vector3(3.8f, 3.8f, .34f), cream, true);

            BuildBackdrop(root, wood);
            BuildArchitecture(root, wood, red, curtain, bronze);
            BuildWallDecor(root, wood, red, bronze);
            BuildSurfaceDetails(root, wood, lightWood, bronze, iron);
            BuildAudience(root, iron, wood);
            BuildExplorationRooms(root, cream, wood, lightWood, floor, bronze, iron, paper,
                porcelain, fadedBlue, costume, curtain);
            BuildChime(root, bronze, patina, iron, stringGlow);
            BuildDrum(root, wood, bronze, iron, ivory);
            BuildGuzheng(root, wood, lightWood, bronze, stringGlow);
            BuildYangqin(root, wood, lightWood, bronze, stringGlow);
            BuildPlayerAndCamera(root, wood);
            BuildLighting(root);
            BuildPostProcessing(root);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.58f, .59f, .58f);
            RenderSettings.ambientEquatorColor = new Color(.34f, .25f, .18f);
            RenderSettings.ambientGroundColor = new Color(.11f, .075f, .050f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.105f, .082f, .068f);
            RenderSettings.fogDensity = .0042f;
        }

        private static void BuildHallSideWall(Transform root, int side, Material wall)
        {
            float x = side * 10f;
            Box("HallSideWallContinuous", root, new Vector3(x, 4.3f, 0f),
                new Vector3(.42f, 8.6f, 25.6f), wall, true);
        }

        private static void BuildStageRearWall(Transform root, Material wall)
        {
            const float rearZ = 12.32f;
            // One continuous header and three overlapping piers produce two exact
            // stage-height openings without the hairline seams of the old five-piece wall.
            Box("RearWallCenterPier", root, new Vector3(0f, 4.30f, rearZ),
                new Vector3(13.96f, 8.60f, .50f), wall, true);
            Box("RearWallWestPier", root, new Vector3(-9.43f, 4.30f, rearZ),
                new Vector3(1.20f, 8.60f, .50f), wall, true);
            Box("RearWallEastPier", root, new Vector3(9.43f, 4.30f, rearZ),
                new Vector3(1.20f, 8.60f, .50f), wall, true);
            Box("RearWallContinuousHeader", root, new Vector3(0f, 6.25f, rearZ),
                new Vector3(20.30f, 4.70f, .54f), wall, true);
        }

        private static void BuildBackdrop(Transform root, Material wood)
        {
            Texture2D texture = Resources.Load<Texture2D>("Art/MeixiTheatreBackdrop");
            Material backdrop = Mat("MeixiPhotoReconstruction", Color.white, .12f);
            backdrop.mainTexture = texture;
            if (backdrop.HasProperty("_BaseMap")) backdrop.SetTexture("_BaseMap", texture);
            if (backdrop.HasProperty("_Cull")) backdrop.SetFloat("_Cull", 0f);
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "MeixiTheatre_DigitalBackdrop";
            quad.transform.SetParent(root, false);
            quad.transform.localPosition = new Vector3(0, 4.66f, 12.10f);
            quad.transform.localRotation = Quaternion.identity;
            // Keep the painted backdrop clear of the two real backstage doors.
            quad.transform.localScale = new Vector3(14.0f, 7.05f, 1);
            quad.GetComponent<Renderer>().sharedMaterial = backdrop;
            DestroyGeneratedComponent(quad.GetComponent<Collider>());
            Box("BackdropTopFrame", root, new Vector3(0, 8.18f, 11.96f), new Vector3(16.4f, .25f, .24f), wood, false);
            Box("BackdropLeftFrame", root, new Vector3(-7.12f, 4.66f, 11.96f), new Vector3(.25f, 7.28f, .24f), wood, false);
            Box("BackdropRightFrame", root, new Vector3(7.12f, 4.66f, 11.96f), new Vector3(.25f, 7.28f, .24f), wood, false);
        }

        private static void BuildArchitecture(Transform root, Material wood, Material red, Material curtain, Material bronze)
        {
            Box("ProsceniumBeam", root, new Vector3(0, 7.82f, 4.82f), new Vector3(20, .38f, .50f), wood, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Cylinder("RedColumn", root, new Vector3(side * 8.25f, 4.15f, 5.12f), new Vector3(.38f, 4.15f, .38f), red, true);
                Box("StageRail", root, new Vector3(side * 7.58f, 1.55f, 7.2f), new Vector3(.16f, 1.25f, 4.2f), wood, false);
            }
            for (int x = -8; x <= 8; x += 4)
            {
                for (int i = -3; i <= 3; i++)
                    Cylinder("CreamValance", root, new Vector3(x + i * .30f, 6.65f, 9.9f), new Vector3(.14f, 1.2f, .14f), curtain, false);
                GameObject lamp = Cylinder("WarmLantern", root, new Vector3(x, 6.9f, 4.8f), new Vector3(.38f, .24f, .38f), bronze, false);
                Light light = lamp.AddComponent<Light>();
                light.type = LightType.Point; light.range = 5; light.intensity = 1.35f; light.color = new Color(1, .50f, .18f);
            }
            for (int i = -4; i <= 4; i++)
                Box("StageGoldInlay", root, new Vector3(i * 1.65f, 1.112f, 8.45f), new Vector3(.018f, .012f, 6.1f), bronze, false);
        }

        private static void BuildWallDecor(Transform root, Material wood, Material red, Material bronze)
        {
            Texture2D frameTexture = LoadChapterTwoWallPoster();
            Material poster = Mat("MeixiTheatreWallDecal", Color.white, .10f);
            poster.mainTexture = frameTexture;
            if (poster.HasProperty("_BaseMap")) poster.SetTexture("_BaseMap", frameTexture);
            if (poster.HasProperty("_Cull")) poster.SetFloat("_Cull", 0f);

            // Framed wall decals and timber battens reproduce the photographed
            // enclosed hall instead of leaving large empty plaster surfaces.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float z = -8.6f + i * 4.7f;
                    GameObject decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    decal.name = "ChapterTwoCampaignMural";
                    decal.transform.SetParent(root, false);
                    decal.transform.localPosition = new Vector3(side * 9.65f, 4.45f, z);
                    decal.transform.localRotation = Quaternion.Euler(0, side < 0 ? 90f : -90f, 0);
                    decal.transform.localScale = new Vector3(3.35f, 1.88f, 1f);
                    decal.GetComponent<Renderer>().sharedMaterial = poster;
                    DestroyGeneratedComponent(decal.GetComponent<Collider>());
                    Box("MuralBacking", root, new Vector3(side * 9.72f, 4.45f, z),
                        new Vector3(.12f, 2.10f, 3.64f), wood, false);
                    Box("MuralTopFrame", root, new Vector3(side * 9.57f, 5.47f, z),
                        new Vector3(.16f, .12f, 3.68f), wood, false);
                    Box("MuralBottomFrame", root, new Vector3(side * 9.57f, 3.43f, z),
                        new Vector3(.16f, .12f, 3.68f), wood, false);
                    Box("MuralLeftFrame", root, new Vector3(side * 9.57f, 4.45f, z - 1.78f),
                        new Vector3(.16f, 2.04f, .12f), wood, false);
                    Box("MuralRightFrame", root, new Vector3(side * 9.57f, 4.45f, z + 1.78f),
                        new Vector3(.16f, 2.04f, .12f), wood, false);
                }
            }

            // Closed entrance, lattice screen and overhead beams.
            for (int x = -1; x <= 1; x += 2)
            {
                Box("EntrancePost", root, new Vector3(x * 1.95f, 2.55f, -13.10f), new Vector3(.28f, 5.1f, .32f), red, true);
                for (int i = 0; i < 4; i++)
                    Box("EntranceLattice", root, new Vector3(x * (2.5f + i * .55f), 3.75f, -13.08f), new Vector3(.10f, 5.8f, .22f), wood, false);
            }
            Box("EntranceBeam", root, new Vector3(0, 5.25f, -13.08f), new Vector3(4.25f, .30f, .34f), wood, false);
            Box("EntrancePlaque", root, new Vector3(0, 6.05f, -13.02f), new Vector3(2.8f, .62f, .18f), bronze, false);

            for (int z = -10; z <= 8; z += 3)
                Box("CeilingCrossBeam", root, new Vector3(0, 8.36f, z), new Vector3(19.2f, .22f, .30f), wood, false);
            for (int z = -9; z <= 6; z += 5)
            {
                Cylinder("CeilingPendant", root, new Vector3(0, 7.52f, z), new Vector3(.30f, .34f, .30f), bronze, false);
                GameObject lampObject = new GameObject("CeilingPendantLight");
                lampObject.transform.SetParent(root, false);
                lampObject.transform.localPosition = new Vector3(0, 7.35f, z);
                Light lamp = lampObject.AddComponent<Light>();
                lamp.type = LightType.Point; lamp.range = 7f; lamp.intensity = 1.8f; lamp.color = new Color(1f, .50f, .18f);
            }
        }

        private static Texture2D LoadChapterTwoWallPoster()
        {
            Texture2D texture = Resources.Load<Texture2D>("Art/ChapterTwoOldSongPoster");
            if (texture != null) return texture;

            string projectOrBuildRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] candidates =
            {
                Path.Combine(projectOrBuildRoot, "MeixiChapterTwoWallPoster.png"),
                Path.Combine(projectOrBuildRoot, "PromoWeb", "public", "media", "poster-chapter-two-old-song.png")
            };
            foreach (string path in candidates)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    Texture2D loaded = new(2, 2, TextureFormat.RGBA32, true)
                    {
                        name = "ChapterTwoOldSongPoster_Runtime",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Trilinear,
                        anisoLevel = 4
                    };
                    if (loaded.LoadImage(File.ReadAllBytes(path), false)) return loaded;
                    Object.Destroy(loaded);
                }
                catch (IOException exception)
                {
                    Debug.LogWarning("[MeixiTheatre] Could not load wall mural: " + exception.Message);
                }
            }
            return Resources.Load<Texture2D>("Art/QuestFrameV2");
        }

        private static void BuildSurfaceDetails(Transform root, Material wood, Material lightWood, Material bronze, Material iron)
        {
            // Thin seams break the floor into believable timber boards without
            // adding colliders or expensive custom shaders.
            Material groove = Mat("TimberSeam", new Color(.055f, .025f, .014f), .08f);
            for (int x = -9; x <= 9; x++)
                Box("HallFloorBoardSeam", root, new Vector3(x + .5f, .018f, -.2f), new Vector3(.018f, .012f, 24.8f), groove, false);
            for (int x = -8; x <= 8; x++)
                Box("StageFloorBoardSeam", root, new Vector3(x + .5f, 1.112f, 8.65f), new Vector3(.014f, .014f, 7.15f), groove, false);

            // Timber wainscot, inset panels and corner posts make the hall feel like
            // a closed historic interior instead of a box with empty walls.
            for (int side = -1; side <= 1; side += 2)
            {
                Box("SideWainscot", root, new Vector3(side * 9.78f, 1.15f, 0f), new Vector3(.18f, 2.3f, 24.6f), wood, false);
                for (int z = -11; z <= 10; z += 3)
                {
                    Box("WainscotPanelInset", root, new Vector3(side * 9.66f, 1.14f, z), new Vector3(.06f, 1.55f, 2.25f), lightWood, false);
                    Box("WainscotVerticalTrim", root, new Vector3(side * 9.60f, 1.18f, z + 1.45f), new Vector3(.10f, 2.12f, .10f), bronze, false);
                }
            }

            // Carved apron approximation: repeated frames, bosses and a continuous
            // lower rail read as traditional joinery from the gameplay camera.
            Box("StageApronLowerRail", root, new Vector3(0, .48f, 4.78f), new Vector3(16.3f, .16f, .16f), iron, true);
            for (int x = -7; x <= 7; x += 2)
            {
                Box("StageApronPanel", root, new Vector3(x, .70f, 4.80f), new Vector3(1.62f, .58f, .12f), lightWood, false);
                Box("StageApronPanelInset", root, new Vector3(x, .70f, 4.72f), new Vector3(1.20f, .30f, .045f), groove, false);
                Sphere("StageApronBronzeBoss", root, new Vector3(x, .70f, 4.65f), new Vector3(.085f, .085f, .045f), bronze, false);
            }

            // Bracket clusters beneath the main beam and handrails around the stage.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int z = 5; z <= 11; z += 2)
                {
                    Box("DougongArmLong", root, new Vector3(side * 8.18f, 6.42f, z), new Vector3(.95f, .16f, .26f), wood, false);
                    GameObject shortArm = Box("DougongArmShort", root, new Vector3(side * 7.92f, 6.18f, z), new Vector3(.62f, .14f, .48f), lightWood, false);
                    shortArm.transform.localRotation = Quaternion.Euler(0, side * 7f, side * 8f);
                }
                for (int z = 5; z <= 10; z++)
                {
                    Cylinder("StageRailPost", root, new Vector3(side * 7.58f, 1.78f, z), new Vector3(.08f, .68f, .08f), wood, true);
                    Box("StageRailTop", root, new Vector3(side * 7.58f, 2.43f, z + .5f), new Vector3(.16f, .13f, 1.02f), bronze, true);
                }
            }

            // Two low stair newels visibly anchor the central stair and provide
            // accurate collision at its open sides.
            for (int side = -1; side <= 1; side += 2)
            {
                Box("CentralStairSide", root, new Vector3(side * 2.48f, .58f, 4.26f), new Vector3(.22f, 1.16f, 1.55f), wood, true);
                Sphere("CentralStairFinial", root, new Vector3(side * 2.48f, 1.28f, 3.73f), new Vector3(.23f, .23f, .23f), bronze, false);
            }
        }

        private static void BuildAudience(Transform root, Material dark, Material wood)
        {
            Material cushion = Mat("TheatreChairCushion", new Color(.40f, .060f, .040f), .24f);
            Material brass = Mat("ChairBrass", new Color(.40f, .25f, .075f), .52f, .72f);
            for (int row = 0; row < 5; row++)
            for (int col = -3; col <= 3; col++)
            {
                if (col == 0) continue; // preserve a clear central aisle from the reference hall
                Vector3 p = new Vector3(col * 2.18f, .55f, -1.2f - row * 2.15f);
                GameObject chair = new GameObject($"AudienceChair_R{row}_C{col}");
                chair.transform.SetParent(root, false);
                chair.transform.localPosition = p;
                BoxCollider chairCollider = chair.AddComponent<BoxCollider>();
                chairCollider.center = new Vector3(0, .42f, -.10f);
                chairCollider.size = new Vector3(1.02f, 1.48f, .88f);

                Box("ChairSeatFrame", chair.transform, new Vector3(0, .03f, 0), new Vector3(.92f, .16f, .78f), wood, false);
                Box("ChairSeatCushion", chair.transform, new Vector3(0, .15f, -.02f), new Vector3(.78f, .14f, .66f), cushion, false);
                Box("ChairBackOuter", chair.transform, new Vector3(0, .62f, -.34f), new Vector3(.94f, .96f, .10f), dark, false);
                Box("ChairBackInner", chair.transform, new Vector3(0, .63f, -.265f), new Vector3(.70f, .66f, .055f), cushion, false);
                Box("ChairBackTopRail", chair.transform, new Vector3(0, 1.12f, -.33f), new Vector3(1.02f, .12f, .14f), wood, false);
                for (int lx = -1; lx <= 1; lx += 2)
                for (int lz = -1; lz <= 1; lz += 2)
                    Cylinder("ChairLeg", chair.transform, new Vector3(lx * .38f, -.28f, lz * .28f), new Vector3(.045f, .30f, .045f), wood, false);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("ChairArm", chair.transform, new Vector3(side * .51f, .48f, -.02f), new Vector3(.10f, .10f, .72f), wood, false);
                    Cylinder("ChairArmPost", chair.transform, new Vector3(side * .51f, .20f, .22f), new Vector3(.038f, .28f, .038f), brass, false);
                }
                if (row % 2 == 0)
                {
                    Cylinder("TeaTableTop", root, p + new Vector3(.92f, .37f, -.1f), new Vector3(.40f, .045f, .40f), wood, false);
                    Cylinder("TeaTablePost", root, p + new Vector3(.92f, .17f, -.1f), new Vector3(.045f, .20f, .045f), dark, false);
                }
            }
        }

        private static void BuildExplorationRooms(Transform root, Material plaster, Material wood,
            Material lightWood, Material floor, Material bronze, Material iron, Material paper,
            Material porcelain, Material fadedBlue, Material costume, Material curtain)
        {
            Transform rooms = new GameObject("TheatreExplorationRooms").transform;
            rooms.SetParent(root, false);
            Material mirror = Mat("DressingMirror", new Color(.22f, .31f, .31f), .74f, .32f);
            Material darkFabric = Mat("BackstageBlackDrape", new Color(.035f, .030f, .028f), .08f);
            Texture2D diary = Resources.Load<Texture2D>("Art/MeixiDiaryPaperLandscapeV1");
            if (diary != null)
            {
                paper.mainTexture = diary;
                if (paper.HasProperty("_BaseMap")) paper.SetTexture("_BaseMap", diary);
            }

            // Side wings are at audience-floor height; the rear crossover is level with the stage.
            Box("WestWingFloor", rooms, new Vector3(-13f, -.10f, 0f), new Vector3(6.40f, .24f, 26.20f), floor, true);
            Box("EastWingFloor", rooms, new Vector3(13f, -.10f, 0f), new Vector3(6.40f, .24f, 26.20f), floor, true);
            Box("BackstageFloor", rooms, new Vector3(0f, 1.0f, 16.15f), new Vector3(32.40f, .24f, 8.10f), floor, true);
            Box("WestWingCeiling", rooms, new Vector3(-13f, 4.08f, 0f), new Vector3(6.40f, .26f, 26.20f), plaster, true);
            Box("EastWingCeiling", rooms, new Vector3(13f, 4.08f, 0f), new Vector3(6.40f, .26f, 26.20f), plaster, true);
            Box("BackstageCeiling", rooms, new Vector3(0f, 5.45f, 16.15f), new Vector3(32.40f, .28f, 8.10f), darkFabric, true);
            // The wing and crossover ceilings meet at different heights. This
            // overlap panel closes the exposed seam at the backstage threshold
            // and extends past every trim edge so the camera cannot see outside.
            Box("BackstageCeilingOverlapSeal", rooms, new Vector3(0f, 5.60f, 16.15f),
                new Vector3(33.05f, .38f, 9.05f), darkFabric, true);
            Box("WestOuterWall", rooms, new Vector3(-16f, 2.05f, 3.55f), new Vector3(.46f, 4.30f, 33.70f), plaster, true);
            Box("EastOuterWall", rooms, new Vector3(16f, 2.05f, 3.55f), new Vector3(.46f, 4.30f, 33.70f), plaster, true);
            Box("WingFrontWall", rooms, new Vector3(0f, 2.05f, -13.05f), new Vector3(32.40f, 4.30f, .46f), plaster, true);
            Box("BackstageRearWall", rooms, new Vector3(0f, 3.10f, 20.05f), new Vector3(32.40f, 6.20f, .50f), plaster, true);
            Box("BackstageWestWallSeal", rooms, new Vector3(-16f, 2.78f, 16.15f), new Vector3(.50f, 5.76f, 8.10f), plaster, true);
            Box("BackstageEastWallSeal", rooms, new Vector3(16f, 2.78f, 16.15f), new Vector3(.50f, 5.76f, 8.10f), plaster, true);
            Box("BackstageCeilingWestTrim", rooms, new Vector3(-15.78f, 5.27f, 16.25f), new Vector3(.18f, .28f, 7.45f), wood, false);
            Box("BackstageCeilingEastTrim", rooms, new Vector3(15.78f, 5.27f, 16.25f), new Vector3(.18f, .28f, 7.45f), wood, false);
            Box("BackstageCeilingFrontTrim", rooms, new Vector3(0f, 5.27f, 12.56f),
                new Vector3(31.70f, .28f, .20f), wood, false);
            Box("BackstageCeilingRearTrim", rooms, new Vector3(0f, 5.27f, 19.92f),
                new Vector3(31.70f, .28f, .20f), wood, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("WingCeilingInnerTrim", rooms, new Vector3(side * 10.22f, 3.96f, 0f),
                    new Vector3(.22f, .26f, 25.45f), wood, false);
                Box("WingCeilingOuterTrim", rooms, new Vector3(side * 15.78f, 3.96f, 0f),
                    new Vector3(.18f, .26f, 25.45f), wood, false);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                BuildDoorFrame(rooms, new Vector3(side * 7.90f, 2.45f, 12.18f), 0f, wood, bronze);
                float hingeX = side < 0 ? -8.52f : 8.52f;
                BuildSwingDoor(rooms, "BackstageSwingDoor", new Vector3(hingeX, 1.10f, 12.20f),
                    0f, side < 0 ? 1f : -1f, side < 0 ? -98f : 98f, wood, bronze);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                BuildWingPartition(rooms, side, -4.55f, plaster, wood, bronze);
                BuildWingPartition(rooms, side, 4.55f, plaster, wood, bronze);
                for (int i = 0; i < 5; i++)
                {
                    float height = .22f * (i + 1);
                    Box("WingToBackstageStep", rooms,
                        new Vector3(side * 11.25f, height * .5f, 10.82f + i * .31f),
                        new Vector3(1.65f, height, .42f), floor, true);
                    Box("WingStairNosing", rooms,
                        new Vector3(side * 11.25f, height + .025f, 10.63f + i * .31f),
                        new Vector3(1.72f, .05f, .08f), bronze, false);
                }
                Box("WingStairOuterSkirt", rooms,
                    new Vector3(side * 11.25f - .86f, .55f, 11.45f),
                    new Vector3(.12f, 1.10f, 1.82f), wood, true);
                Box("WingStairInnerSkirt", rooms,
                    new Vector3(side * 11.25f + .86f, .55f, 11.45f),
                    new Vector3(.12f, 1.10f, 1.82f), wood, true);
                Box("WingStairTopLanding", rooms, new Vector3(side * 11.25f, 1.0f, 12.52f),
                    new Vector3(1.80f, .20f, .72f), floor, true);
                for (int z = -9; z <= 9; z += 6)
                    CreateRoomLight(rooms, new Vector3(side * 13f, 3.45f, z), new Color(1f, .72f, .50f), 2.0f, 6.2f);
            }

            BuildDressingRoom(rooms, -1, wood, lightWood, bronze, mirror, porcelain, costume, curtain);
            BuildPropRoom(rooms, -1, wood, bronze, iron, costume, paper);
            BuildArchiveRoom(rooms, -1, wood, lightWood, bronze, paper, porcelain);
            BuildGreenRoom(rooms, wood, lightWood, bronze, porcelain, costume);
            BuildWorkshop(rooms, wood, lightWood, bronze, iron, paper);
            BuildRehearsalRoom(rooms, wood, bronze, fadedBlue, costume, curtain);
            BuildBackstageCrossover(rooms, wood, lightWood, bronze, iron, paper, costume, darkFabric);
            BuildInteractiveSetDressing(rooms, wood, lightWood, bronze, iron, paper,
                porcelain, fadedBlue, costume, darkFabric);
        }

        private static void BuildInteractiveSetDressing(Transform root, Material wood, Material lightWood,
            Material bronze, Material iron, Material paper, Material porcelain, Material fadedBlue,
            Material costume, Material darkFabric)
        {
            // Vanity drawer: a shallow, physically separate drawer avoids the old
            // "painted-on furniture" look and gives the dressing room a useful touch point.
            GameObject drawerProp = SetPropRoot("化妆台抽屉_可互动", root, new Vector3(-14.10f, 0f, -9.55f));
            Transform drawer = new GameObject("化妆台活动抽屉").transform;
            drawer.SetParent(drawerProp.transform, false);
            Box("抽屉木盒", drawer, new Vector3(0f, .62f, .14f), new Vector3(1.30f, .24f, .52f), wood, true);
            Box("抽屉面板", drawer, new Vector3(0f, .62f, .43f), new Vector3(1.44f, .31f, .08f), lightWood, false);
            Box("抽屉内腔", drawer, new Vector3(0f, .72f, .14f), new Vector3(1.08f, .025f, .34f), darkFabric, false);
            Cylinder("抽屉铜拉手", drawer, new Vector3(0f, .62f, .50f), new Vector3(.10f, .035f, .10f), bronze, false)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            drawerProp.AddComponent<ChapterTwoSetProp>().ConfigureSlide(drawer, new Vector3(0f, 0f, .48f),
                "拉开化妆台抽屉", "抽屉里只有干涸的胭脂和一枚写着‘守场’的旧铜扣",
                "你把化妆台抽屉轻轻推回原位");

            // A banded costume trunk with a genuine hinged lid and recessed lining.
            GameObject trunk = SetPropRoot("戏箱_可互动", root, new Vector3(-12.70f, 0f, 1.25f));
            Box("戏箱箱体", trunk.transform, new Vector3(0f, .43f, 0f), new Vector3(1.62f, .72f, .92f), wood, true);
            Box("戏箱内衬", trunk.transform, new Vector3(0f, .77f, 0f), new Vector3(1.38f, .035f, .70f), costume, false);
            for (int band = -1; band <= 1; band += 2)
                Box("戏箱铜箍", trunk.transform, new Vector3(band * .58f, .44f, -.47f),
                    new Vector3(.11f, .78f, .045f), bronze, false);
            for (int foot = -1; foot <= 1; foot += 2)
                Box("戏箱足", trunk.transform, new Vector3(foot * .57f, .08f, 0f),
                    new Vector3(.18f, .16f, .72f), iron, false);
            Transform lid = new GameObject("戏箱活动箱盖").transform;
            lid.SetParent(trunk.transform, false);
            lid.localPosition = new Vector3(0f, .82f, .43f);
            Box("戏箱箱盖", lid, new Vector3(0f, 0f, -.43f), new Vector3(1.66f, .18f, .90f), wood, false);
            Box("戏箱盖内衬", lid, new Vector3(0f, -.105f, -.43f), new Vector3(1.40f, .025f, .68f), costume, false);
            // The lid hinge is along its back edge; positive X rotation lifts
            // the front edge upward instead of driving it through the trunk.
            trunk.AddComponent<ChapterTwoSetProp>().ConfigureHinge(lid, new Vector3(76f, 0f, 0f),
                "打开旧戏箱", "旧戏服下面压着一张褪色合影：台上本来站着三个人",
                "沉重的戏箱盖重新合上");

            // A rotating mask is small, readable and unsettling without becoming a quest spoiler.
            GameObject maskStand = SetPropRoot("旧面具台_可互动", root, new Vector3(-12.35f, 0f, 3.25f), -12f);
            Box("面具台底座", maskStand.transform, new Vector3(0f, .16f, 0f), new Vector3(.70f, .30f, .58f), wood, true);
            Cylinder("面具台立柱", maskStand.transform, new Vector3(0f, .91f, 0f), new Vector3(.055f, .72f, .055f), bronze, false);
            Transform maskPivot = new GameObject("活动面具").transform;
            maskPivot.SetParent(maskStand.transform, false);
            maskPivot.localPosition = new Vector3(0f, 1.62f, 0f);
            Sphere("旧木面具", maskPivot, new Vector3(0f, 0f, 0f), new Vector3(.29f, .38f, .11f), paper, false);
            Sphere("旧木面具暗半", maskPivot, new Vector3(.13f, 0f, -.045f), new Vector3(.16f, .35f, .10f), darkFabric, false);
            Sphere("面具左眼孔", maskPivot, new Vector3(-.10f, .07f, -.105f), new Vector3(.06f, .07f, .025f), iron, false);
            Sphere("面具右眼孔", maskPivot, new Vector3(.10f, .05f, -.108f), new Vector3(.065f, .075f, .025f), iron, false);
            maskStand.AddComponent<ChapterTwoSetProp>().ConfigureTurn(maskPivot, new Vector3(0f, 172f, 0f),
                "转动旧面具", "面具背后刻着：‘看见我的时候，不要移开眼睛’",
                "旧面具缓慢转回正面", ChapterTwoSetProp.SoundKind.Wood);

            // Gramophone cabinet, rotating record and a profiled horn. It is intentionally
            // acoustic only; no extra real-time light or per-frame allocations are added.
            GameObject gramophone = SetPropRoot("候场留声机_可互动", root, new Vector3(14.20f, 0f, -7.55f), 8f);
            Box("留声机木柜", gramophone.transform, new Vector3(0f, .48f, 0f), new Vector3(1.20f, .86f, .82f), wood, true);
            Box("留声机柜面", gramophone.transform, new Vector3(0f, .93f, 0f), new Vector3(1.28f, .09f, .90f), lightWood, false);
            Transform record = new GameObject("活动唱片").transform;
            record.SetParent(gramophone.transform, false);
            record.localPosition = new Vector3(-.16f, 1.01f, -.02f);
            Cylinder("旧唱片", record, Vector3.zero, new Vector3(.34f, .018f, .34f), iron, false);
            Cylinder("唱片纸标", record, new Vector3(0f, .022f, 0f), new Vector3(.09f, .009f, .09f), paper, false);
            CylinderBetween("唱臂", gramophone.transform, new Vector3(.34f, 1.04f, -.26f),
                new Vector3(.05f, 1.13f, -.07f), .022f, bronze);
            CreateFlaredHorn("留声机铜喇叭", gramophone.transform, new Vector3(.25f, 1.48f, .12f),
                new Vector3(-18f, -20f, 0f), .92f, .075f, .42f, bronze);
            gramophone.AddComponent<ChapterTwoSetProp>().ConfigureTurn(record, new Vector3(0f, 165f, 0f),
                "拨动留声机", "唱针划过空白沟槽，只传出一段像倒放脚步的沙响",
                "你按住唱片，候场室重新安静下来", ChapterTwoSetProp.SoundKind.Metal);

            // The archive book has a separately hinged cover; interaction remains optional lore.
            GameObject ledger = SetPropRoot("守场账簿_可互动", root, new Vector3(-13.65f, 0f, 7.95f), -7f);
            Box("账簿页芯", ledger.transform, new Vector3(0f, .91f, 0f), new Vector3(.92f, .055f, .66f), paper, true);
            Box("账簿书脊", ledger.transform, new Vector3(-.48f, .94f, 0f), new Vector3(.08f, .10f, .69f), bronze, false);
            Transform cover = new GameObject("账簿活动封面").transform;
            cover.SetParent(ledger.transform, false);
            cover.localPosition = new Vector3(-.47f, .96f, 0f);
            Box("账簿封皮", cover, new Vector3(.47f, 0f, 0f), new Vector3(.96f, .045f, .70f), fadedBlue, false);
            // The cover pivots around its left spine. Positive Z rotation raises
            // the free edge and keeps the cover clear of the page block.
            ledger.AddComponent<ChapterTwoSetProp>().ConfigureHinge(cover, new Vector3(0f, 0f, 138f),
                "翻开守场账簿", "末页反复登记着同一个名字，日期却跨了四十七年",
                "你合上账簿，纸页仍在里面轻轻摩擦", ChapterTwoSetProp.SoundKind.Cloth);

            // A usable scenery vise gives the workshop something mechanical to play with.
            GameObject vise = SetPropRoot("布景台木工钳_可互动", root, new Vector3(13.35f, 0f, -.55f), 90f);
            Box("木工钳底座", vise.transform, new Vector3(0f, .98f, 0f), new Vector3(.72f, .12f, .48f), iron, true);
            Box("木工钳固定颚", vise.transform, new Vector3(-.22f, 1.18f, 0f), new Vector3(.15f, .38f, .56f), iron, false);
            Transform movingJaw = new GameObject("木工钳活动颚").transform;
            movingJaw.SetParent(vise.transform, false);
            Box("木工钳滑动颚", movingJaw, new Vector3(.22f, 1.18f, 0f), new Vector3(.15f, .38f, .56f), iron, false);
            CylinderBetween("木工钳丝杆", movingJaw, new Vector3(.22f, 1.0f, -.38f),
                new Vector3(.22f, 1.0f, .38f), .025f, bronze);
            vise.AddComponent<ChapterTwoSetProp>().ConfigureSlide(movingJaw, new Vector3(.30f, 0f, 0f),
                "转动木工钳", "丝杆还能顺畅转动，钳口里夹着新鲜的木屑",
                "木工钳重新咬合", ChapterTwoSetProp.SoundKind.Metal);

            // A fly-system hand weight moves vertically but is kept short so its
            // interaction bounds stay at eye level instead of spanning the whole rigging.
            GameObject flyControl = SetPropRoot("后台吊杆配重_可互动", root, new Vector3(4.15f, 0f, 18.35f));
            Box("吊杆配重架", flyControl.transform, new Vector3(0f, 1.70f, 0f), new Vector3(.52f, 2.55f, .28f), iron, true);
            Transform flyWeight = new GameObject("活动配重").transform;
            flyWeight.SetParent(flyControl.transform, false);
            Box("吊杆铁配重", flyWeight, new Vector3(0f, 1.25f, -.20f), new Vector3(.36f, .58f, .24f), iron, false);
            Cylinder("吊杆拉环", flyWeight, new Vector3(0f, .88f, -.20f), new Vector3(.14f, .035f, .14f), bronze, false)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            flyControl.AddComponent<ChapterTwoSetProp>().ConfigureSlide(flyWeight, new Vector3(0f, .88f, 0f),
                "拉动舞台吊杆配重", "幕布上方传来沉闷滑轮声，落灰像有人刚从横梁走过",
                "配重缓缓落回限位扣", ChapterTwoSetProp.SoundKind.Metal);
        }

        private static void BuildWingPartition(Transform root, int side, float z, Material plaster,
            Material wood, Material bronze)
        {
            float outerCenter = side * 14.05f;
            Box("WingRoomPartition", root, new Vector3(outerCenter, 2.05f, z),
                new Vector3(3.90f, 4.30f, .24f), plaster, true);
            Box("WingRoomDoorLintel", root, new Vector3(side * 11.05f, 3.35f, z),
                new Vector3(2.10f, 1.70f, .24f), plaster, true);
            Box("WingRoomDoorPost", root, new Vector3(side * 12.05f, 1.35f, z),
                new Vector3(.16f, 2.70f, .34f), wood, true);
            Box("WingRoomDoorCap", root, new Vector3(side * 11.05f, 2.68f, z),
                new Vector3(2.10f, .14f, .38f), bronze, false);
        }

        private static void BuildDoorFrame(Transform root, Vector3 center, float yaw, Material wood, Material bronze)
        {
            GameObject frame = new GameObject("TraditionalDoorFrame");
            frame.transform.SetParent(root, false);
            frame.transform.localPosition = center;
            frame.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box("DoorPostLeft", frame.transform, new Vector3(-.72f, 0f, 0f), new Vector3(.14f, 2.70f, .18f), wood, false);
            Box("DoorPostRight", frame.transform, new Vector3(.72f, 0f, 0f), new Vector3(.14f, 2.70f, .18f), wood, false);
            Box("DoorHeader", frame.transform, new Vector3(0f, 1.35f, 0f), new Vector3(1.58f, .16f, .20f), bronze, false);
        }

        private static void BuildSwingDoor(Transform root, string label, Vector3 hinge, float yaw,
            float panelDirection, float openYaw, Material wood, Material bronze)
        {
            GameObject door = new GameObject(label);
            door.transform.SetParent(root, false);
            door.transform.localPosition = hinge;
            door.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            GameObject panel = Box("DoorPanel", door.transform,
                new Vector3(panelDirection * .62f, 1.35f, 0f), new Vector3(1.24f, 2.70f, .12f), wood, true);
            Box("DoorInset", panel.transform, new Vector3(0f, 0f, -.56f),
                new Vector3(.78f, .76f, .05f), bronze, false);
            Box("DoorTopRail", panel.transform, new Vector3(0f, .38f, -.56f),
                new Vector3(.90f, .06f, .05f), bronze, false);
            Sphere("DoorHandle", panel.transform, new Vector3(panelDirection * .34f, 0f, -.62f),
                new Vector3(.075f, .075f, .075f), bronze, false);
            TheatreSwingDoor swing = door.AddComponent<TheatreSwingDoor>();
            swing.Configure(openYaw);
        }

        private static void BuildDressingRoom(Transform root, int side, Material wood, Material lightWood,
            Material bronze, Material mirror, Material porcelain, Material costume, Material curtain)
        {
            float x = side * 14.1f;
            Box("DressingVanity", root, new Vector3(x, .78f, -9.55f), new Vector3(2.55f, .12f, .72f), lightWood, true);
            for (int leg = -1; leg <= 1; leg += 2)
                Box("VanityLeg", root, new Vector3(x + leg * .95f, .39f, -9.55f), new Vector3(.12f, .78f, .56f), wood, false);
            Box("VanityMirror", root, new Vector3(x, 2.05f, -9.92f), new Vector3(2.10f, 1.85f, .08f), mirror, false);
            Box("VanityMirrorFrameTop", root, new Vector3(x, 3.02f, -9.94f), new Vector3(2.34f, .12f, .12f), bronze, false);
            for (int lamp = -2; lamp <= 2; lamp++)
                Sphere("VanityBulb", root, new Vector3(x + lamp * .48f, 3.0f, -9.82f), new Vector3(.075f, .075f, .075f), porcelain, false);
            Cylinder("DressingStool", root, new Vector3(x, .38f, -8.60f), new Vector3(.42f, .09f, .42f), costume, true);
            Cylinder("DressingStoolPost", root, new Vector3(x, .19f, -8.60f), new Vector3(.07f, .19f, .07f), bronze, false);
            for (int item = -2; item <= 2; item++)
            {
                Cylinder("MakeupJar", root, new Vector3(x + item * .31f, .90f, -9.45f),
                    new Vector3(.07f + (item & 1) * .015f, .08f, .07f + (item & 1) * .015f), porcelain, false);
            }
            BuildCostumeRack(root, new Vector3(side * 13.9f, 0f, -6.35f), wood, bronze, costume, curtain);
        }

        private static void BuildPropRoom(Transform root, int side, Material wood, Material bronze,
            Material iron, Material costume, Material paper)
        {
            float x = side * 14.25f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = new Vector3(x + (i % 2) * .78f, .36f + (i / 2) * .66f, -2.4f + (i % 2) * .72f);
                Box("PropCrate", root, p, new Vector3(.72f, .62f, .70f), wood, i < 2);
                Box("PropCrateSlat", root, p + new Vector3(0f, .02f, -.37f), new Vector3(.62f, .10f, .04f), bronze, false);
            }
            Box("MaskDisplayBoard", root, new Vector3(x, 2.25f, 2.65f), new Vector3(2.45f, 1.45f, .10f), costume, false);
            for (int i = 0; i < 5; i++)
            {
                Sphere("OperaMask", root, new Vector3(x - .82f + (i % 3) * .82f, 2.55f - (i / 3) * .62f, 2.56f),
                    new Vector3(.28f, .34f, .08f), i % 2 == 0 ? paper : bronze, false);
            }
            for (int i = 0; i < 4; i++)
                CylinderBetween("PropSpear", root, new Vector3(x - 1.2f + i * .30f, .18f, 3.25f),
                    new Vector3(x - 1.0f + i * .30f, 2.85f, 3.25f), .025f, iron);
            Cylinder("CoiledStageRope", root, new Vector3(x + 1.05f, .22f, 3.25f), new Vector3(.38f, .12f, .38f), paper, false);
        }

        private static void BuildArchiveRoom(Transform root, int side, Material wood, Material lightWood,
            Material bronze, Material paper, Material porcelain)
        {
            float x = side * 14.05f;
            for (int shelf = 0; shelf < 2; shelf++)
            {
                float z = 6.15f + shelf * 4.3f;
                Box("ArchiveBookcase", root, new Vector3(side * 15.35f, 1.55f, z), new Vector3(.55f, 3.05f, 3.25f), wood, true);
                for (int level = 0; level < 4; level++)
                    Box("ArchiveShelf", root, new Vector3(side * 15.02f, .35f + level * .78f, z),
                        new Vector3(.12f, .08f, 3.02f), bronze, false);
                for (int roll = 0; roll < 10; roll++)
                    Cylinder("ScriptRoll", root, new Vector3(side * 14.90f, .56f + (roll % 4) * .76f, z - 1.25f + (roll / 4) * .72f),
                        new Vector3(.055f, .38f, .055f), paper, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            Box("ArchiveDesk", root, new Vector3(x, .80f, 7.75f), new Vector3(2.40f, .14f, 1.18f), lightWood, true);
            Box("ArchiveDeskPedestal", root, new Vector3(x - side * .88f, .40f, 7.75f), new Vector3(.42f, .80f, .94f), wood, false);
            Box("OpenStageLedger", root, new Vector3(x, .90f, 7.70f), new Vector3(1.18f, .035f, .82f), paper, false).transform.localRotation = Quaternion.Euler(0f, -8f, 0f);
            Cylinder("ArchiveInkStone", root, new Vector3(x + side * .72f, .91f, 7.55f), new Vector3(.18f, .035f, .18f), porcelain, false);
            CylinderBetween("ArchiveBrush", root, new Vector3(x + side * .45f, .94f, 7.28f),
                new Vector3(x + side * 1.03f, .94f, 7.48f), .018f, bronze);
        }

        private static void BuildGreenRoom(Transform root, Material wood, Material lightWood,
            Material bronze, Material porcelain, Material costume)
        {
            Box("GreenRoomBench", root, new Vector3(13.75f, .58f, -9.55f), new Vector3(3.45f, .28f, .72f), costume, true);
            Box("GreenRoomBenchBack", root, new Vector3(13.75f, 1.22f, -9.87f), new Vector3(3.45f, 1.08f, .14f), wood, false);
            Cylinder("GreenRoomTeaTable", root, new Vector3(12.65f, .46f, -7.65f), new Vector3(.68f, .08f, .68f), lightWood, true);
            Cylinder("GreenRoomTeaTablePost", root, new Vector3(12.65f, .23f, -7.65f), new Vector3(.10f, .23f, .10f), bronze, false);
            GameObject teaSet = SetPropRoot("候场茶壶_可互动", root, new Vector3(12.65f, .64f, -7.65f), -12f);
            Transform teaPot = new GameObject("活动旧茶壶").transform;
            teaPot.SetParent(teaSet.transform, false);
            Sphere("茶壶圆腹", teaPot, Vector3.zero, new Vector3(.30f, .25f, .30f), porcelain, false);
            Cylinder("茶壶盖", teaPot, new Vector3(0f, .15f, 0f), new Vector3(.12f, .025f, .12f), porcelain, false);
            Sphere("茶壶盖钮", teaPot, new Vector3(0f, .205f, 0f), new Vector3(.055f, .055f, .055f), bronze, false);
            CylinderBetween("茶壶流", teaPot, new Vector3(.12f, .04f, 0f), new Vector3(.38f, .15f, 0f), .045f, porcelain);
            CylinderBetween("茶壶提梁左", teaPot, new Vector3(-.12f, .06f, -.12f), new Vector3(-.25f, .27f, 0f), .024f, bronze);
            CylinderBetween("茶壶提梁右", teaPot, new Vector3(-.25f, .27f, 0f), new Vector3(-.12f, .06f, .12f), .024f, bronze);
            teaSet.AddComponent<ChapterTwoSetProp>().ConfigureTurn(teaPot, new Vector3(0f, 0f, -34f),
                "提起候场茶壶", "壶底还温着，茶水却已经干了很久",
                "你把旧茶壶放回桌面", ChapterTwoSetProp.SoundKind.Metal);
            for (int cup = 0; cup < 3; cup++)
                Cylinder("TeaCup", root, new Vector3(12.20f + cup * .42f, .58f, -7.25f), new Vector3(.09f, .09f, .09f), porcelain, false);
            Box("GreenRoomCallBoard", root, new Vector3(15.76f, 2.15f, -6.55f), new Vector3(.08f, 1.35f, 2.45f), lightWood, false);
        }

        private static void BuildWorkshop(Transform root, Material wood, Material lightWood,
            Material bronze, Material iron, Material paper)
        {
            Box("WorkshopBench", root, new Vector3(13.95f, .82f, -.60f), new Vector3(3.35f, .18f, 1.10f), lightWood, true);
            for (int leg = -1; leg <= 1; leg += 2)
                Box("WorkshopBenchLeg", root, new Vector3(13.95f + leg * 1.35f, .41f, -.60f), new Vector3(.18f, .82f, .84f), wood, false);
            for (int tool = 0; tool < 6; tool++)
                CylinderBetween("HandTool", root, new Vector3(12.75f + tool * .46f, .96f, -.75f),
                    new Vector3(12.95f + tool * .46f, 1.18f, -.28f), .025f, tool % 2 == 0 ? iron : bronze);
            for (int board = 0; board < 5; board++)
                Box("SceneryTimber", root, new Vector3(12.25f + board * .22f, .25f + board * .18f, 2.55f),
                    new Vector3(.16f, .16f, 3.05f), wood, board < 2);
            Box("WorkshopSceneryPattern", root, new Vector3(15.76f, 2.28f, -2.10f), new Vector3(.08f, 1.25f, 1.75f), paper, false);
        }

        private static void BuildRehearsalRoom(Transform root, Material wood, Material bronze,
            Material fadedBlue, Material costume, Material curtain)
        {
            for (int strip = 0; strip < 5; strip++)
                Box("RehearsalFloorMark", root, new Vector3(12.15f + strip * .72f, .012f, 7.65f),
                    new Vector3(.025f, .018f, 5.10f), bronze, false);
            Box("RehearsalScreen", root, new Vector3(14.85f, 1.48f, 10.65f), new Vector3(2.10f, 2.90f, .16f), fadedBlue, true);
            for (int panel = -1; panel <= 1; panel++)
                Box("RehearsalScreenFrame", root, new Vector3(14.85f + panel * .70f, 1.48f, 10.54f),
                    new Vector3(.08f, 2.80f, .08f), wood, false);
            BuildCostumeRack(root, new Vector3(12.15f, 0f, 10.15f), wood, bronze, costume, curtain);
            GameObject handDrum = SetPropRoot("排练手鼓_可互动", root, new Vector3(12.20f, .58f, 6.45f));
            GameObject drumBody = Cylinder("排练手鼓鼓身", handDrum.transform, Vector3.zero,
                new Vector3(.42f, .22f, .42f), costume, true);
            drumBody.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject drumSkin = Cylinder("手鼓皮面", handDrum.transform, new Vector3(0f, 0f, -.46f),
                new Vector3(.40f, .025f, .40f), fadedBlue, false);
            drumSkin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int stud = 0; stud < 10; stud++)
            {
                float angle = stud * Mathf.PI * 2f / 10f;
                Sphere("手鼓铜钉", handDrum.transform,
                    new Vector3(Mathf.Cos(angle) * .34f, Mathf.Sin(angle) * .34f, -.49f),
                    new Vector3(.028f, .028f, .018f), bronze, false);
            }
            handDrum.AddComponent<ChapterTwoSetProp>().ConfigurePulse(drumSkin.transform,
                "轻敲排练手鼓", "鼓皮发出短促的闷响，后台深处隔了半拍回了一声",
                ChapterTwoSetProp.SoundKind.Drum);
        }

        private static void BuildBackstageCrossover(Transform root, Material wood, Material lightWood,
            Material bronze, Material iron, Material paper, Material costume, Material darkFabric)
        {
            for (int x = -13; x <= 13; x += 2)
            {
                Cylinder("FlySystemRope", root, new Vector3(x, 3.18f, 18.85f), new Vector3(.025f, 2.05f, .025f), paper, false);
                if ((x & 3) == 1 || (x & 3) == -1)
                    Cylinder("FlySystemWeight", root, new Vector3(x, 1.62f, 18.85f), new Vector3(.16f, .36f, .16f), iron, false);
            }
            Box("BackstageFlyRail", root, new Vector3(0f, 4.95f, 18.85f), new Vector3(29.5f, .18f, .24f), iron, false);
            for (int i = 0; i < 5; i++)
            {
                float x = -10.5f + i * 5.25f;
                Box("SceneryFlat", root, new Vector3(x, 2.70f, 19.55f), new Vector3(3.55f, 3.45f, .12f),
                    i % 2 == 0 ? darkFabric : costume, false);
                Box("SceneryFlatFrame", root, new Vector3(x, 4.42f, 19.48f), new Vector3(3.70f, .10f, .10f), wood, false);
            }
            for (int i = 0; i < 4; i++)
            {
                float x = -12f + i * 8f;
                Box("CostumeTrunk", root, new Vector3(x, 1.48f, 14.60f), new Vector3(1.60f, .72f, .88f), wood, true);
                Box("CostumeTrunkBand", root, new Vector3(x, 1.50f, 14.14f), new Vector3(1.68f, .16f, .05f), bronze, false);
            }
            Box("BackstagePromptTable", root, new Vector3(0f, 1.62f, 16.20f), new Vector3(2.45f, .16f, 1.20f), lightWood, true);
            Box("BackstagePromptBook", root, new Vector3(-.32f, 1.74f, 16.15f), new Vector3(1.16f, .035f, .82f), paper, false);
            CreateRoomLight(root, new Vector3(-7.8f, 4.75f, 15.6f), new Color(1f, .47f, .22f), 3.0f, 8.5f);
            CreateRoomLight(root, new Vector3(0f, 4.75f, 15.6f), new Color(.78f, .50f, .30f), 2.5f, 8.5f);
            CreateRoomLight(root, new Vector3(7.8f, 4.75f, 15.6f), new Color(1f, .47f, .22f), 3.0f, 8.5f);
        }

        private static void BuildCostumeRack(Transform root, Vector3 origin, Material wood,
            Material bronze, Material costume, Material curtain)
        {
            for (int side = -1; side <= 1; side += 2)
                Cylinder("CostumeRackPost", root, origin + new Vector3(side * 1.25f, 1.18f, 0f),
                    new Vector3(.055f, 1.18f, .055f), bronze, false);
            CylinderBetween("CostumeRackRail", root, origin + new Vector3(-1.25f, 2.32f, 0f),
                origin + new Vector3(1.25f, 2.32f, 0f), .055f, bronze);
            Box("CostumeRackFoot", root, origin + new Vector3(0f, .08f, 0f), new Vector3(2.85f, .16f, .72f), wood, true);
            for (int i = 0; i < 6; i++)
            {
                Material cloth = i % 2 == 0 ? costume : curtain;
                GameObject garment = Box("HangingCostume", root, origin + new Vector3(-1.02f + i * .40f, 1.38f, 0f),
                    new Vector3(.34f, 1.55f - (i % 3) * .12f, .18f), cloth, false);
                garment.transform.localRotation = Quaternion.Euler(0f, 0f, -5f + i * 2f);
            }
        }

        private static GameObject SetPropRoot(string name, Transform parent, Vector3 position, float yaw = 0f)
        {
            GameObject value = new(name);
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return value;
        }

        private static GameObject CreateFlaredHorn(string name, Transform parent, Vector3 position,
            Vector3 eulerAngles, float length, float throatRadius, float bellRadius, Material material)
        {
            const int segments = 18;
            const int rings = 6;
            Vector3[] vertices = new Vector3[segments * rings];
            int[] triangles = new int[(rings - 1) * segments * 6];
            for (int ring = 0; ring < rings; ring++)
            {
                float t = ring / (float)(rings - 1);
                float radius = Mathf.Lerp(throatRadius, bellRadius, t * t);
                float z = Mathf.Lerp(-length * .5f, length * .5f, t);
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    vertices[ring * segments + segment] =
                        new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z);
                }
            }
            int triangle = 0;
            for (int ring = 0; ring < rings - 1; ring++)
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                int a = ring * segments + segment;
                int b = ring * segments + next;
                int c = (ring + 1) * segments + segment;
                int d = (ring + 1) * segments + next;
                triangles[triangle++] = a; triangles[triangle++] = c; triangles[triangle++] = b;
                triangles[triangle++] = b; triangles[triangle++] = c; triangles[triangle++] = d;
            }
            Mesh mesh = new() { name = name + "Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GameObject horn = new(name);
            horn.transform.SetParent(parent, false);
            horn.transform.localPosition = position;
            horn.transform.localRotation = Quaternion.Euler(eulerAngles);
            horn.AddComponent<MeshFilter>().sharedMesh = mesh;
            horn.AddComponent<MeshRenderer>().sharedMaterial = material;
            Cylinder("留声机喇叭口沿", horn.transform, new Vector3(0f, 0f, length * .5f),
                new Vector3(bellRadius * 1.04f, .025f, bellRadius * 1.04f), material, false)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return horn;
        }

        private static void CreateRoomLight(Transform root, Vector3 position, Color color, float intensity, float range)
        {
            GameObject fixture = new GameObject("BackstagePracticalLight");
            fixture.transform.SetParent(root, false);
            fixture.transform.localPosition = position;
            Light light = fixture.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void BuildChime(Transform root, Material bronze, Material patina, Material dark, Material glow)
        {
            GameObject instrument = InstrumentRoot("中央编钟_可互动", root, new Vector3(0, 1.10f, 9.55f));
            AddBoxCollider(instrument, new Vector3(0, 2.12f, 0), new Vector3(7.15f, 4.35f, 1.05f));
            for (int level = 0; level < 3; level++)
            {
                float y = level * 1.05f;
                Box("ChimeBeam", instrument.transform, new Vector3(0, y + 1.62f, 0), new Vector3(6.85f - level * .32f, .24f, .30f), dark, false);
                Box("ChimeBeamBronzeInlay", instrument.transform, new Vector3(0, y + 1.63f, -.18f), new Vector3(6.45f - level * .32f, .065f, .035f), glow, false);
                for (int i = 0; i < 7 - level; i++)
                {
                    float scale = 1f - level * .10f + (i % 2) * .025f;
                    Vector3 bellPosition = new Vector3((i - (6 - level) / 2f) * .84f, y + 1.05f, 0);
                    CreateBronzeBell(instrument.transform, bellPosition, scale, bronze, patina, dark);
                }
            }
            for (int x = -1; x <= 1; x += 2)
            {
                Cylinder("ChimePost", instrument.transform, new Vector3(x * 3.25f, 1.98f, 0), new Vector3(.20f, 2.08f, .20f), dark, false);
                Box("ChimeFoot", instrument.transform, new Vector3(x * 3.25f, .05f, 0), new Vector3(1.05f, .18f, 1.05f), dark, false);
                Sphere("ChimePostCap", instrument.transform, new Vector3(x * 3.25f, 4.10f, 0), new Vector3(.32f, .22f, .32f), bronze, false);
            }
            Box("ChimeCrownBeam", instrument.transform, new Vector3(0, 4.04f, 0), new Vector3(7.15f, .28f, .38f), dark, false);
            CylinderBetween("ChimeMallet", instrument.transform, new Vector3(2.10f, .40f, -.72f), new Vector3(3.15f, 1.18f, -.72f), .075f, dark);
            Cylinder("ChimeMalletHead", instrument.transform, new Vector3(2.05f, .36f, -.72f), new Vector3(.18f, .28f, .18f), bronze, false).transform.localRotation = Quaternion.Euler(0, 0, 90);
            instrument.AddComponent<TheatreInstrument>().Configure("中央编钟", "敲响编钟，找回古戏台的第一层回声", 1f);
        }

        private static void BuildDrum(Transform root, Material wood, Material bronze, Material dark, Material ivory)
        {
            GameObject instrument = InstrumentRoot("大鼓_可互动", root, new Vector3(-6.1f, 1.45f, 9f));
            AddBoxCollider(instrument, new Vector3(0, 1.25f, 0), new Vector3(2.85f, 2.70f, 1.25f));
            Material lacquer = Mat("DrumLacquer", new Color(.50f, .048f, .024f), .34f);
            GameObject drum = Cylinder("DrumBody", instrument.transform, new Vector3(0, 1.6f, 0), new Vector3(1.15f, .45f, 1.15f), lacquer, false);
            drum.transform.localRotation = Quaternion.Euler(90, 0, 0);
            for (int face = -1; face <= 1; face += 2)
            {
                GameObject skin = Cylinder("DrumHide", instrument.transform, new Vector3(0, 1.6f, face * .49f), new Vector3(1.10f, .035f, 1.10f), ivory, false);
                skin.transform.localRotation = Quaternion.Euler(90, 0, 0);
                for (int i = 0; i < 18; i++)
                {
                    float angle = i * Mathf.PI * 2f / 18f;
                    Sphere("DrumNail", instrument.transform,
                        new Vector3(Mathf.Cos(angle) * 1.02f, 1.6f + Mathf.Sin(angle) * 1.02f, face * .535f),
                        new Vector3(.055f, .055f, .035f), bronze, false);
                }
            }
            for (int band = -1; band <= 1; band += 2)
            {
                GameObject ring = Cylinder("DrumLacquerBand", instrument.transform, new Vector3(0, 1.6f, band * .30f), new Vector3(1.17f, .035f, 1.17f), bronze, false);
                ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            Cylinder("DrumStandL", instrument.transform, new Vector3(-1.25f, .85f, 0), new Vector3(.16f, .95f, .16f), dark, false);
            Cylinder("DrumStandR", instrument.transform, new Vector3(1.25f, .85f, 0), new Vector3(.16f, .95f, .16f), dark, false);
            CylinderBetween("DrumStandBraceA", instrument.transform, new Vector3(-1.25f, .18f, 0), new Vector3(1.25f, 1.28f, 0), .075f, dark);
            CylinderBetween("DrumStandBraceB", instrument.transform, new Vector3(1.25f, .18f, .08f), new Vector3(-1.25f, 1.28f, .08f), .075f, dark);
            Box("DrumStandFoot", instrument.transform, new Vector3(0, -.02f, 0), new Vector3(3.0f, .16f, 1.05f), dark, false);
            CylinderBetween("DrumStickLeft", instrument.transform, new Vector3(-.85f, .26f, -.72f), new Vector3(.02f, .90f, -.72f), .045f, wood);
            CylinderBetween("DrumStickRight", instrument.transform, new Vector3(.85f, .26f, -.72f), new Vector3(-.02f, .90f, -.72f), .045f, wood);
            instrument.AddComponent<TheatreInstrument>().Configure("堂鼓", "击鼓唤醒木梁中沉睡的低频节拍", .52f);
        }

        private static void BuildGuzheng(Transform root, Material wood, Material soundboard, Material bronze, Material glow)
        {
            GameObject instrument = InstrumentRoot("古筝_可互动", root, new Vector3(5.5f, 1.18f, 7.2f));
            instrument.transform.localRotation = Quaternion.Euler(0, -14, 0);
            AddBoxCollider(instrument, new Vector3(0, .77f, 0), new Vector3(3.90f, .42f, .90f));
            Box("GuzhengBody", instrument.transform, new Vector3(0, .72f, 0), new Vector3(3.8f, .28f, .78f), wood, false);
            Box("GuzhengSoundboard", instrument.transform, new Vector3(0, .885f, 0), new Vector3(3.56f, .075f, .68f), soundboard, false);
            Sphere("GuzhengHeadCurve", instrument.transform, new Vector3(-1.84f, .76f, 0), new Vector3(.32f, .22f, .77f), wood, false);
            Sphere("GuzhengTailCurve", instrument.transform, new Vector3(1.84f, .76f, 0), new Vector3(.28f, .18f, .76f), wood, false);
            for (int i = 0; i < 21; i++)
            {
                float z = -.30f + i * .030f;
                Box("GuzhengString", instrument.transform, new Vector3(0, .965f, z), new Vector3(3.46f, .010f, .007f), i % 5 == 0 ? glow : bronze, false);
                float bridgeX = Mathf.Lerp(-.55f, .66f, i / 20f);
                GameObject bridge = Box("GuzhengBridge", instrument.transform, new Vector3(bridgeX, .935f, z), new Vector3(.085f, .19f, .025f), soundboard, false);
                bridge.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            }
            for (int i = 0; i < 8; i++)
                Cylinder("GuzhengTuningPeg", instrument.transform, new Vector3(1.55f, .83f, -.28f + i * .08f), new Vector3(.035f, .12f, .035f), bronze, false).transform.localRotation = Quaternion.Euler(0, 0, 90);
            for (int x = -1; x <= 1; x += 2)
            {
                Cylinder("GuzhengLeg", instrument.transform, new Vector3(x * 1.35f, .34f, 0), new Vector3(.09f, .38f, .09f), wood, false);
                Box("GuzhengLegCrossbar", instrument.transform, new Vector3(x * 1.35f, .13f, 0), new Vector3(.18f, .12f, .88f), wood, false);
            }
            instrument.AddComponent<TheatreInstrument>().Configure("古筝", "拨动古筝，把溪流旋律带进古戏台", 1.5f);
        }

        private static void BuildYangqin(Transform root, Material wood, Material soundboard, Material bronze, Material glow)
        {
            GameObject instrument = InstrumentRoot("扬琴_可互动", root, new Vector3(-4.9f, 1.05f, 6.65f));
            instrument.transform.localRotation = Quaternion.Euler(0, 9f, 0);
            AddBoxCollider(instrument, new Vector3(0, .70f, 0), new Vector3(2.65f, .45f, 1.45f));
            TrapezoidBox("YangqinBody", instrument.transform, new Vector3(0, .67f, 0), 2.65f, 1.95f, 1.38f, .28f, wood);
            TrapezoidBox("YangqinSoundboard", instrument.transform, new Vector3(0, .84f, 0), 2.48f, 1.82f, 1.23f, .075f, soundboard);
            for (int i = 0; i < 15; i++)
            {
                float z = -.52f + i * .074f;
                float length = Mathf.Lerp(1.82f, 2.30f, 1f - Mathf.Abs(z) / .58f);
                Box("YangqinString", instrument.transform, new Vector3(0, .905f, z), new Vector3(length, .009f, .007f), i % 4 == 0 ? glow : bronze, false);
            }
            Box("YangqinBridgeLeft", instrument.transform, new Vector3(-.38f, .925f, 0), new Vector3(.055f, .16f, 1.02f), bronze, false);
            Box("YangqinBridgeRight", instrument.transform, new Vector3(.38f, .925f, 0), new Vector3(.055f, .16f, 1.02f), bronze, false);
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 8; i++)
                Cylinder("YangqinTuningPeg", instrument.transform, new Vector3(side * (1.08f + i * .025f), .83f, -.48f + i * .14f),
                    new Vector3(.028f, .11f, .028f), bronze, false).transform.localRotation = Quaternion.Euler(0, 0, 90);
            for (int x = -1; x <= 1; x += 2)
                Cylinder("YangqinLeg", instrument.transform, new Vector3(x * .82f, .31f, 0), new Vector3(.075f, .34f, .075f), wood, false);
            CylinderBetween("YangqinMalletLeft", instrument.transform, new Vector3(-.95f, 1.03f, -.12f), new Vector3(-.15f, 1.22f, .35f), .025f, soundboard);
            CylinderBetween("YangqinMalletRight", instrument.transform, new Vector3(.95f, 1.03f, -.12f), new Vector3(.15f, 1.22f, .35f), .025f, soundboard);
            instrument.AddComponent<TheatreInstrument>().Configure("扬琴", "敲出明亮声部，让四件乐器完成合奏", 1.92f);
        }

        private static GameObject InstrumentRoot(string name, Transform root, Vector3 position)
        {
            GameObject value = new GameObject(name); value.transform.SetParent(root, false); value.transform.localPosition = position; return value;
        }

        private static void CreateBronzeBell(Transform parent, Vector3 position, float scale,
            Material bronze, Material patina, Material dark)
        {
            GameObject bellRoot = new GameObject("BronzeBell_Detailed");
            bellRoot.transform.SetParent(parent, false);
            bellRoot.transform.localPosition = position;
            bellRoot.transform.localScale = Vector3.one * scale;

            const int segments = 18;
            Vector2[] profile =
            {
                new(.075f, .34f), new(.11f, .27f), new(.14f, .12f), new(.21f, -.05f),
                new(.275f, -.22f), new(.32f, -.31f), new(.335f, -.34f)
            };
            Vector3[] vertices = new Vector3[profile.Length * segments];
            int[] triangles = new int[(profile.Length - 1) * segments * 6];
            for (int ring = 0; ring < profile.Length; ring++)
            for (int segment = 0; segment < segments; segment++)
            {
                float angle = segment * Mathf.PI * 2f / segments;
                vertices[ring * segments + segment] = new Vector3(Mathf.Cos(angle) * profile[ring].x,
                    profile[ring].y, Mathf.Sin(angle) * profile[ring].x);
            }
            int triangle = 0;
            for (int ring = 0; ring < profile.Length - 1; ring++)
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                int a = ring * segments + segment;
                int b = ring * segments + next;
                int c = (ring + 1) * segments + segment;
                int d = (ring + 1) * segments + next;
                triangles[triangle++] = a; triangles[triangle++] = b; triangles[triangle++] = c;
                triangles[triangle++] = b; triangles[triangle++] = d; triangles[triangle++] = c;
            }
            Mesh mesh = new Mesh { name = "TraditionalBronzeBellMesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            bellRoot.AddComponent<MeshFilter>().sharedMesh = mesh;
            bellRoot.AddComponent<MeshRenderer>().sharedMaterial = bronze;

            Cylinder("BellPatinaRim", bellRoot.transform, new Vector3(0, -.325f, 0), new Vector3(.35f, .028f, .35f), patina, false);
            Sphere("BellCrown", bellRoot.transform, new Vector3(0, .37f, 0), new Vector3(.13f, .10f, .13f), bronze, false);
            CylinderBetween("BellSuspensionCord", bellRoot.transform, new Vector3(0, .43f, 0), new Vector3(0, .69f, 0), .018f, dark);
            CylinderBetween("BellClapperStem", bellRoot.transform, new Vector3(0, .16f, 0), new Vector3(0, -.22f, 0), .018f, dark);
            Sphere("BellClapper", bellRoot.transform, new Vector3(0, -.25f, 0), new Vector3(.07f, .07f, .07f), dark, false);
        }

        private static void AddBoxCollider(GameObject target, Vector3 center, Vector3 size)
        {
            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private static GameObject CylinderBetween(string name, Transform parent, Vector3 start, Vector3 end,
            float radius, Material material)
        {
            Vector3 direction = end - start;
            GameObject value = Cylinder(name, parent, (start + end) * .5f,
                new Vector3(radius, Mathf.Max(.001f, direction.magnitude * .5f), radius), material, false);
            if (direction.sqrMagnitude > .0001f)
                value.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            return value;
        }

        private static GameObject TrapezoidBox(string name, Transform parent, Vector3 position,
            float frontWidth, float backWidth, float depth, float height, Material material)
        {
            float front = frontWidth * .5f;
            float back = backWidth * .5f;
            float halfDepth = depth * .5f;
            float halfHeight = height * .5f;
            Vector3[] vertices =
            {
                new(-front, -halfHeight, -halfDepth), new(front, -halfHeight, -halfDepth),
                new(back, -halfHeight, halfDepth), new(-back, -halfHeight, halfDepth),
                new(-front, halfHeight, -halfDepth), new(front, halfHeight, -halfDepth),
                new(back, halfHeight, halfDepth), new(-back, halfHeight, halfDepth)
            };
            int[] triangles =
            {
                0,2,1, 0,3,2, 4,5,6, 4,6,7,
                0,1,5, 0,5,4, 1,2,6, 1,6,5,
                2,3,7, 2,7,6, 3,0,4, 3,4,7
            };
            Mesh mesh = new Mesh { name = name + "Mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GameObject value = new GameObject(name);
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.AddComponent<MeshFilter>().sharedMesh = mesh;
            value.AddComponent<MeshRenderer>().sharedMaterial = material;
            return value;
        }

        private static void BuildPlayerAndCamera(Transform root, Material placeholderMaterial)
        {
            GameObject player = new GameObject("舞台访客");
            player.transform.SetParent(root, false); player.transform.localPosition = new Vector3(0, .08f, -7.4f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.82f; controller.radius = .34f; controller.center = new Vector3(0, .91f, 0);
            controller.stepOffset = .38f; controller.slopeLimit = 52f; controller.skinWidth = .045f;
            GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholder.name = "舞台角色加载占位"; placeholder.transform.SetParent(player.transform, false);
            placeholder.transform.localPosition = Vector3.up * .9f; placeholder.GetComponent<Renderer>().sharedMaterial = placeholderMaterial;
            DestroyGeneratedComponent(placeholder.GetComponent<Collider>());

            GameObject cameraObject = new GameObject("舞台第三人称相机");
            cameraObject.transform.SetParent(root, false); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.fieldOfView = 58; camera.nearClipPlane = .08f; camera.farClipPlane = 120;
            cameraObject.AddComponent<AudioListener>();
        }

        private static void BuildLighting(Transform root)
        {
            GameObject keyObject = new GameObject("WarmKeyLight"); keyObject.transform.SetParent(root, false);
            Light key = keyObject.AddComponent<Light>(); key.type = LightType.Directional; key.color = new Color(1, .70f, .40f);
            key.intensity = 1.18f; key.shadows = LightShadows.Soft; key.transform.localRotation = Quaternion.Euler(50, -25, 0);
            for (int i = -1; i <= 1; i++)
            {
                GameObject fillObject = new GameObject("StageFillLight"); fillObject.transform.SetParent(root, false);
                fillObject.transform.localPosition = new Vector3(i * 5, 6, 6);
                Light fill = fillObject.AddComponent<Light>(); fill.type = LightType.Point; fill.color = new Color(1, .46f, .15f); fill.range = 12; fill.intensity = 4.6f;
                GameObject spotObject = new GameObject("StageSpotlight"); spotObject.transform.SetParent(root, false);
                spotObject.transform.localPosition = new Vector3(i * 6.2f, 7.7f, 2.8f);
                spotObject.transform.localRotation = Quaternion.LookRotation(new Vector3(i * 2.4f, 2.2f, 9) - spotObject.transform.localPosition);
                Light spot = spotObject.AddComponent<Light>(); spot.type = LightType.Spot;
                spot.color = i == 0 ? new Color(1, .61f, .27f) : new Color(.39f, .66f, 1);
                spot.range = 22; spot.intensity = i == 0 ? 8 : 5.5f; spot.spotAngle = 42; spot.shadows = LightShadows.Soft;
            }
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject audienceObject = new GameObject("AudienceWarmFill");
                audienceObject.transform.SetParent(root, false);
                audienceObject.transform.localPosition = new Vector3(side * 5.8f, 4.8f, -5.0f);
                Light audience = audienceObject.AddComponent<Light>();
                audience.type = LightType.Point;
                audience.color = new Color(1f, .62f, .34f);
                audience.range = 13f;
                audience.intensity = 2.8f;
                audience.shadows = LightShadows.None;
            }
            GameObject ambientFillObject = new GameObject("HallSoftDirectionalFill");
            ambientFillObject.transform.SetParent(root, false);
            Light ambientFill = ambientFillObject.AddComponent<Light>();
            ambientFill.type = LightType.Directional;
            ambientFill.color = new Color(.58f, .70f, 1f);
            ambientFill.intensity = .52f;
            ambientFill.shadows = LightShadows.None;
            ambientFill.transform.localRotation = Quaternion.Euler(58f, 145f, 0f);
        }

        private static void BuildPostProcessing(Transform root)
        {
            Camera camera = root.GetComponentInChildren<Camera>(true);
            if (camera != null)
            {
                UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            }

            GameObject volumeObject = new GameObject("TheatrePostProcessing");
            volumeObject.transform.SetParent(root, false);
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 20f;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(.42f);
            bloom.threshold.Override(1.05f);
            bloom.scatter.Override(.62f);
            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(.88f);
            color.contrast.Override(8f);
            color.saturation.Override(5f);
            color.colorFilter.Override(new Color(1f, .94f, .84f));
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.18f);
            vignette.smoothness.Override(.52f);
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
        }

        private static GameObject Box(string n, Transform p, Vector3 pos, Vector3 scale, Material m, bool collider) => Primitive(PrimitiveType.Cube, n, p, pos, scale, m, collider);
        private static GameObject Cylinder(string n, Transform p, Vector3 pos, Vector3 scale, Material m, bool collider) => Primitive(PrimitiveType.Cylinder, n, p, pos, scale, m, collider);
        private static GameObject Sphere(string n, Transform p, Vector3 pos, Vector3 scale, Material m, bool collider) => Primitive(PrimitiveType.Sphere, n, p, pos, scale, m, collider);

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider)
        {
            GameObject value = GameObject.CreatePrimitive(type); value.name = name; value.transform.SetParent(parent, false);
            value.transform.localPosition = position; value.transform.localScale = scale;
            Renderer renderer = value.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            float volume = Mathf.Abs(scale.x * scale.y * scale.z);
            if (!collider && (volume < .018f || name.Contains("String") || name.Contains("纹") ||
                              name.Contains("铜钉") || name.Contains("Seam")))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            if (!collider) DestroyGeneratedComponent(value.GetComponent<Collider>()); return value;
        }

        private static void DestroyGeneratedComponent(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }

        private static Material Mat(string name, Color color, float smoothness, float metallic = 0f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            material.enableInstancing = true;
            return material;
        }

        private static Material EmissiveMat(string name, Color color, float intensity)
        {
            Material material = Mat(name, color, .42f, .48f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * intensity);
            }
            return material;
        }

        private enum SurfaceStyle { Plaster, Wood, Fabric }

        private static void ApplyMaterialTexture(Material material, SurfaceStyle style)
        {
            if (material == null) return;
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = material.name + "_Surface",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            Color baseColor = material.color;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float broad = Mathf.Sin(x * .37f + Mathf.Sin(y * .11f) * 2.1f) * .5f + .5f;
                float fine = Mathf.Sin(x * 2.31f + y * 1.73f) * Mathf.Sin(y * .83f - x * .47f) * .5f + .5f;
                float value;
                if (style == SurfaceStyle.Wood)
                {
                    float grain = Mathf.Sin(y * .72f + Mathf.Sin(x * .13f) * 3.2f) * .5f + .5f;
                    value = Mathf.Lerp(.72f, 1.22f, grain * .72f + fine * .28f);
                }
                else if (style == SurfaceStyle.Fabric)
                {
                    float weave = ((x & 3) == 0 ? .07f : 0f) + ((y & 3) == 0 ? .05f : 0f);
                    value = Mathf.Lerp(.86f, 1.10f, fine) + weave;
                }
                else
                {
                    value = Mathf.Lerp(.88f, 1.10f, broad * .55f + fine * .45f);
                }
                Color pixel = baseColor * value;
                pixels[y * size + x] = new Color(pixel.r, pixel.g, pixel.b, baseColor.a);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", style == SurfaceStyle.Plaster ? new Vector2(4f, 4f) : new Vector2(3f, 3f));
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", style == SurfaceStyle.Plaster ? new Vector2(4f, 4f) : new Vector2(3f, 3f));
            }
        }
    }
}
