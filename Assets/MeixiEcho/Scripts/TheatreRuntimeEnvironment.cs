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
            Material stringGlow = EmissiveMat("InstrumentStrings", new Color(1f, .58f, .16f), 1.25f);

            Box("HallFloor", root, new Vector3(0, -.12f, 0), new Vector3(20, .24f, 27), floor, true);
            Box("AudienceCarpet", root, new Vector3(0, .015f, -6.2f), new Vector3(17.4f, .045f, 14.8f), carpet, false);
            Box("Stage", root, new Vector3(0, .55f, 8.65f), new Vector3(16.4f, 1.1f, 7.5f), floor, true);
            Box("StageApron", root, new Vector3(0, .22f, 5.03f), new Vector3(16.5f, .45f, .42f), wood, true);
            for (int i = 0; i < 5; i++)
            {
                float h = .22f * (i + 1);
                Box("CentralStep_" + i, root, new Vector3(0, h * .5f, 3.70f + i * .31f), new Vector3(4.7f, h, .43f), floor, true);
            }
            Box("RearWall", root, new Vector3(0, 4.3f, 12.32f), new Vector3(20, 8.6f, .32f), cream, true);
            Box("LeftWall", root, new Vector3(-10, 4.3f, 0), new Vector3(.3f, 8.6f, 25), cream, true);
            Box("RightWall", root, new Vector3(10, 4.3f, 0), new Vector3(.3f, 8.6f, 25), cream, true);
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
            quad.transform.localScale = new Vector3(15.8f, 7.05f, 1);
            quad.GetComponent<Renderer>().sharedMaterial = backdrop;
            Object.Destroy(quad.GetComponent<Collider>());
            Box("BackdropTopFrame", root, new Vector3(0, 8.18f, 11.96f), new Vector3(16.4f, .25f, .24f), wood, false);
            Box("BackdropLeftFrame", root, new Vector3(-8.03f, 4.66f, 11.96f), new Vector3(.25f, 7.28f, .24f), wood, false);
            Box("BackdropRightFrame", root, new Vector3(8.03f, 4.66f, 11.96f), new Vector3(.25f, 7.28f, .24f), wood, false);
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
            Texture2D frameTexture = Resources.Load<Texture2D>("Art/QuestFrameV2");
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
                    decal.name = "TraditionalWallDecal";
                    decal.transform.SetParent(root, false);
                    decal.transform.localPosition = new Vector3(side * 9.80f, 4.45f, z);
                    decal.transform.localRotation = Quaternion.Euler(0, side < 0 ? 90f : -90f, 0);
                    decal.transform.localScale = new Vector3(2.8f, 1.65f, 1f);
                    decal.GetComponent<Renderer>().sharedMaterial = poster;
                    Object.Destroy(decal.GetComponent<Collider>());
                    Box("DecalTopFrame", root, new Vector3(side * 9.65f, 5.35f, z), new Vector3(.16f, .11f, 3.05f), wood, false);
                    Box("DecalBottomFrame", root, new Vector3(side * 9.65f, 3.55f, z), new Vector3(.16f, .11f, 3.05f), wood, false);
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
            Object.Destroy(placeholder.GetComponent<Collider>());

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
            value.transform.localPosition = position; value.transform.localScale = scale; value.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.Destroy(value.GetComponent<Collider>()); return value;
        }

        private static Material Mat(string name, Color color, float smoothness, float metallic = 0f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
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
    }
}
