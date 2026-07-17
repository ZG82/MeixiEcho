using UnityEngine;

namespace MeixiEcho
{
    /// <summary>Creates the chapter-two theatre after its tiny bootstrap scene has loaded.</summary>
    internal static class TheatreRuntimeEnvironment
    {
        private const string MarkerName = "Theatre_Set_RuntimeGenerated";

        public static void Ensure(Transform host)
        {
            if (host.Find(MarkerName) != null) return;
            Transform root = new GameObject(MarkerName).transform;
            root.SetParent(host, false);

            Material cream = Mat("Plaster", new Color(.72f, .65f, .53f), .18f);
            Material wood = Mat("DarkRedWood", new Color(.23f, .055f, .028f), .42f);
            Material floor = Mat("PolishedWood", new Color(.25f, .095f, .038f), .48f);
            Material bronze = Mat("AgedBronze", new Color(.34f, .22f, .075f), .74f);
            Material iron = Mat("BlackIron", new Color(.025f, .021f, .019f), .56f);
            Material curtain = Mat("CreamValance", new Color(.75f, .61f, .38f), .22f);
            Material carpet = Mat("AudienceCarpet", new Color(.035f, .105f, .072f), .30f);
            Material red = Mat("TheatreRed", new Color(.34f, .035f, .026f), .36f);

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
            BuildAudience(root, iron, wood);
            BuildChime(root, bronze, iron);
            BuildDrum(root, wood, bronze, iron);
            BuildGuzheng(root, wood, bronze);
            BuildYangqin(root, wood, bronze);
            BuildPlayerAndCamera(root, wood);
            BuildLighting(root);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.37f, .43f, .48f);
            RenderSettings.ambientEquatorColor = new Color(.18f, .135f, .105f);
            RenderSettings.ambientGroundColor = new Color(.035f, .027f, .022f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.105f, .082f, .068f);
            RenderSettings.fogDensity = .007f;
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

        private static void BuildAudience(Transform root, Material dark, Material wood)
        {
            Material cushion = Mat("TheatreChairCushion", new Color(.19f, .025f, .020f), .28f);
            for (int row = 0; row < 5; row++)
            for (int col = -3; col <= 3; col++)
            {
                if (col == 0) continue; // preserve a clear central aisle from the reference hall
                Vector3 p = new Vector3(col * 2.18f, .55f, -1.2f - row * 2.15f);
                Box("ChairSeat", root, p + new Vector3(0, .06f, 0), new Vector3(.86f, .14f, .72f), cushion, false);
                Box("ChairBackFrame", root, p + new Vector3(0, .55f, -.31f), new Vector3(.88f, .88f, .11f), dark, false);
                Box("ChairBackCushion", root, p + new Vector3(0, .56f, -.245f), new Vector3(.70f, .58f, .06f), cushion, false);
                for (int lx = -1; lx <= 1; lx += 2)
                for (int lz = -1; lz <= 1; lz += 2)
                    Cylinder("ChairLeg", root, p + new Vector3(lx * .34f, -.27f, lz * .26f), new Vector3(.035f, .28f, .035f), wood, false);
                if (row % 2 == 0)
                {
                    Cylinder("TeaTableTop", root, p + new Vector3(.92f, .37f, -.1f), new Vector3(.40f, .045f, .40f), wood, false);
                    Cylinder("TeaTablePost", root, p + new Vector3(.92f, .17f, -.1f), new Vector3(.045f, .20f, .045f), dark, false);
                }
            }
        }

        private static void BuildChime(Transform root, Material bronze, Material dark)
        {
            GameObject instrument = InstrumentRoot("中央编钟_可互动", root, new Vector3(0, 1.6f, 9.55f));
            for (int level = 0; level < 3; level++)
            {
                float y = level * 1.05f;
                Box("ChimeBeam", instrument.transform, new Vector3(0, y + 2.5f, 0), new Vector3(6.8f, .22f, .24f), dark, false);
                for (int i = 0; i < 7 - level; i++)
                {
                    GameObject bell = Cylinder("BronzeBell", instrument.transform, new Vector3((i - (6 - level) / 2f) * .82f, y + 1.85f, 0), new Vector3(.24f, .38f, .24f), bronze, false);
                    bell.transform.localRotation = Quaternion.Euler(0, 0, 180);
                }
            }
            for (int x = -1; x <= 1; x += 2) Cylinder("ChimePost", instrument.transform, new Vector3(x * 3.1f, 1.7f, 0), new Vector3(.18f, 1.8f, .18f), dark, false);
            instrument.AddComponent<TheatreInstrument>().Configure("中央编钟", "敲响编钟，找回古戏台的第一层回声", 1f);
        }

        private static void BuildDrum(Transform root, Material wood, Material bronze, Material dark)
        {
            GameObject instrument = InstrumentRoot("大鼓_可互动", root, new Vector3(-6.1f, 1.45f, 9f));
            Material lacquer = Mat("DrumLacquer", new Color(.42f, .045f, .025f), .4f);
            GameObject drum = Cylinder("DrumBody", instrument.transform, new Vector3(0, 1.6f, 0), new Vector3(1.15f, .45f, 1.15f), lacquer, false);
            drum.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Cylinder("DrumStandL", instrument.transform, new Vector3(-1.15f, .8f, 0), new Vector3(.14f, .9f, .14f), dark, false);
            Cylinder("DrumStandR", instrument.transform, new Vector3(1.15f, .8f, 0), new Vector3(.14f, .9f, .14f), dark, false);
            Sphere("DrumBoss", instrument.transform, new Vector3(0, 1.6f, -.49f), new Vector3(.22f, .22f, .22f), bronze, false);
            instrument.AddComponent<TheatreInstrument>().Configure("堂鼓", "击鼓唤醒木梁中沉睡的低频节拍", .52f);
        }

        private static void BuildGuzheng(Transform root, Material wood, Material bronze)
        {
            GameObject instrument = InstrumentRoot("古筝_可互动", root, new Vector3(5.5f, 1.18f, 7.2f));
            instrument.transform.localRotation = Quaternion.Euler(0, -14, 0);
            Box("GuzhengBody", instrument.transform, new Vector3(0, .75f, 0), new Vector3(3.7f, .18f, .68f), wood, false);
            for (int i = 0; i < 11; i++) Box("GuzhengString", instrument.transform, new Vector3(0, .87f, -.27f + i * .054f), new Vector3(3.3f, .014f, .012f), bronze, false);
            for (int x = -1; x <= 1; x += 2) Cylinder("GuzhengLeg", instrument.transform, new Vector3(x * 1.35f, .37f, 0), new Vector3(.07f, .38f, .07f), wood, false);
            instrument.AddComponent<TheatreInstrument>().Configure("古筝", "拨动古筝，把溪流旋律带进古戏台", 1.5f);
        }

        private static void BuildYangqin(Transform root, Material wood, Material bronze)
        {
            GameObject instrument = InstrumentRoot("扬琴_可互动", root, new Vector3(-4.9f, 1.05f, 6.65f));
            Box("YangqinBody", instrument.transform, new Vector3(0, .68f, 0), new Vector3(2.3f, .18f, 1.2f), wood, false);
            for (int i = 0; i < 9; i++) Box("YangqinString", instrument.transform, new Vector3(0, .80f, -.43f + i * .11f), new Vector3(2.05f, .012f, .012f), bronze, false);
            instrument.AddComponent<TheatreInstrument>().Configure("扬琴", "敲出明亮声部，让四件乐器完成合奏", 1.92f);
        }

        private static GameObject InstrumentRoot(string name, Transform root, Vector3 position)
        {
            GameObject value = new GameObject(name); value.transform.SetParent(root, false); value.transform.localPosition = position; return value;
        }

        private static void BuildPlayerAndCamera(Transform root, Material placeholderMaterial)
        {
            GameObject player = new GameObject("舞台访客");
            player.transform.SetParent(root, false); player.transform.localPosition = new Vector3(0, .08f, -10.5f);
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

        private static Material Mat(string name, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }
    }
}
