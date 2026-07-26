using UnityEngine;

namespace MeixiEcho
{
    /// <summary>
    /// Runtime safety net for the road between chapter one and the theatre.
    /// The authored scene used by older builds does not contain the editor-generated
    /// exit, so the route must be guaranteed whenever the village scene starts.
    /// </summary>
    public static class MeixiSecondChapterRoute
    {
        public static readonly Vector3 EntrancePosition = new(0f, 0f, 34.8f);
        private const string RootName = "第二章入口_运行时保障";

        public static void Ensure(MeixiGame3D game)
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                MeixiRouteBeacon existingBeacon = existing.GetComponent<MeixiRouteBeacon>();
                if (existingBeacon != null) existingBeacon.Bind(game);
                return;
            }

            RemoveBlockingNorthBoundary();

            Material slate = Material("戏台路青石", new Color(.20f, .27f, .29f), .28f);
            Material edge = Material("戏台路石沿", new Color(.11f, .16f, .16f), .18f);
            Material timber = Material("戏台入口木构", new Color(.24f, .075f, .035f), .32f);
            Material gold = EmissiveMaterial("戏台入口灯火", new Color(1f, .46f, .10f), 2.8f);
            Material cyan = EmissiveMaterial("戏台入口声纹", new Color(.10f, .82f, 1f), 3.2f);

            GameObject root = new(RootName);
            Box("北行路基", root.transform, new Vector3(0f, -.24f, 27.7f), new Vector3(6.4f, .48f, 18.8f), edge, true);

            for (int i = 0; i < 22; i++)
            {
                float z = 18.7f + i * .78f;
                float x = Mathf.Sin(i * 1.71f) * .11f;
                GameObject slab = Box("引路青石", root.transform, new Vector3(x, .035f, z),
                    new Vector3(4.75f - (i % 3) * .12f, .12f, .66f), slate, true);
                slab.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(i * .83f) * 1.7f, 0f);
            }

            // Solid, visible boundaries guide the player toward the portal without
            // relying on another invisible wall.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 9; i++)
                {
                    float z = 20f + i * 1.75f;
                    Box("竹木护栏柱", root.transform, new Vector3(side * 3.05f, .58f, z),
                        new Vector3(.13f, 1.16f, .13f), timber, true);
                    Box("竹木护栏横梁", root.transform, new Vector3(side * 3.05f, .72f, z + .78f),
                        new Vector3(.10f, .10f, 1.62f), timber, true);
                    if (i % 2 == 0) CreateLantern(root.transform, new Vector3(side * 2.72f, 1.42f, z), timber, gold);
                }
            }

            // A tall illuminated gate can be seen from the central village square.
            Box("古戏台门柱左", root.transform, new Vector3(-2.65f, 2.25f, 34.0f), new Vector3(.48f, 4.5f, .52f), timber, true);
            Box("古戏台门柱右", root.transform, new Vector3(2.65f, 2.25f, 34.0f), new Vector3(.48f, 4.5f, .52f), timber, true);
            Box("古戏台门楣", root.transform, new Vector3(0f, 4.36f, 34.0f), new Vector3(6.35f, .55f, .65f), timber, true);
            Box("古戏台金匾", root.transform, new Vector3(0f, 4.35f, 33.62f), new Vector3(2.85f, .72f, .10f), gold, false);

            CreatePortal(root.transform, cyan, gold);
            CreateWorldLabel(root.transform, timber, gold);

            GameObject trigger = new("第二章_古戏台加载触发区");
            trigger.transform.SetParent(root.transform, false);
            trigger.transform.position = EntrancePosition + Vector3.up * 1.5f;
            BoxCollider triggerCollider = trigger.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(5.0f, 3.0f, 2.2f);
            trigger.AddComponent<MeixiSceneExit>();

            MeixiRouteBeacon beacon = root.AddComponent<MeixiRouteBeacon>();
            beacon.Bind(game);
            Debug.Log("[MeixiRoute] Runtime chapter-two road and theatre entrance created.");
        }

        private static void RemoveBlockingNorthBoundary()
        {
            foreach (BoxCollider collider in Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include))
            {
                if (collider == null || collider.isTrigger) continue;
                Vector3 center = collider.transform.TransformPoint(collider.center);
                Vector3 worldSize = Vector3.Scale(collider.size, collider.transform.lossyScale);
                if (center.z > 23.2f && center.z < 24.8f && Mathf.Abs(center.x) < 2f && worldSize.x > 35f)
                {
                    collider.enabled = false;
                    Renderer renderer = collider.GetComponent<Renderer>();
                    if (renderer != null) renderer.enabled = false;
                    Debug.Log($"[MeixiRoute] Disabled north boundary: {collider.name}");
                }
            }
        }

        private static void CreatePortal(Transform parent, Material cyan, Material gold)
        {
            GameObject portal = new("第二章声纹门");
            portal.transform.SetParent(parent, false);
            portal.transform.position = new Vector3(0f, 2.15f, 34.18f);

            for (int ringIndex = 0; ringIndex < 3; ringIndex++)
            {
                GameObject ring = new("声纹门环");
                ring.transform.SetParent(portal.transform, false);
                LineRenderer line = ring.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 56;
                line.widthMultiplier = .055f - ringIndex * .009f;
                line.material = ringIndex == 1 ? gold : cyan;
                float radiusX = 1.45f + ringIndex * .22f;
                float radiusY = 1.80f + ringIndex * .18f;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float angle = i / (line.positionCount - 1f) * Mathf.PI * 2f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY, ringIndex * .035f));
                }
            }

            GameObject lightObject = new("戏台入口引导光");
            lightObject.transform.SetParent(portal.transform, false);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(.18f, .82f, 1f);
            light.range = 12f;
            light.intensity = 3.2f;
        }

        private static void CreateWorldLabel(Transform parent, Material timber, Material gold)
        {
            Box("入口指示牌", parent, new Vector3(0f, 5.25f, 33.88f), new Vector3(5.2f, .82f, .16f), timber, false);
            GameObject label = new("第二章入口文字");
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(0f, 5.24f, 33.76f);
            // The player approaches from the south side of the board. TextMesh's
            // identity-facing side is readable from that direction; 180 degrees
            // mirrored the chapter title at the end of the first-chapter road.
            label.transform.rotation = Quaternion.identity;
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = "第二章 · 古厝戏台";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 72;
            text.characterSize = .075f;
            text.color = new Color(1f, .78f, .35f);
            Font font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 72);
            if (font != null)
            {
                text.font = font;
                MeshRenderer textRenderer = text.GetComponent<MeshRenderer>();
                Shader depthTextShader = Shader.Find("MeixiEcho/World Text Depth");
                if (depthTextShader != null)
                {
                    Material depthTextMaterial = new(depthTextShader) { name = "第二章入口深度文字" };
                    depthTextMaterial.SetTexture("_MainTex", font.material.mainTexture);
                    depthTextMaterial.SetColor("_Color", Color.white);
                    textRenderer.sharedMaterial = depthTextMaterial;
                    Debug.Log("[MeixiRoute] Entrance label uses depth-tested world text material.");
                }
                else textRenderer.sharedMaterial = font.material;
                textRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                textRenderer.receiveShadows = false;
            }
        }

        private static void CreateLantern(Transform parent, Vector3 position, Material timber, Material glow)
        {
            Box("引路灯杆", parent, position, new Vector3(.10f, 2.75f, .10f), timber, true);
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "引路灯笼";
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = position + Vector3.up * 1.23f;
            lamp.transform.localScale = new Vector3(.38f, .48f, .38f);
            lamp.GetComponent<Renderer>().sharedMaterial = glow;
            Object.Destroy(lamp.GetComponent<Collider>());
            Light light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, .48f, .14f);
            light.range = 5.5f;
            light.intensity = 1.7f;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.Destroy(box.GetComponent<Collider>());
            return box;
        }

        private static Material Material(string name, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static Material EmissiveMaterial(string name, Color color, float intensity)
        {
            Material material = Material(name, color, .42f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * intensity);
            }
            return material;
        }
    }

    public sealed class MeixiRouteBeacon : MonoBehaviour
    {
        private MeixiGame3D game;
        private Light beaconLight;
        private LineRenderer[] rings;

        public void Bind(MeixiGame3D owner)
        {
            game = owner;
            beaconLight = GetComponentInChildren<Light>();
            rings = GetComponentsInChildren<LineRenderer>();
        }

        private void Update()
        {
            bool unlocked = game != null && game.SecondChapterRouteActive;
            float pulse = .78f + Mathf.Sin(Time.unscaledTime * 3.4f) * .22f;
            if (beaconLight != null)
            {
                beaconLight.intensity = (unlocked ? 4.8f : 2.2f) * pulse;
                beaconLight.color = unlocked ? new Color(.18f, 1f, .86f) : new Color(1f, .42f, .12f);
            }
            if (rings == null) return;
            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i] == null) continue;
                rings[i].transform.localRotation = Quaternion.Euler(0f, 0f,
                    Time.unscaledTime * (i % 2 == 0 ? 18f : -14f));
                rings[i].widthMultiplier = (.046f + i * .006f) * pulse;
            }
        }
    }
}
