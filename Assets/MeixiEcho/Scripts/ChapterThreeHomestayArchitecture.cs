using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;

namespace MeixiEcho
{
    /// <summary>
    /// Detailed, meter-scaled architecture for the chapter-three homestay.
    /// The plan is a two-storey courtyard house: every guest-room door opens
    /// directly onto an exterior gallery, so there is no internal corridor.
    /// </summary>
    public sealed partial class ChapterThreeHomestayRuntime
    {
        private const float BuildingFront = 5.25f;
        private const float BuildingBack = 17.25f;
        private const float StoreyHeight = 4.05f;
        private const float BuildingHalfWidth = 10.35f;
        private const float RoomHalfWidth = 3.35f;
        private const float RoomCenterZ = 11.25f;
        private const float RoomDepth = 12f;
        private static readonly float[] RoomCenters = { -6.90f, 0f, 6.90f };

        private Material worldTextMaterial;
        private Material wetStone;
        private Material dampGround;
        private Material grassGround;
        private Material roofTiles;
        private Material porcelain;
        private Material bathroomTile;
        private Material mirror;
        private Material blackMetal;
        private Material softWhite;
        private Material softBlue;
        private Material mutedGreen;
        private Material distantSilhouette;
        private Material foliageCards;
        private Material villageDecals;
        private Material surfaceDecals;
        private Material architectureDecals;
        private Material interiorDecorCards;
        private Material homestayFoliageCards;
        private Material frostedGlass;
        private Material stableWindowGlass;
        private Material coldAirParticleMaterial;
        private Material stairTread;
        private Material stairRunner;
        private Mesh roundedBoxMesh;
        private Mesh windowPaneMesh;
        private readonly Dictionary<int, Mesh> atlasQuadMeshes = new();

        private void BuildRefinedMaterials()
        {
            Texture2D architectureAtlas = Resources.Load<Texture2D>("ChapterThree/ArchitectureSurfaceAtlas");
            Texture2D bathroomAtlas = Resources.Load<Texture2D>("ChapterThree/BathroomSurfaceAtlas");
            Texture2D courtyardAtlas = Resources.Load<Texture2D>("ChapterThree/CourtyardSurfaceAtlas");
            Texture2D stairAtlas = Resources.Load<Texture2D>("ChapterThree/StairSurfaceAtlas");
            wetStone = AtlasMaterial("Chapter3_WetBluestone", courtyardAtlas, new Vector2(0f, .5f), new Color(.82f, .86f, .84f), .26f);
            dampGround = AtlasMaterial("Chapter3_DampGround", courtyardAtlas, new Vector2(.5f, .5f), new Color(.76f, .73f, .66f), .10f);
            grassGround = TexturedMaterial("Chapter3_HumidGrass",
                Resources.Load<Texture2D>("Textures/Realistic/HumidGrassGround"),
                new Color(.54f, .61f, .43f), .08f, new Vector2(12f, 12f));
            roofTiles = AtlasMaterial("Chapter3_GreyRoofTiles", architectureAtlas, new Vector2(.5f, 0f), new Color(.66f, .69f, .70f), .20f);
            porcelain = AtlasMaterial("Chapter3_Porcelain", bathroomAtlas, new Vector2(.5f, 0f), new Color(.97f, .97f, .94f), .70f);
            bathroomTile = AtlasMaterial("Chapter3_BathroomTile", bathroomAtlas, new Vector2(0f, .5f), new Color(.91f, .94f, .90f), .40f);
            mirror = Material("Chapter3_Mirror", new Color(.36f, .47f, .50f), .96f);
            blackMetal = AtlasMaterial("Chapter3_BlackMetal", stairAtlas, new Vector2(.5f, .5f), new Color(.34f, .36f, .36f), .72f);
            SetMetallic(blackMetal, .78f);
            SetMetallic(brass, .72f);
            softWhite = Material("Chapter3_SoftWhiteFabric", new Color(.83f, .80f, .72f), .06f);
            softBlue = AtlasMaterial("Chapter3_IndigoFabric", stairAtlas, new Vector2(0f, 0f), new Color(.64f, .72f, .77f), .07f);
            mutedGreen = Material("Chapter3_MutedFoliage", new Color(.10f, .25f, .13f), .10f);
            distantSilhouette = Material("Chapter3_DistantVillage", new Color(.105f, .12f, .12f), .16f);
            stairTread = AtlasMaterial("Chapter3_StairTread", stairAtlas, new Vector2(0f, .5f), new Color(.82f, .72f, .61f), .25f);
            stairRunner = AtlasMaterial("Chapter3_StairRunner", stairAtlas, new Vector2(0f, 0f), new Color(.72f, .78f, .80f), .06f);
            frostedGlass = TransparentAtlasMaterial("Chapter3_FrostedRibbedGlass", bathroomAtlas,
                new Vector2(0f, 0f), new Color(.88f, .96f, .98f, .26f), .68f);
            stableWindowGlass = Material("Chapter3_StableWindowGlass", new Color(.70f, .90f, .94f, .085f), .76f, true);
            Shader stablePaneShader = Shader.Find("MeixiEcho/StableTransparentPane");
            if (stablePaneShader != null)
            {
                stableWindowGlass = new Material(stablePaneShader) { name = "Chapter3_StableWindowGlass" };
                stableWindowGlass.SetColor("_BaseColor", new Color(.70f, .90f, .94f, .085f));
                stableWindowGlass.SetFloat("_Smoothness", .76f);
                frostedGlass = new Material(stablePaneShader) { name = "Chapter3_FrostedRibbedGlass" };
                frostedGlass.SetColor("_BaseColor", new Color(.88f, .96f, .98f, .26f));
                frostedGlass.SetFloat("_Smoothness", .68f);
                if (bathroomAtlas != null)
                {
                    frostedGlass.SetTexture("_BaseMap", bathroomAtlas);
                    frostedGlass.SetTextureScale("_BaseMap", new Vector2(.5f, .5f));
                    frostedGlass.SetTextureOffset("_BaseMap", Vector2.zero);
                }
            }
            // Transparent panes write depth and render both sides. This removes
            // view-dependent sorting swaps between the six facade panes without
            // putting a second surface behind the glass.
            if (stableWindowGlass.HasProperty("_ZWrite")) stableWindowGlass.SetInt("_ZWrite", 1);
            if (stableWindowGlass.HasProperty("_Cull")) stableWindowGlass.SetInt("_Cull", (int)CullMode.Off);
            stableWindowGlass.renderQueue = (int)RenderQueue.Transparent - 4;
            if (frostedGlass.HasProperty("_ZWrite")) frostedGlass.SetInt("_ZWrite", 1);
            if (frostedGlass.HasProperty("_Cull")) frostedGlass.SetInt("_Cull", (int)CullMode.Off);
            frostedGlass.renderQueue = (int)RenderQueue.Transparent - 3;
            coldAirParticleMaterial = CreateColdAirParticleMaterial();
            foliageCards = CutoutMaterial("Chapter3_FoliageCards",
                Resources.Load<Texture2D>("Textures/Realistic/Generated/MeixiFoliageAtlas"), .34f);
            villageDecals = CutoutMaterial("Chapter3_VillageDecals",
                Resources.Load<Texture2D>("Textures/Realistic/Generated/MeixiVillageDecals"), .12f);
            surfaceDecals = CutoutMaterial("Chapter3_SurfaceDecals",
                Resources.Load<Texture2D>("Textures/Realistic/Generated/MeixiSurfaceDecals"), .12f);
            architectureDecals = CutoutMaterial("Chapter3_ArchitectureDecals",
                Resources.Load<Texture2D>("ChapterThree/ArchitectureDecalAtlas"), .08f);
            homestayFoliageCards = CutoutMaterial("Chapter3_HomestayFoliage",
                Resources.Load<Texture2D>("ChapterThree/HomestayFoliageAtlas"), .08f);
            interiorDecorCards = UnlitTextureMaterial("Chapter3_InteriorDecor",
                Resources.Load<Texture2D>("ChapterThree/InteriorDecorAtlas"), Color.white);
        }

        private void BuildRefinedOutdoorEnvironment()
        {
            // One continuous collision surface prevents the character controller from
            // catching on the seams that existed in the earlier thirty-tile courtyard.
            Box("LandscapeGround", new Vector3(0f, -.46f, 5f), new Vector3(96f, .72f, 112f), dampGround, true);
            Box("CourtyardBluestone", new Vector3(0f, -.055f, -3.75f), new Vector3(27.8f, .19f, 22.1f), wetStone, true);
            // Foundations stay below all authored finish floors. The previous top
            // surface at y=.20 was coplanar with room slabs, bathroom tiles and rugs,
            // producing black z-fighting across the entire ground floor.
            // Stop before the stone house foundation. The former 30 cm overlap
            // left two top faces only 5 mm apart and shimmered along the facade.
            Box("GalleryFoundation", new Vector3(0f, -.16f, 3.94f), new Vector3(21.9f, .34f, 2.10f), deepWood, true);
            Box("EntryStonePath", new Vector3(0f, .055f, -13.2f), new Vector3(3.15f, .10f, 8.1f), wetStone, true);
            for (int i = 0; i < 13; i++)
            {
                float z = -16.6f + i * .63f;
                GameObject stone = RoundedBox("HandLaidPathStone", new Vector3(Mathf.Sin(i * 1.81f) * .10f, .12f, z),
                    new Vector3(2.62f - (i % 3) * .10f, .095f, .52f), wetStone, .07f, false);
                stone.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(i * 1.17f) * 2.8f, 0f);
            }

            BuildCourtyardBoundary();
            BuildApproachLandscape();
            BuildVillageBackdrop();

            // Decals use the actual finish beneath them. WetFootprints sits on
            // the raised entry path, while DampThreshold sits on the gallery.
            CreateGroundDecal("WetFootprints", new Vector3(-.42f, .126f, -10.1f), new Vector2(1.15f, 2.0f), 2, 0, 4, 4);
            CreateGroundDecal("CourtyardLeaves", new Vector3(6.4f, .057f, -3.4f), new Vector2(2.0f, 1.65f), 0, 1, 4, 4);
            CreateGroundDecal("CourtyardCrack", new Vector3(-5.4f, .058f, -2.1f), new Vector2(1.75f, 1.15f), 2, 1, 4, 4);
            CreateGroundDecal("DampThreshold", new Vector3(.15f, .248f, 4.3f), new Vector2(2.5f, 1.0f), 1, 2, 4, 4);
            BuildPublicHomestayDetails();
        }

        private void BuildCourtyardBoundary()
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 15.0f;
                Box("CourtyardRenderedWall", new Vector3(x, 1.18f, -2.9f), new Vector3(.48f, 2.45f, 25.2f), plaster, true);
                Box("CourtyardStonePlinth", new Vector3(x - side * .02f, .42f, -2.9f), new Vector3(.54f, .84f, 25.35f), wetStone, false);
                Box("CourtyardWallCap", new Vector3(x, 2.48f, -2.9f), new Vector3(.66f, .18f, 25.35f), roofTiles, false);
                for (int i = 0; i < 10; i++)
                {
                    float z = -13.4f + i * 2.2f;
                    Box("WallTimberPost", new Vector3(x - side * .28f, 1.48f, z), new Vector3(.14f, 2.64f, .18f), deepWood, false);
                }
            }

            // Front wall is split around a real 3.2 m gate opening.
            // Stop the wall panels just outside the pillars. Their former 12 cm
            // penetration produced unstable corner faces at the gate opening.
            Box("FrontCourtyardWallLeft", new Vector3(-8.51f, 1.25f, -15.7f), new Vector3(12.88f, 2.5f, .50f), plaster, true);
            Box("FrontCourtyardWallRight", new Vector3(8.51f, 1.25f, -15.7f), new Vector3(12.88f, 2.5f, .50f), plaster, true);
            Box("GatePillarLeft", new Vector3(-1.78f, 1.65f, -15.65f), new Vector3(.58f, 3.3f, .72f), deepWood, true);
            Box("GatePillarRight", new Vector3(1.78f, 1.65f, -15.65f), new Vector3(.58f, 3.3f, .72f), deepWood, true);
            // The lintel is shallower than the pillars, so their front and rear
            // faces cannot occupy the same depth where the joints intersect.
            Box("GateLintel", new Vector3(0f, 3.12f, -15.65f), new Vector3(4.15f, .34f, .54f), deepWood, true);
            CreatePlaque("民宿", new Vector3(0f, 3.64f, -16.03f), Quaternion.identity,
                new Vector2(1.85f, .62f), .072f, new Color(1f, .72f, .29f));
            CreateWallDecal("GateWallMoss", new Vector3(-6.2f, .94f, -15.41f), Quaternion.Euler(0f, 180f, 0f),
                new Vector2(3.0f, 1.45f), 0, 0, 4, 4);
        }

        private void BuildApproachLandscape()
        {
            // Near-field scenery beyond the gate keeps the player on authored terrain
            // instead of exposing a flat void immediately outside the courtyard.
            Box("HomestayApproachLane", new Vector3(0f, -.015f, -25.25f), new Vector3(3.55f, .17f, 19.2f), wetStone, true);
            // End the approach banks at the courtyard edge instead of laying
            // them over the bluestone for almost two metres with identical tops.
            Box("ApproachGrassBankLeft", new Vector3(-7.0f, -.035f, -26.10f), new Vector3(10.0f, .15f, 22.40f), grassGround, true);
            Box("ApproachGrassBankRight", new Vector3(7.0f, -.035f, -26.10f), new Vector3(10.0f, .15f, 22.40f), grassGround, true);
            Box("RoadsideDrainLeft", new Vector3(-2.08f, .035f, -25.2f), new Vector3(.34f, .11f, 19.4f), blackMetal, false);
            Box("RoadsideDrainRight", new Vector3(2.08f, .035f, -25.2f), new Vector3(.34f, .11f, 19.4f), blackMetal, false);
            Box("DrainWaterLeft", new Vector3(-2.08f, .102f, -25.2f), new Vector3(.22f, .018f, 19.1f), glass, false);
            Box("DrainWaterRight", new Vector3(2.08f, .102f, -25.2f), new Vector3(.22f, .018f, 19.1f), glass, false);

            // A low roadside fence makes the clue placed beyond the courtyard boundary
            // spatially legible while leaving a walkable opening beside the main gate.
            for (int side = -1; side <= 1; side += 2)
            {
                const float fenceX = 2.82f;
                for (int i = 0; i < 6; i++)
                {
                    float z = -18.4f - i * 3.0f;
                    RoundedBox("ApproachFencePost", new Vector3(side * fenceX, .58f, z),
                        new Vector3(.18f, 1.16f, .18f), deepWood, .035f, true);
                    if (i >= 5) continue;
                    float railZ = z - 1.5f;
                    Box("ApproachFenceRail", new Vector3(side * fenceX, .43f, railZ),
                        new Vector3(.13f, .13f, 2.86f), oldWood, true);
                    Box("ApproachFenceRail", new Vector3(side * fenceX, .82f, railZ),
                        new Vector3(.13f, .11f, 2.86f), oldWood, true);
                }
            }

            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 7; i++)
            {
                float z = -18.4f - i * 2.55f;
                RoundedBox("RoadsideRetainingStone", new Vector3(side * (5.7f + (i % 2) * .24f), .22f, z),
                    new Vector3(2.15f, .44f + (i % 3) * .08f, .72f), wetStone, .12f, false)
                    .transform.rotation = Quaternion.Euler(0f, side * (4f + i * 2.1f), side * 1.2f);
            }

            Vector3[] rocks =
            {
                new(-4.25f, .22f, -19.6f), new(4.65f, .18f, -21.8f),
                new(-7.7f, .30f, -27.2f), new(7.3f, .25f, -31.0f),
                new(-4.8f, .16f, -34.0f), new(5.3f, .20f, -36.2f)
            };
            for (int i = 0; i < rocks.Length; i++)
            {
                GameObject rock = Sphere("MossyRoadsideBoulder", rocks[i],
                    new Vector3(.72f + (i % 3) * .22f, .48f + (i % 2) * .15f, .58f + ((i + 1) % 3) * .18f), wetStone);
                rock.transform.rotation = Quaternion.Euler(i * 11f, i * 37f, i * 7f);
            }

            CreateDetailedTree(new Vector3(-6.8f, .05f, -21.6f), .78f, 1);
            CreateDetailedTree(new Vector3(7.4f, .05f, -24.8f), .86f, 2);
            CreateDetailedTree(new Vector3(-8.3f, .05f, -32.4f), .92f, 3);
            CreateDetailedTree(new Vector3(8.8f, .05f, -35.2f), .76f, 0);
            CreateGroundDecal("ApproachLeafScatter", new Vector3(-1.15f, .115f, -22.4f), new Vector2(2.2f, 1.7f), 0, 1, 4, 4);
            CreateGroundDecal("ApproachWetTrace", new Vector3(.75f, .116f, -30.7f), new Vector2(1.8f, 2.5f), 1, 2, 4, 4);
        }

        private void BuildVillageBackdrop()
        {
            InstantiateVillageAsset("mountain_A_grass_trees", new Vector3(-42f, -1f, 48f), 18f, 30f, mutedGreen);
            InstantiateVillageAsset("mountain_B_grass_trees", new Vector3(18f, -2f, 54f), -22f, 35f, mutedGreen);
            InstantiateVillageAsset("mountain_A_grass_trees", new Vector3(57f, -2f, 34f), -48f, 27f, mutedGreen);
            InstantiateVillageAsset("building_home_A_red", new Vector3(-25f, 0f, -1f), 24f, 8.2f, distantSilhouette);
            InstantiateVillageAsset("building_home_B_red", new Vector3(25f, 0f, 6f), -28f, 7.4f, distantSilhouette);
            InstantiateVillageAsset("building_watermill_red", new Vector3(-31f, 0f, 22f), 42f, 8.8f, distantSilhouette);
            InstantiateVillageAsset("trees_A_medium", new Vector3(-18f, 0f, 16f), 18f, 7.2f, mutedGreen);
            InstantiateVillageAsset("trees_A_medium", new Vector3(19f, 0f, 20f), -32f, 7.8f, mutedGreen);

            CreateDetailedTree(new Vector3(-8.8f, .05f, -6.0f), 1.0f, 0);
            CreateDetailedTree(new Vector3(8.6f, .05f, -8.1f), .88f, 1);
            CreateDetailedTree(new Vector3(-9.5f, .05f, 1.7f), .72f, 2);
            CreateDetailedTree(new Vector3(9.6f, .05f, 1.2f), .78f, 3);
        }

        private void BuildTwoStoreyHomestay()
        {
            Box("HomestayFoundation", new Vector3(0f, -.145f, RoomCenterZ), new Vector3(20.95f, .32f, RoomDepth + .45f), wetStone, true);
            for (int floor = 0; floor < 2; floor++) BuildStoreyShell(floor);
            BuildExteriorGalleryAndStair();
            BuildTraditionalRoof();
            BuildRearAndSideExteriorDetails();
            BuildFacadeDetailPass();

            for (int floor = 0; floor < 2; floor++)
            for (int room = 0; room < RoomCenters.Length; room++)
            {
                int roomNumber = (floor == 0 ? 101 : 201) + room;
                BuildGuestRoom(RoomCenters[room], floor, roomNumber, floor * 3 + room);
            }

            // The sixth room is intentionally a spare/archive room. Five occupied
            // rooms account for nine beds, making the absent tenth name legible.
            CreatePlaque("实践档案 · 203", new Vector3(6.90f, 7.22f, 5.01f), Quaternion.identity,
                new Vector2(1.62f, .38f), .030f, new Color(.96f, .72f, .33f));
            CreateReflectionProbe("CourtyardReflection", new Vector3(0f, 2.2f, -3.5f), new Vector3(28f, 7f, 20f));
            CreateReflectionProbe("GroundRoomsReflection", new Vector3(0f, 1.85f, RoomCenterZ), new Vector3(21f, 3.6f, RoomDepth));
            CreateReflectionProbe("UpperRoomsReflection", new Vector3(0f, 5.90f, RoomCenterZ), new Vector3(21f, 3.6f, RoomDepth));
            NormalizeRoomLayout();
        }

        private void BuildRearAndSideExteriorDetails()
        {
            Box("RearServiceBluestone", new Vector3(0f, -.018f, 19.25f), new Vector3(25.8f, .17f, 3.75f), wetStone, true);
            Box("WestSideBluestone", new Vector3(-12.15f, -.018f, 12.55f), new Vector3(3.10f, .17f, 16.8f), wetStone, true);
            Box("EastSideBluestone", new Vector3(12.15f, -.018f, 12.55f), new Vector3(3.10f, .17f, 16.8f), wetStone, true);

            for (int floor = 0; floor < 2; floor++)
            for (int room = 0; room < RoomCenters.Length; room++)
            {
                float baseY = floor * StoreyHeight;
                float x = RoomCenters[room];
                Box("RearBathroomWindowGlass", new Vector3(x, baseY + 2.46f, 17.425f), new Vector3(1.70f, .72f, .028f), frostedGlass ?? glass, false);
                Box("RearWindowTopFrame", new Vector3(x, baseY + 2.85f, 17.49f), new Vector3(1.94f, .10f, .11f), deepWood, false);
                Box("RearWindowBottomFrame", new Vector3(x, baseY + 2.07f, 17.49f), new Vector3(1.94f, .10f, .11f), deepWood, false);
                // Vertical rails fit between the horizontal rails instead of
                // overlapping them at four identical-depth corner patches.
                Box("RearWindowLeftFrame", new Vector3(x - .90f, baseY + 2.46f, 17.49f), new Vector3(.10f, .68f, .11f), deepWood, false);
                Box("RearWindowRightFrame", new Vector3(x + .90f, baseY + 2.46f, 17.49f), new Vector3(.10f, .68f, .11f), deepWood, false);
                Box("RearWindowMullion", new Vector3(x, baseY + 2.46f, 17.51f), new Vector3(.065f, .68f, .055f), brass, false);
                CreateWallDecal("RearWallDampBloom", new Vector3(RoomCenters[room] - 1.55f, baseY + 1.25f, 17.405f),
                    Quaternion.Euler(0f, 180f, 0f), new Vector2(.86f, 1.18f), (room + floor) % 4, 2, 4, 4);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                CylinderByDimensions("RearRainDownpipe", new Vector3(side * 9.72f, 3.92f, 17.64f), .105f, 7.65f, blackMetal, false);
                Box("RainwaterCollector", new Vector3(side * 9.72f, .48f, 17.80f), new Vector3(.48f, .72f, .42f), blackMetal, false);
                for (int i = 0; i < 4; i++)
                    Box("SideWallTimberButtress", new Vector3(side * 10.53f, 2.04f, 6.9f + i * 2.85f),
                        new Vector3(.13f, 3.58f, .18f), deepWood, false);
            }

            for (int i = 0; i < 4; i++)
            {
                float x = -7.1f + i * 4.75f;
                RoundedBox("RearHerbPlanter", new Vector3(x, .35f, 17.92f), new Vector3(1.72f, .55f, .72f), oldWood, .08f, true);
                Box("PlanterSoil", new Vector3(x, .64f, 17.92f), new Vector3(1.52f, .055f, .55f), dampGround, false);
                for (int stem = -2; stem <= 2; stem++)
                {
                    CylinderByDimensions("KitchenHerbStem", new Vector3(x + stem * .27f, .86f + (stem & 1) * .07f, 17.92f),
                        .035f, .44f, mutedGreen, false);
                    Sphere("KitchenHerbLeaf", new Vector3(x + stem * .27f, 1.08f + (stem & 1) * .07f, 17.92f),
                        new Vector3(.24f, .12f, .28f), mutedGreen);
                }
            }

            RoundedBox("RearUtilityCabinet", new Vector3(-9.05f, .92f, 17.72f), new Vector3(1.12f, 1.54f, .44f), blackMetal, .035f, true);
            for (int i = 0; i < 3; i++)
                Sphere("UtilityMeter", new Vector3(-9.32f + i * .28f, 1.12f, 17.47f), Vector3.one * .105f, porcelain);
        }

        private void BuildStoreyShell(int floor)
        {
            float baseY = floor * StoreyHeight;
            Box(floor == 0 ? "GroundFloorContinuousSlab" : "SecondFloorContinuousSlab",
                new Vector3(0f, baseY + .03f, RoomCenterZ), new Vector3(BuildingHalfWidth * 2f, .18f, RoomDepth), floorWood, true);
            Box("StoreyCeiling", new Vector3(0f, baseY + 3.78f, RoomCenterZ), new Vector3(BuildingHalfWidth * 2f, .18f, RoomDepth), oldWood, true);
            BuildRearWallWithBathroomWindows(baseY);
            Box("LeftStructuralWall", new Vector3(-BuildingHalfWidth, baseY + 1.9f, RoomCenterZ), new Vector3(.28f, 3.8f, RoomDepth + .25f), plaster, true);
            Box("RightStructuralWall", new Vector3(BuildingHalfWidth, baseY + 1.9f, RoomCenterZ), new Vector3(.28f, 3.8f, RoomDepth + .25f), plaster, true);
            Box("PartitionWallA", new Vector3(-3.45f, baseY + 1.9f, RoomCenterZ), new Vector3(.20f, 3.8f, RoomDepth), plaster, true);
            Box("PartitionWallB", new Vector3(3.45f, baseY + 1.9f, RoomCenterZ), new Vector3(.20f, 3.8f, RoomDepth), plaster, true);
            Box("RearStoneBase", new Vector3(0f, baseY + .38f, BuildingBack - .15f), new Vector3(BuildingHalfWidth * 2f - .20f, .76f, .10f), wetStone, false);

            for (int room = 0; room < RoomCenters.Length; room++)
                BuildRoomFacade(RoomCenters[room], baseY, (floor == 0 ? 101 : 201) + room);
        }

        private void BuildRearWallWithBathroomWindows(float baseY)
        {
            // The original rear wall was one solid cube with glass placed in
            // front of it. It looked opaque and also produced overlapping faces.
            // Build a genuine wall opening: two continuous horizontal bands and
            // four non-overlapping piers between the three bathroom windows.
            const float windowHalfWidth = .85f;
            const float windowBottom = 2.10f;
            const float windowTop = 2.82f;
            const float wallHeight = 3.80f;
            Box("RearStructuralWallLower", new Vector3(0f, baseY + windowBottom * .5f, BuildingBack),
                new Vector3(BuildingHalfWidth * 2f + .05f, windowBottom, .28f), plaster, true);
            Box("RearStructuralWallUpper", new Vector3(0f, baseY + (windowTop + wallHeight) * .5f, BuildingBack),
                new Vector3(BuildingHalfWidth * 2f + .05f, wallHeight - windowTop, .28f), plaster, true);

            float[] starts = { -BuildingHalfWidth, RoomCenters[0] + windowHalfWidth,
                RoomCenters[1] + windowHalfWidth, RoomCenters[2] + windowHalfWidth };
            float[] ends = { RoomCenters[0] - windowHalfWidth, RoomCenters[1] - windowHalfWidth,
                RoomCenters[2] - windowHalfWidth, BuildingHalfWidth };
            for (int i = 0; i < starts.Length; i++)
            {
                float width = ends[i] - starts[i];
                if (width <= .01f) continue;
                Box("RearStructuralPier", new Vector3((starts[i] + ends[i]) * .5f,
                        baseY + (windowBottom + windowTop) * .5f, BuildingBack),
                    new Vector3(width, windowTop - windowBottom, .28f), plaster, true);
            }
        }

        private void BuildRoomFacade(float roomX, float baseY, int roomNumber)
        {
            const float half = RoomHalfWidth;
            const float doorCenter = -1.55f;
            const float doorWidth = 1.24f;
            const float windowCenter = 1.12f;
            const float windowWidth = 1.68f;
            float doorLeft = doorCenter - doorWidth * .5f;
            float doorRight = doorCenter + doorWidth * .5f;
            float windowLeft = windowCenter - windowWidth * .5f;
            float windowRight = windowCenter + windowWidth * .5f;

            CreateFacadeSegment(roomX - half, roomX + doorLeft, baseY, "FacadeLeftPier");
            CreateFacadeSegment(roomX + doorRight, roomX + windowLeft, baseY, "FacadeMiddlePier");
            CreateFacadeSegment(roomX + windowRight, roomX + half, baseY, "FacadeRightPier");
            Box("FacadeWindowApron", new Vector3(roomX + windowCenter, baseY + .54f, BuildingFront),
                new Vector3(windowWidth, 1.08f, .28f), plaster, true);
            Box("FacadeContinuousHeader", new Vector3(roomX, baseY + 3.39f, BuildingFront),
                new Vector3(RoomHalfWidth * 2f, .78f, .28f), plaster, true);

            float doorBase = baseY + .24f;
            CreateDoorFrame(new Vector3(roomX + doorCenter, doorBase, BuildingFront - .15f), doorWidth, 2.34f);
            CreateOutwardOpeningDoor(new Vector3(roomX + doorCenter, doorBase, BuildingFront - .24f), doorWidth, 2.30f,
                84f, (roomNumber & 1) == 1);
            CreateDeepWindow(new Vector3(roomX + windowCenter, baseY + 2.02f, BuildingFront - .16f), new Vector2(windowWidth, 1.62f));
            CreatePlaque(roomNumber.ToString(), new Vector3(roomX - .50f, baseY + 2.34f, BuildingFront - .37f), Quaternion.identity,
                new Vector2(.62f, .30f), .030f, new Color(1f, .73f, .31f));
            CreateWallDecal("FacadeWeathering", new Vector3(roomX + 2.55f, baseY + 1.26f, BuildingFront - .155f), Quaternion.identity,
                new Vector2(.82f, 1.15f), roomNumber % 4, 2, 4, 4);
        }

        private void CreateFacadeSegment(float startX, float endX, float baseY, string label)
        {
            float width = endX - startX;
            if (width <= .02f) return;
            // Piers end exactly below the continuous header. Previously the
            // full-height piers occupied the same facade plane beneath it.
            const float pierHeight = 2.90f;
            Box(label, new Vector3((startX + endX) * .5f, baseY + pierHeight * .5f, BuildingFront),
                new Vector3(width, pierHeight, .28f), plaster, true);
            Box("FacadeTimberEdge", new Vector3(startX + .08f, baseY + pierHeight * .5f, BuildingFront - .17f),
                new Vector3(.14f, pierHeight - .18f, .12f), deepWood, false);
        }

        private void BuildExteriorGalleryAndStair()
        {
            Box("GroundFloorExteriorGallery", new Vector3(0f, .12f, 4.12f), new Vector3(21.5f, .22f, 2.25f), floorWood, true);
            Box("SecondFloorExteriorGallery", new Vector3(0f, 4.12f, 4.12f), new Vector3(21.5f, .24f, 2.25f), floorWood, true);
            // Align load-bearing posts with room bays instead of distributing them
            // uniformly across door approaches. The earlier 1.9 m spacing placed
            // three posts within a player-capsule radius of guest-room centre lines.
            float[] galleryPostX = { -6.82f, -3.42f, 3.42f, 6.82f };
            foreach (float x in galleryPostX)
            {
                Box("GalleryColumn", new Vector3(x, 2.08f, 2.90f), new Vector3(.16f, 4.12f, .16f), deepWood, true);
                Box("GalleryUpperPost", new Vector3(x, 6.15f, 2.90f), new Vector3(.14f, 3.65f, .14f), deepWood, false);
            }
            Box("GalleryBeam", new Vector3(0f, 3.92f, 2.90f), new Vector3(21.65f, .22f, .22f), deepWood, false);
            Box("UpperGalleryBeam", new Vector3(0f, 7.84f, 2.90f), new Vector3(21.65f, .22f, .22f), deepWood, false);

            // A single continuous rail leaves only the west stair landing open.
            // It sits far enough from the outward-swinging doors for a 0.64 m
            // character capsule to pass with comfortable clearance.
            BuildRailingRun(1.18f, 18.34f, 4.12f, 2.86f);
            BuildExteriorStaircase();

            CreatePlaque("民宿", new Vector3(0f, 7.48f, 3.48f), Quaternion.identity,
                new Vector2(1.85f, .58f), .066f, new Color(1f, .72f, .29f));
        }

        private void BuildFacadeDetailPass()
        {
            // A second facade pass adds weather protection, room identity and
            // small planted details so the exterior reads as a lived-in homestay.
            for (int floor = 0; floor < 2; floor++)
            for (int room = 0; room < RoomCenters.Length; room++)
            {
                float baseY = floor * StoreyHeight;
                float roomX = RoomCenters[room];
                float front = BuildingFront - .38f;
                float doorX = roomX - 1.55f;
                float windowX = roomX + 1.12f;

                GameObject canopy = Box("GuestDoorRainCanopy", new Vector3(doorX, baseY + 2.63f, front),
                    new Vector3(1.72f, .10f, .72f), roofTiles, false);
                canopy.transform.rotation = Quaternion.Euler(-8f, 0f, 0f);
                Box("GuestDoorCanopyBracketL", new Vector3(doorX - .68f, baseY + 2.38f, front + .12f),
                    new Vector3(.08f, .48f, .08f), deepWood, false).transform.rotation = Quaternion.Euler(0f, 0f, -28f);
                Box("GuestDoorCanopyBracketR", new Vector3(doorX + .68f, baseY + 2.38f, front + .12f),
                    new Vector3(.08f, .48f, .08f), deepWood, false).transform.rotation = Quaternion.Euler(0f, 0f, 28f);

                RoundedBox("FacadeWindowFlowerBox", new Vector3(windowX, baseY + .98f, front),
                    new Vector3(1.92f, .20f, .46f), oldWood, .06f, false);
                RoundedBox("FacadeWindowFlowerSoil", new Vector3(windowX, baseY + 1.10f, front - .02f),
                    new Vector3(1.66f, .055f, .32f), dampGround, .03f, false);
                for (int stem = -2; stem <= 2; stem++)
                {
                    Vector3 plant = new(windowX + stem * .31f, baseY + 1.30f + (Mathf.Abs(stem) % 2) * .05f, front - .04f);
                    CylinderByDimensions("FacadeFlowerStem", plant, .028f, .34f, mutedGreen, false);
                    Sphere("FacadeFlowerLeaf", plant + Vector3.up * .18f, new Vector3(.16f, .10f, .12f), leaf);
                }

                RoundedBox("GuestDoorLanternFrame", new Vector3(doorX + .78f, baseY + 2.22f, front - .10f),
                    new Vector3(.26f, .40f, .16f), blackMetal, .035f, false);
                RoundedBox("GuestDoorLanternGlow", new Vector3(doorX + .78f, baseY + 2.22f, front - .20f),
                    new Vector3(.16f, .24f, .08f), warmGlow, .025f, false);
                CreatePointLight(new Vector3(doorX + .78f, baseY + 2.12f, front - .42f),
                    new Color(1f, .48f, .20f), .34f, 2.4f);
            }

            // Keep decorative braces on solid plaster bays only. The former row
            // of large X braces crossed every window and shared depth with the
            // frames, so their intersections shimmered as the camera moved.
            float[] braceOffsets = { -2.76f, -.32f, 2.65f };
            for (int room = 0; room < RoomCenters.Length; room++)
            for (int bay = 0; bay < braceOffsets.Length; bay++)
            {
                float x = RoomCenters[room] + braceOffsets[bay];
                Box("FacadeBayBrace", new Vector3(x, 1.72f, BuildingFront - .305f),
                    new Vector3(.06f, 1.18f, .055f), deepWood, false).transform.rotation =
                    Quaternion.Euler(0f, 0f, bay % 2 == 0 ? 22f : -22f);
            }
            Box("EntryThresholdStep", new Vector3(0f, .16f, BuildingFront - .54f), new Vector3(4.60f, .18f, .62f), wetStone, true);
            Box("EntryThresholdStepUpper", new Vector3(0f, .28f, BuildingFront - .30f), new Vector3(3.85f, .08f, .34f), slate, false);
        }

        private void BuildRailingRun(float centerX, float width, float floorY, float z)
        {
            Box("BalconyTopRail", new Vector3(centerX, floorY + 1.02f, z), new Vector3(width, .12f, .14f), deepWood, true);
            Box("BalconyMidRail", new Vector3(centerX, floorY + .50f, z), new Vector3(width, .08f, .10f), oldWood, false);
            int posts = Mathf.Max(2, Mathf.CeilToInt(width / .72f));
            for (int i = 0; i <= posts; i++)
            {
                float x = centerX - width * .5f + width * i / posts;
                Box("BalconyBaluster", new Vector3(x, floorY + .56f, z), new Vector3(.075f, .96f, .09f), deepWood, false);
            }
        }

        private void BuildExteriorStaircase()
        {
            const int steps = 20;
            const float rise = StoreyHeight / steps;
            const float run = .34f;
            // The final tread meets the gallery's outer edge. Keeping the earlier
            // start position would run four steps underneath the gallery collider.
            const float startZ = -4.15f;
            const float x = -11.05f;
            for (int i = 0; i < steps; i++)
            {
                float height = rise * (i + 1);
                // Fixed-thickness independent treads replace the former stacked
                // full-height cubes. Those cubes shared large coplanar side faces
                // and visibly shimmered while the player climbed the stair.
                Box("ExteriorStairTread", new Vector3(x, height - .085f, startZ + i * run),
                    new Vector3(1.72f, .17f, run - .012f), stairTread ?? floorWood, true);
                Box("ExteriorStairRunner", new Vector3(x, height + .006f, startZ + i * run),
                    new Vector3(.72f, .012f, run - .040f), stairRunner ?? softBlue, false);
                Box("ExteriorStairNosing", new Vector3(x, height + .012f, startZ + i * run - run * .46f),
                    new Vector3(1.76f, .035f, .035f), brass, false);
                if (i % 3 == 0)
                {
                    CylinderByDimensions("StairRailPost", new Vector3(x - .91f, height + .58f, startZ + i * run), .09f, 1.16f, deepWood, false);
                    CylinderByDimensions("StairRailPost", new Vector3(x + .91f, height + .58f, startZ + i * run), .09f, 1.16f, deepWood, false);
                }
            }
            Vector3 bottomLeft = new(x - .91f, .72f, startZ);
            Vector3 topLeft = new(x - .91f, StoreyHeight + .72f, startZ + (steps - 1) * run);
            Vector3 bottomRight = new(x + .91f, .72f, startZ);
            Vector3 topRight = new(x + .91f, StoreyHeight + .72f, startZ + (steps - 1) * run);
            BeamBetween("StairHandrail", bottomLeft, topLeft, .07f, deepWood, false);
            BeamBetween("StairHandrail", bottomRight, topRight, .07f, deepWood, false);
            BeamBetween("StairStringerLeft", new Vector3(x - .72f, .08f, startZ - .08f),
                new Vector3(x - .72f, StoreyHeight - .14f, startZ + (steps - 1) * run), .15f, blackMetal, false);
            BeamBetween("StairStringerRight", new Vector3(x + .72f, .08f, startZ - .08f),
                new Vector3(x + .72f, StoreyHeight - .14f, startZ + (steps - 1) * run), .15f, blackMetal, false);
            // Front edge starts immediately after the final tread. This removes the
            // low overhang that previously intercepted the capsule on step 19.
            Box("StairTopLanding", new Vector3(-10.48f, 4.10f, 2.83f), new Vector3(3.15f, .24f, .65f), floorWood, true);
            RoundedBox("StairLandingRunner", new Vector3(-10.48f, 4.235f, 2.83f),
                new Vector3(1.72f, .025f, .48f), stairRunner ?? softBlue, .05f, false);
            CreateCourtyardLantern(new Vector3(-12.18f, .02f, -2.90f));
        }

        private void BuildTraditionalRoof()
        {
            GameObject frontRoof = Box("FrontRoofSlope", new Vector3(0f, 8.50f, 6.55f), new Vector3(22.8f, .26f, 8.45f), roofTiles, false);
            frontRoof.transform.rotation = Quaternion.Euler(-14.5f, 0f, 0f);
            GameObject rearRoof = Box("RearRoofSlope", new Vector3(0f, 8.50f, 14.35f), new Vector3(22.8f, .26f, 7.25f), roofTiles, false);
            rearRoof.transform.rotation = Quaternion.Euler(14.5f, 0f, 0f);
            CylinderByDimensions("RoofRidge", new Vector3(0f, 9.28f, 11.35f), .26f, 23.0f, roofTiles, false, Quaternion.Euler(0f, 0f, 90f));
            for (int i = 0; i < 28; i++)
            {
                float x = -10.8f + i * .80f;
                Box("FrontRoofTileRib", new Vector3(x, 8.61f, 6.55f), new Vector3(.055f, .055f, 8.52f), deepWood, false)
                    .transform.rotation = Quaternion.Euler(-14.5f, 0f, 0f);
                Box("RearRoofTileRib", new Vector3(x, 8.61f, 14.35f), new Vector3(.055f, .055f, 7.32f), deepWood, false)
                    .transform.rotation = Quaternion.Euler(14.5f, 0f, 0f);
            }
            Box("FrontGutter", new Vector3(0f, 7.48f, 2.12f), new Vector3(22.9f, .16f, .18f), blackMetal, false);
            Box("RearGutter", new Vector3(0f, 7.78f, 18.02f), new Vector3(22.9f, .16f, .18f), blackMetal, false);
            CylinderByDimensions("RainChain", new Vector3(10.55f, 3.84f, 2.14f), .055f, 7.35f, blackMetal, false);
        }

        private void BuildGuestRoom(float roomX, int floor, int roomNumber, int roomIndex)
        {
            float baseY = floor * StoreyHeight;
            CreateRoomFloorDetails(roomX, baseY);
            CreateHotelHeadboardWall(roomX, baseY);
            // The hand-edited template rotates the bed into the front-right
            // bedroom bay. Its headboard rests against the right partition while
            // the rear quarter remains a clean, continuous bathroom zone.
            CreateBed(roomX + 2.12f, baseY, 8.40f, 2.55f, 90f,
                roomNumber == 101 ? "BedFrame" : "DoubleGuestBed", roomNumber == 101, roomNumber);
            CreateRoomTelevision(roomX, baseY, roomNumber);
            CreateRoomWardrobe(roomX, baseY, roomNumber);
            CreateAirConditioner(roomX, baseY);
            CreateIndependentBathroom(roomX, baseY, roomNumber);
            CreateRoomLighting(roomX, baseY, roomNumber);

            CreateGuestAmenities(roomX, baseY, roomIndex, false, roomNumber);
            CreateRoomDecorPass(roomX, baseY, roomNumber, roomIndex);
        }

        private void CreateRoomDecorPass(float roomX, float baseY, int roomNumber, int variant)
        {
            // Framed art sits on the bedroom face of the bathroom partition with a
            // deliberate 28 mm stand-off; it never shares a plane with the wall.
            int artColumn = roomNumber % 3;
            int artRow = (roomNumber / 100) % 2;
            CreateDecorPanel($"GuestRoomArtwork_{roomNumber}",
                new Vector3(roomX + .90f, baseY + 2.35f, 12.936f), Quaternion.identity,
                new Vector2(1.18f, .72f), artColumn, artRow, 3, 3);

            // Small, modeled personal belongings make every room feel occupied
            // without reintroducing a desk or narrowing the main route.
            Vector3 basket = new(roomX - 2.80f, baseY + .26f, 5.56f);
            CylinderByDimensions("GuestUmbrellaBasket", basket, .34f, .50f, deepWood, false);
            for (int i = 0; i < 3; i++)
            {
                Vector3 bottom = basket + new Vector3(-.09f + i * .09f, .20f, 0f);
                Vector3 top = bottom + new Vector3((i - 1) * .04f, .68f + i * .06f, (i % 2) * .05f);
                BeamBetween("GuestUmbrella", bottom, top, .035f, i == 1 ? brass : blackMetal, false);
                Sphere("UmbrellaHandle", top, Vector3.one * .075f, i == 1 ? brass : deepWood);
            }

            Vector3 waste = new(roomX + 2.72f, baseY + .24f, 11.72f);
            CylinderByDimensions("WovenWasteBasket", waste, .38f, .46f, oldWood, false);
            CylinderByDimensions("WasteBasketInner", waste + Vector3.up * .22f, .30f, .025f, blackMetal, false);

            RoundedBox("BedsideTissueBox", new Vector3(roomX + .37f, baseY + .92f, 9.62f),
                new Vector3(.25f, .13f, .18f), linen, .045f, false);
            RoundedBox("BedsideTissue", new Vector3(roomX + .37f, baseY + 1.03f, 9.62f),
                new Vector3(.08f, .12f, .04f), softWhite, .06f, false);
            for (int i = 0; i < 2; i++)
                RoundedBox("GuestReadingBook", new Vector3(roomX + .53f, baseY + .93f + i * .045f, 7.12f),
                    new Vector3(.26f, .035f, .18f), i == 0 ? softBlue : linen, .035f, false)
                    .transform.rotation = Quaternion.Euler(0f, -8f + variant * 3f, 0f);
        }

        private void CreateRoomFloorDetails(float roomX, float baseY)
        {
            Box("RoomSkirtingLeft", new Vector3(roomX - 3.25f, baseY + .16f, RoomCenterZ), new Vector3(.08f, .22f, 11.55f), deepWood, false);
            Box("RoomSkirtingRight", new Vector3(roomX + 3.25f, baseY + .16f, RoomCenterZ), new Vector3(.08f, .22f, 11.55f), deepWood, false);
            Box("RoomSkirtingRear", new Vector3(roomX, baseY + .16f, BuildingBack - .23f), new Vector3(6.45f, .22f, .08f), deepWood, false);
            RoundedBox("BedsideRug", new Vector3(roomX + .70f, baseY + .135f, 8.40f), new Vector3(4.80f, .035f, 4.90f), softBlue, .09f, false);
            for (int i = 0; i < 7; i++)
                Box("RugWeave", new Vector3(roomX + .70f, baseY + .158f, 6.35f + i * .55f), new Vector3(4.45f, .012f, .045f), brass, false);
        }

        private void CreateHotelHeadboardWall(float roomX, float baseY)
        {
            RoundedBox("HotelHeadboardFeatureWall", new Vector3(roomX + 3.18f, baseY + 1.52f, 8.40f),
                new Vector3(.10f, 2.72f, 4.62f), oldWood, .035f, false);
            for (int i = 0; i < 6; i++)
                RoundedBox("UpholsteredHeadboardPanel", new Vector3(roomX + 3.125f, baseY + 1.56f, 6.23f + i * .86f),
                    new Vector3(.055f, 1.42f, .74f), i % 2 == 0 ? softBlue : linen, .10f, false);
            Box("HeadboardWarmTrim", new Vector3(roomX + 3.11f, baseY + 2.30f, 8.40f), new Vector3(.035f, .055f, 4.48f), brass, false);
        }

        private void CreateBed(float x, float baseY, float z, float width, float yaw, string frameName, bool heroBed, int roomNumber)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 center = new(x, baseY + .34f, z);
            GameObject frame = BedRoundedBox(frameName, center, Vector3.zero, new Vector3(width + .16f, .38f, 2.30f), deepWood, .055f, true, rotation);
            ChapterThreeInteractable bed = frame.AddComponent<ChapterThreeInteractable>();
            bed.Configure(ChapterThreeInteractionKind.Bed, roomNumber, "选择睡眠时长", "1—12小时可自定义；睡满9小时可恢复100点理智");
            AddInteractionProxy(frame, Vector3.up * .74f, new Vector3(width + 1.05f, 1.55f, 3.05f), "BedInteractionProxy");
            BedRoundedBox("PocketSpringMattress", center, new Vector3(0f, .27f, 0f), new Vector3(width, .24f, 2.13f), softWhite, .12f, false, rotation);
            BedRoundedBox("IndigoQuilt", center, new Vector3(0f, .43f, -.24f), new Vector3(width - .07f, .16f, 1.52f), softBlue, .16f, false, rotation);
            BedRoundedBox("QuiltFold", center, new Vector3(0f, .52f, -.98f), new Vector3(width - .08f, .13f, .23f), linen, .12f, false, rotation);
            if (width > 1.5f)
            {
                BedRoundedBox("GuestPillow", center, new Vector3(-width * .23f, .48f, .82f), new Vector3(width * .43f, .18f, .48f), softWhite, .20f, false, rotation);
                BedRoundedBox("GuestPillow", center, new Vector3(width * .23f, .48f, .82f), new Vector3(width * .43f, .18f, .48f), softWhite, .20f, false, rotation);
            }
            else BedRoundedBox("GuestPillow", center, new Vector3(0f, .48f, .82f), new Vector3(width * .72f, .18f, .48f), softWhite, .20f, false, rotation);
            BedBox("BedHeadboard", center, new Vector3(0f, .83f, 1.11f), new Vector3(width + .14f, 1.22f, .12f), oldWood, true, rotation);
            for (int i = 0; i < 4; i++)
            {
                float px = -width * .36f + width * .24f * i;
                BedBox("HeadboardInset", center, new Vector3(px, .82f, 1.035f), new Vector3(.055f, .88f, .035f), deepWood, false, rotation);
            }
            for (int side = -1; side <= 1; side += 2)
            for (int end = -1; end <= 1; end += 2)
                BedBox("BedFrameLeg", center, new Vector3(side * width * .43f, -.14f, end * .96f), new Vector3(.09f, .35f, .09f), blackMetal, false, rotation);
            BedBox("MattressPipingLeft", center, new Vector3(-width * .50f, .33f, 0f), new Vector3(.025f, .035f, 2.02f), brass, false, rotation);
            BedBox("MattressPipingRight", center, new Vector3(width * .50f, .33f, 0f), new Vector3(.025f, .035f, 2.02f), brass, false, rotation);
            if (heroBed) BedBox("MossyBedsheetTrace", center, new Vector3(.27f, .535f, -.18f), new Vector3(.28f, .018f, .13f), leaf, false, rotation);
        }

        private GameObject BedBox(string name, Vector3 center, Vector3 localPosition, Vector3 scale, Material material, bool collider, Quaternion rotation)
        {
            GameObject value = Box(name, center + rotation * localPosition, scale, material, collider);
            value.transform.rotation = rotation;
            return value;
        }

        private GameObject BedRoundedBox(string name, Vector3 center, Vector3 localPosition, Vector3 scale, Material material, float bevel, bool collider, Quaternion rotation)
        {
            GameObject value = RoundedBox(name, center + rotation * localPosition, scale, material, bevel, collider);
            value.transform.rotation = rotation;
            return value;
        }

        private void CreateRoomTelevision(float roomX, float baseY, int roomNumber)
        {
            float x = roomX - 3.08f;
            GameObject television = RoundedBox("TelevisionHousing", new Vector3(x, baseY + 1.86f, 9.10f),
                new Vector3(.14f, .98f, 1.56f), blackMetal, .055f, true);
            television.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            ChapterThreeInteractable interactable = television.AddComponent<ChapterThreeInteractable>();
            interactable.Configure(ChapterThreeInteractionKind.Television, roomNumber, "切换电视", "播放或关闭实践队影像");
            AddInteractionProxy(television, Vector3.zero, new Vector3(.70f, 1.35f, 1.85f), "TelevisionInteractionProxy");
            GameObject televisionGlass = Box("TelevisionGlass", new Vector3(x, baseY + 1.86f, 9.10f), new Vector3(.022f, .66f, 1.14f), screen, false);
            televisionGlass.transform.SetParent(television.transform, false);
            // The housing is authored with its face on local +X (the carcass is
            // rotated 180 degrees in the room). Keep the display plane clearly
            // ahead of the rounded bezel so the bezel cannot occlude the video.
            // 1.14 / .64 is the exact 16:9 aspect of TVPractice.mp4.
            // This child inherits the housing's non-uniform scale (.14, .98, 1.56),
            // so author the local values with that scale compensated. The previous
            // x=1 assignment made the display a deep slab hidden inside the bezel.
            televisionGlass.transform.localPosition = new Vector3(-.62f, 0f, 0f);
            televisionGlass.transform.localScale = new Vector3(.022f / .14f, .64f / .98f, 1.14f / 1.56f);
            Renderer televisionRenderer = televisionGlass.GetComponent<Renderer>();
            televisionRenderer.shadowCastingMode = ShadowCastingMode.Off;
            televisionRenderer.receiveShadows = false;
            VideoPlayer video = television.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.isLooping = true;
            video.waitForFirstFrame = false;
            // Route the embedded practice-video track through a spatial source on
            // the TV. The earlier None mode decoded pictures only, so every set
            // appeared to work while remaining completely silent.
            AudioSource televisionAudio = television.AddComponent<AudioSource>();
            televisionAudio.playOnAwake = false;
            televisionAudio.loop = true;
            televisionAudio.volume = .78f;
            televisionAudio.spatialBlend = .72f;
            televisionAudio.minDistance = 1.4f;
            televisionAudio.maxDistance = 12f;
            televisionAudio.rolloffMode = AudioRolloffMode.Logarithmic;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.controlledAudioTrackCount = 1;
            video.EnableAudioTrack(0, true);
            video.SetTargetAudioSource(0, televisionAudio);
            video.renderMode = VideoRenderMode.MaterialOverride;
            video.aspectRatio = VideoAspectRatio.FitInside;
            video.targetCameraAlpha = 1f;
            video.skipOnDrop = true;
            video.targetMaterialRenderer = televisionGlass.GetComponent<Renderer>();
            video.targetMaterialProperty = "_BaseMap";
            video.clip = Resources.Load<VideoClip>("ChapterThree/TVPractice");
            // A thin emissive scan bar gives the television a readable power-on
            // animation before the first video frame is ready.
            GameObject bootLine = ChildBox("TelevisionBootLine", television.transform,
                new Vector3(-.64f, -.24f, 0f), new Vector3(.012f / .14f, .018f / .98f, 1.08f / 1.56f), coolGlow, false);
            bootLine.SetActive(false);
            Box("TelevisionStand", new Vector3(x + .22f, baseY + .59f, 9.10f), new Vector3(.40f, .68f, 1.78f), deepWood, true);
            for (int i = 0; i < 2; i++)
                Box("TvConsoleDrawer", new Vector3(x + .435f, baseY + .48f + i * .25f, 9.10f), new Vector3(.035f, .17f, 1.48f), oldWood, false);
            for (int i = -2; i <= 2; i++)
                Box("TvSpeakerSlot", new Vector3(x - .105f, baseY + 1.29f, 9.10f + i * .15f), new Vector3(.018f, .030f, .075f), brass, false);
            Sphere("TvStandbyLight", new Vector3(x + .108f, baseY + 1.45f, 9.68f), Vector3.one * .030f, coolGlow);
        }

        private void CreateRoomDesk(float roomX, float baseY, int roomNumber)
        {
            RoundedBox("WritingDeskTop", new Vector3(roomX + 1.74f, baseY + .78f, 6.75f), new Vector3(1.72f, .10f, .68f), oldWood, .055f, true);
            for (int side = -1; side <= 1; side += 2)
                Box("WritingDeskLeg", new Vector3(roomX + 1.74f + side * .70f, baseY + .39f, 6.75f), new Vector3(.09f, .78f, .52f), deepWood, true);
            RoundedBox("DeskChairSeat", new Vector3(roomX + 1.74f, baseY + .47f, 7.62f), new Vector3(.60f, .12f, .54f), softBlue, .11f, true);
            Box("DeskChairBack", new Vector3(roomX + 1.74f, baseY + .90f, 7.92f), new Vector3(.59f, .78f, .10f), deepWood, true);
            for (int side = -1; side <= 1; side += 2)
                Box("DeskChairLeg", new Vector3(roomX + 1.74f + side * .23f, baseY + .23f, 7.62f), new Vector3(.07f, .46f, .07f), deepWood, false);
            RoundedBox("WelcomeFolder", new Vector3(roomX + 1.42f, baseY + .86f, 6.68f), new Vector3(.52f, .035f, .36f), roomNumber % 2 == 0 ? linen : softBlue, .07f, false)
                .transform.rotation = Quaternion.Euler(0f, -12f, 0f);
            CreateCompactKettleSet(new Vector3(roomX + 2.16f, baseY + .94f, 6.72f));
            GameObject informationStand = RoundedBox("HotelInformationStand", new Vector3(roomX + 1.78f, baseY + 1.08f, 6.98f),
                new Vector3(.42f, .36f, .045f), linen, .045f, false);
            informationStand.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
            Texture2D informationTexture = Resources.Load<Texture2D>("ChapterThree/RoomInformationCard");
            if (informationTexture != null)
            {
                GameObject sticker = GameObject.CreatePrimitive(PrimitiveType.Quad);
                sticker.name = $"RoomInformationSticker_{roomNumber}";
                sticker.transform.SetParent(informationStand.transform, false);
                sticker.transform.localPosition = new Vector3(0f, 0f, -.56f);
                sticker.transform.localRotation = Quaternion.identity;
                // Compensate the stand's non-uniform scale so the source card keeps
                // its portrait aspect ratio instead of becoming another flat block.
                sticker.transform.localScale = new Vector3(.54f, .92f, 1f);
                Renderer stickerRenderer = sticker.GetComponent<Renderer>();
                stickerRenderer.sharedMaterial = UnlitTextureMaterial($"RoomInformationCard_{roomNumber}", informationTexture, Color.white);
                stickerRenderer.shadowCastingMode = ShadowCastingMode.Off;
                stickerRenderer.receiveShadows = false;
                stickerRenderer.sortingOrder = 9;
                RemoveCollider(sticker.GetComponent<Collider>());
            }
            Box("DeskCableGrommet", new Vector3(roomX + 1.08f, baseY + .855f, 6.75f), new Vector3(.13f, .025f, .13f), blackMetal, false);
        }

        private void CreateRoomWardrobe(float roomX, float baseY, int roomNumber)
        {
            GameObject wardrobe = new("InteractiveWardrobe");
            wardrobe.transform.SetParent(root, false);
            // Beside the rotated bed, clear of the television wall and the
            // entry lane. The same position is reused in every room.
            wardrobe.transform.position = new Vector3(roomX - 2.70f, baseY, 7.18f);
            // The bed sits to the wardrobe's +X side. Doors are authored on local
            // -Z, therefore a -90 degree turn makes the entire wardrobe face the
            // bed while keeping its depth against the left wall.
            wardrobe.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            BoxCollider interactionProxy = wardrobe.AddComponent<BoxCollider>();
            interactionProxy.center = new Vector3(0f, 1.28f, -.47f);
            interactionProxy.size = new Vector3(1.42f, 2.52f, .20f);
            interactionProxy.isTrigger = true;
            ChapterThreeInteractable interactable = wardrobe.AddComponent<ChapterThreeInteractable>();
            interactable.Configure(ChapterThreeInteractionKind.Wardrobe, roomNumber, "开合衣柜", "检查衣柜和行李架");
            // The carcass is visual only; a separate front proxy handles the
            // interaction so a clue inside the wardrobe can be reached when open.
            // Build a real open-front carcass instead of one solid cuboid. The
            // solid version visually sealed the cupboard even after both doors
            // opened, so evidence parented inside it could never be seen.
            ChildBox("WardrobeBackPanel", wardrobe.transform, new Vector3(0f, 1.28f, .34f), new Vector3(1.28f, 2.42f, .08f), deepWood, false);
            ChildBox("WardrobeLeftSide", wardrobe.transform, new Vector3(-.61f, 1.28f, 0f), new Vector3(.08f, 2.42f, .68f), deepWood, false);
            ChildBox("WardrobeRightSide", wardrobe.transform, new Vector3(.61f, 1.28f, 0f), new Vector3(.08f, 2.42f, .68f), deepWood, false);
            ChildRoundedBox("WardrobeTop", wardrobe.transform, new Vector3(0f, 2.47f, 0f), new Vector3(1.28f, .10f, .76f), deepWood, .025f, false);
            ChildRoundedBox("WardrobeBottom", wardrobe.transform, new Vector3(0f, .09f, 0f), new Vector3(1.28f, .12f, .76f), deepWood, .025f, false);
            GameObject leftPivot = new("WardrobeDoorLeftPivot");
            leftPivot.transform.SetParent(wardrobe.transform, false);
            leftPivot.transform.localPosition = new Vector3(-.61f, 0f, -.41f);
            ChildBox("WardrobeDoorLeft", leftPivot.transform, new Vector3(.30f, 1.28f, 0f), new Vector3(.58f, 2.23f, .075f), oldWood, false);
            GameObject leftHandle = ChildSphere("WardrobeHandleLeft", leftPivot.transform, new Vector3(.51f, 1.28f, -.065f), Vector3.one * .045f, brass);
            // A trigger on the unscaled hinge pivot follows the opened leaf. The
            // former collider lived on the 4.5 cm handle scale, making its actual
            // hit radius only a few millimetres and effectively impossible to aim.
            BoxCollider leftCloseTarget = leftPivot.AddComponent<BoxCollider>();
            leftCloseTarget.center = new Vector3(.30f, 1.28f, -.02f);
            leftCloseTarget.size = new Vector3(.66f, 2.30f, .40f);
            leftCloseTarget.isTrigger = true;
            GameObject rightPivot = new("WardrobeDoorRightPivot");
            rightPivot.transform.SetParent(wardrobe.transform, false);
            rightPivot.transform.localPosition = new Vector3(.61f, 0f, -.41f);
            ChildBox("WardrobeDoorRight", rightPivot.transform, new Vector3(-.30f, 1.28f, 0f), new Vector3(.58f, 2.23f, .075f), oldWood, false);
            GameObject rightHandle = ChildSphere("WardrobeHandleRight", rightPivot.transform, new Vector3(-.51f, 1.28f, -.065f), Vector3.one * .045f, brass);
            BoxCollider rightCloseTarget = rightPivot.AddComponent<BoxCollider>();
            rightCloseTarget.center = new Vector3(-.30f, 1.28f, -.02f);
            rightCloseTarget.size = new Vector3(.66f, 2.30f, .40f);
            rightCloseTarget.isTrigger = true;
            ChildBox("WardrobeTopShelf", wardrobe.transform, new Vector3(0f, 2.05f, -.02f), new Vector3(1.05f, .07f, .60f), oldWood, false);
            ChildBox("WardrobeHangingRail", wardrobe.transform, new Vector3(0f, 1.68f, -.04f), new Vector3(1.00f, .055f, .055f), brass, false);
            for (int i = -1; i <= 1; i++)
                ChildBox("WoodenCoatHanger", wardrobe.transform, new Vector3(i * .30f, 1.48f, -.05f), new Vector3(.42f, .035f, .06f), oldWood, false);
            ChildRoundedBox("WardrobeDrawerLeft", wardrobe.transform, new Vector3(-.28f, .37f, -.10f), new Vector3(.47f, .31f, .48f), oldWood, .035f, false);
            ChildRoundedBox("WardrobeDrawerRight", wardrobe.transform, new Vector3(.28f, .37f, -.10f), new Vector3(.47f, .31f, .48f), oldWood, .035f, false);
            ChildSphere("WardrobeDrawerKnobLeft", wardrobe.transform, new Vector3(-.28f, .39f, -.36f), Vector3.one * .035f, brass);
            ChildSphere("WardrobeDrawerKnobRight", wardrobe.transform, new Vector3(.28f, .39f, -.36f), Vector3.one * .035f, brass);
            ChildRoundedBox("FoldedGuestLinen", wardrobe.transform, new Vector3(-.25f, 2.16f, -.04f), new Vector3(.44f, .10f, .42f), linen, .055f, false);
            ChildRoundedBox("FoldedGuestLinen", wardrobe.transform, new Vector3(.25f, 2.16f, -.04f), new Vector3(.44f, .10f, .42f), softBlue, .055f, false);
            const float luggageZ = 6.10f;
            RoundedBox("LuggageBench", new Vector3(roomX - 2.70f, baseY + .42f, luggageZ), new Vector3(1.08f, .38f, .46f), oldWood, .04f, true);
            for (int i = 0; i < 4; i++) Box("LuggageBenchSlat", new Vector3(roomX - 3.06f + i * .24f, baseY + .64f, luggageZ), new Vector3(.17f, .06f, .42f), deepWood, false);
            RoundedBox("LuggageCase", new Vector3(roomX - 2.70f, baseY + .78f, luggageZ), new Vector3(.78f, .32f, .34f), softBlue, .07f, false);
            Box("LuggageCaseBand", new Vector3(roomX - 2.70f, baseY + .80f, luggageZ), new Vector3(.10f, .36f, .36f), brass, false);
            Box("LuggageCaseHandle", new Vector3(roomX - 2.70f, baseY + 1.00f, luggageZ), new Vector3(.32f, .055f, .055f), blackMetal, false);
            for (int side = -1; side <= 1; side += 2)
                CylinderByDimensions("LuggageCaseWheel", new Vector3(roomX - 2.70f + side * .26f, baseY + .60f, luggageZ - .12f), .055f, .035f, blackMetal, false,
                    Quaternion.Euler(90f, 0f, 0f));
        }

        private void CreateAirConditioner(float roomX, float baseY)
        {
            GameObject unit = RoundedBox("AirConditioner", new Vector3(roomX + 1.26f, baseY + 3.12f, 5.56f), new Vector3(1.56f, .39f, .34f), softWhite, .12f, true);
            ChapterThreeInteractable ac = unit.AddComponent<ChapterThreeInteractable>();
            for (int i = 0; i < 7; i++)
                Box("AirConditionerVent", new Vector3(roomX + .70f + i * .19f, baseY + 2.96f, 5.35f), new Vector3(.11f, .035f, .025f), blackMetal, false);
            Box("AcStatusDisplay", new Vector3(roomX + 1.81f, baseY + 3.12f, 5.36f), new Vector3(.23f, .09f, .018f), coolGlow, false);
            GameObject flap = RoundedBox("AirConditionerFlap", new Vector3(roomX + 1.26f, baseY + 2.94f, 5.36f),
                new Vector3(1.18f, .055f, .065f), softWhite, .03f, false);
            flap.transform.SetParent(unit.transform, true);
            GameObject coldAir = new("AirConditionerColdAir");
            coldAir.transform.SetParent(unit.transform, true);
            coldAir.transform.position = new Vector3(roomX + 1.26f, baseY + 2.86f, 5.28f);
            // The unit is mounted on the front wall; local/world +Z points into
            // the bedroom. The old 180-degree yaw visibly blew through the wall.
            coldAir.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            ParticleSystem particles = coldAir.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.82f, 1.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.62f, 1.02f);
            main.startSize = new ParticleSystem.MinMaxCurve(.045f, .105f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(.62f, .92f, 1f, .18f), new Color(.88f, .98f, 1f, .05f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 110;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 32f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.08f, .05f, .025f);
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = new ParticleSystem.MinMaxCurve(-.18f, -.34f);
            velocity.z = new ParticleSystem.MinMaxCurve(.22f, .48f);
            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = coldAirParticleMaterial;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // Configure after all visual children exist so the interactable
            // caches the status display and animates its glow on each toggle.
            ac.Configure(ChapterThreeInteractionKind.AirConditioner, Mathf.RoundToInt(roomX * 10f) + Mathf.RoundToInt(baseY * 10f), "开关空调", "调节房间温度与送风状态");
            ac.SetToggleState(false);
            AddInteractionProxy(unit, Vector3.zero, new Vector3(2.30f, 1.20f, 1.05f), "AirConditionerInteractionProxy");
        }

        private void BuildPublicHomestayDetails()
        {
            GameObject teaTable = RoundedBox("CourtyardTeaTable", new Vector3(5.6f, .34f, -1.15f), new Vector3(1.65f, .12f, .88f), oldWood, .06f, true);
            ChapterThreeInteractable tea = teaTable.AddComponent<ChapterThreeInteractable>();
            tea.Configure(ChapterThreeInteractionKind.TeaService, 0, "茶水自取", "取一杯庭院茶水");
            CylinderByDimensions("CourtyardTeaTableLeg", new Vector3(5.6f, .12f, -1.15f), .12f, .32f, deepWood, true);
            CreateCompactKettleSet(new Vector3(5.6f, .52f, -1.15f));
            for (int side = -1; side <= 1; side += 2)
                RoundedBox("CourtyardBench", new Vector3(5.6f + side * 1.28f, .30f, -1.15f), new Vector3(.92f, .34f, .42f), oldWood, .05f, true);
            CreatePlaque("茶水自取", new Vector3(5.6f, 1.22f, -1.05f), Quaternion.identity,
                new Vector2(1.05f, .28f), .026f, new Color(.94f, .72f, .36f));
            CreateCourtyardLantern(new Vector3(-5.9f, .0f, -1.8f));
            CreateCourtyardLantern(new Vector3(5.9f, .0f, 2.0f));
            // Keep one entrance advertising board only. The former small
            // CourtyardNoticeBoard duplicated the covered newspaper board below.
        }

        private void CreateIndependentBathroom(float roomX, float baseY, int roomNumber)
        {
            // Rear-quarter wet room: 6.35 m wide by 3.85 m deep, with a
            // centered 1.42 m opening. It follows the hand-edited room's
            // horizontal partition without inheriting its oversized floor.
            const float bathroomFrontZ = 13.05f;
            const float bathroomBackZ = 16.55f;
            const float roomWidth = 5.75f;
            const float doorWidth = 1.42f;
            float roomLeft = roomX - roomWidth * .5f;
            float roomRight = roomX + roomWidth * .5f;
            // Put the wet-room opening on the bedroom's left side, leaving a
            // continuous partition and a clear route from the bed to the door.
            float doorCenterX = roomLeft + .86f;
            float leftWallWidth = Mathf.Max(.12f, doorCenterX - doorWidth * .5f - roomLeft);
            float rightWallWidth = Mathf.Max(.12f, roomRight - (doorCenterX + doorWidth * .5f));
            Box("BathroomPartitionFront", new Vector3(roomLeft + leftWallWidth * .5f, baseY + 1.82f, bathroomFrontZ),
                new Vector3(leftWallWidth, 3.46f, .16f), bathroomTile, true);
            Box("BathroomPartitionFrontRight", new Vector3(roomRight - rightWallWidth * .5f, baseY + 1.82f, bathroomFrontZ),
                new Vector3(rightWallWidth, 3.46f, .16f), bathroomTile, true);
            // Close the space above the sliding glass door. This segment starts
            // above the frame instead of overlapping it, so the doorway reads as
            // a real opening without creating another coplanar flashing surface.
            const float partitionTop = 3.55f;
            const float overheadBottom = 2.43f;
            Box("BathroomDoorOverheadWall", new Vector3(doorCenterX,
                    baseY + (partitionTop + overheadBottom) * .5f, bathroomFrontZ),
                new Vector3(doorWidth, partitionTop - overheadBottom, .16f), bathroomTile, true);
            Box("BathroomPartitionRear", new Vector3(roomX, baseY + 1.82f, bathroomBackZ), new Vector3(roomWidth, 3.46f, .16f), bathroomTile, true);
            Box("BathroomFloor", new Vector3(roomX, baseY + .11f, (bathroomFrontZ + bathroomBackZ) * .5f), new Vector3(roomWidth, .08f, bathroomBackZ - bathroomFrontZ), bathroomTile, false);
            for (int i = 0; i < 6; i++)
                Box("BathroomTileJoint", new Vector3(roomX - 2.34f + i * .94f, baseY + .158f, 14.80f), new Vector3(.018f, .012f, 3.20f), blackMetal, false);
            for (int i = 0; i < 8; i++)
                Box("BathroomTileJointCross", new Vector3(roomX, baseY + .159f, 13.42f + i * .40f), new Vector3(5.45f, .012f, .018f), blackMetal, false);

            CreateRearBathroomDoor(new Vector3(doorCenterX, baseY, bathroomFrontZ), doorWidth);
            CreateToilet(new Vector3(roomX - 2.10f, baseY, 15.55f));
            CreateBathroomVanity(new Vector3(roomX - .25f, baseY, 13.72f));
            CreateShower(new Vector3(roomX + 1.88f, baseY, 15.55f));
            CreateDecorPanel($"BathroomBotanical_{roomNumber}",
                new Vector3(roomX + .78f, baseY + 2.28f, bathroomFrontZ + .091f), Quaternion.Euler(0f, 180f, 0f),
                new Vector2(.62f, .82f), 1, 0, 3, 3);
            Box("TowelRail", new Vector3(roomX - 1.10f, baseY + 1.52f, 16.34f), new Vector3(.78f, .08f, .08f), brass, false);
            RoundedBox("FoldedTowel", new Vector3(roomX - 1.10f, baseY + 1.31f, 16.28f), new Vector3(.68f, .42f, .08f), softWhite, .08f, false);
            RoundedBox("BathroomAmenityTray", new Vector3(roomX - .25f, baseY + 1.02f, 13.72f), new Vector3(.62f, .045f, .22f), brass, .04f, false);
            for (int i = -1; i <= 1; i++)
                CylinderByDimensions("BathroomAmenityBottle", new Vector3(roomX - .25f + i * .18f, baseY + 1.16f, 13.72f), .10f, .24f, i == 0 ? softBlue : softWhite, false);
        }

        private void CreateRearBathroomDoor(Vector3 origin, float width)
        {
            float openingHalf = width * .5f;
            Box("BathroomDoorFrameLeft", origin + new Vector3(-openingHalf, 1.12f, -.08f), new Vector3(.12f, 2.24f, .16f), deepWood, false);
            Box("BathroomDoorFrameRight", origin + new Vector3(openingHalf, 1.12f, -.08f), new Vector3(.12f, 2.24f, .16f), deepWood, false);
            Box("BathroomDoorFrameTop", origin + new Vector3(0f, 2.33f, -.08f), new Vector3(width + .24f, .14f, .16f), deepWood, false);
            GameObject pivot = new("BathroomDoorPivot");
            pivot.transform.SetParent(root, false);
            pivot.transform.position = origin + new Vector3(-openingHalf, 0f, -.12f);
            // Move the entire framed leaf as one unit. Previously only the wood
            // slab moved, leaving its handle/insets and collision in the doorway.
            GameObject leaf = new("BathroomDoorLeaf");
            leaf.transform.SetParent(pivot.transform, false);
            leaf.transform.localPosition = new Vector3(openingHalf, 0f, 0f);
            ChildSingleSidedPane("BathroomDoorGlass", leaf.transform, new Vector3(0f, 1.14f, .012f),
                new Vector2(width - .16f, 2.08f), Quaternion.identity, frostedGlass ?? stableWindowGlass ?? glass);
            // Rails meet at clean seams instead of occupying the same corner
            // volumes. The middle bar also stops before both vertical rails.
            ChildBox("BathroomDoorRailLeft", leaf.transform, new Vector3(-width * .5f + .035f, 1.14f, 0f),
                new Vector3(.07f, 2.14f, .075f), blackMetal, false);
            ChildBox("BathroomDoorRailRight", leaf.transform, new Vector3(width * .5f - .035f, 1.14f, 0f),
                new Vector3(.07f, 2.14f, .075f), blackMetal, false);
            ChildBox("BathroomDoorRailTop", leaf.transform, new Vector3(0f, 2.245f, 0f),
                new Vector3(width - .14f, .07f, .075f), blackMetal, false);
            ChildBox("BathroomDoorRailBottom", leaf.transform, new Vector3(0f, .035f, 0f),
                new Vector3(width - .14f, .07f, .075f), blackMetal, false);
            ChildBox("BathroomDoorRailMiddle", leaf.transform, new Vector3(0f, 1.14f, 0f),
                new Vector3(width - .16f, .045f, .060f), blackMetal, false);
            ChildSphere("BathroomDoorHandle", leaf.transform, new Vector3(width * .34f, 1.10f, -.075f),
                new Vector3(.055f, .11f, .055f), brass);
            GameObject collisionObject = new("BathroomDoorCollision");
            collisionObject.transform.SetParent(leaf.transform, false);
            BoxCollider leafCollision = collisionObject.AddComponent<BoxCollider>();
            leafCollision.center = new Vector3(0f, 1.14f, 0f);
            leafCollision.size = new Vector3(width, 2.28f, .10f);
            // A non-blocking proxy stays at the opening after the leaf slides into
            // its wall pocket, so the player can always close the door again.
            BoxCollider interactionProxy = pivot.AddComponent<BoxCollider>();
            interactionProxy.center = new Vector3(openingHalf, 1.14f, 0f);
            interactionProxy.size = new Vector3(width + .42f, 2.46f, .52f);
            interactionProxy.isTrigger = true;
            ChapterThreeInteractable door = pivot.AddComponent<ChapterThreeInteractable>();
            door.ConfigureSlidingDoor(width * 1.05f);
        }

        private void CreateToilet(Vector3 origin)
        {
            RoundedBox("ToiletCistern", origin + new Vector3(0f, .79f, .42f), new Vector3(.62f, .78f, .27f), porcelain, .16f, true);
            RoundedBox("ToiletBowl", origin + new Vector3(0f, .43f, 0f), new Vector3(.62f, .34f, .78f), porcelain, .20f, true);
            RoundedBox("ToiletSeat", origin + new Vector3(0f, .62f, -.04f), new Vector3(.58f, .07f, .72f), softWhite, .22f, false);
            CylinderByDimensions("FlushButton", origin + new Vector3(0f, 1.20f, .42f), .08f, .025f, chromeMaterial(), false);
        }

        private void CreateBathroomVanity(Vector3 origin)
        {
            RoundedBox("VanityCabinet", origin + new Vector3(0f, .47f, 0f), new Vector3(1.16f, .82f, .58f), deepWood, .08f, true);
            RoundedBox("VanityTop", origin + new Vector3(0f, .91f, -.01f), new Vector3(1.28f, .10f, .66f), porcelain, .09f, false);
            // Real cabinet fronts, shadow gaps and hardware replace the previous
            // single featureless block.
            for (int side = -1; side <= 1; side += 2)
            {
                RoundedBox("VanityDoor", origin + new Vector3(side * .285f, .48f, .305f),
                    new Vector3(.50f, .64f, .035f), oldWood, .05f, false);
                Box("VanityDoorInset", origin + new Vector3(side * .285f, .48f, .334f),
                    new Vector3(.34f, .47f, .012f), deepWood, false);
                Sphere("VanityDoorKnob", origin + new Vector3(side * .09f, .48f, .375f),
                    Vector3.one * .038f, brass);
            }
            Box("VanityToeKick", origin + new Vector3(0f, .13f, .25f), new Vector3(.96f, .13f, .08f), blackMetal, false);
            RoundedBox("WashBasin", origin + new Vector3(0f, .98f, .04f), new Vector3(.66f, .15f, .46f), porcelain, .18f, false);
            RoundedBox("WashBasinInner", origin + new Vector3(0f, 1.055f, .05f), new Vector3(.48f, .026f, .30f), mirror, .16f, false);
            CylinderByDimensions("FaucetStem", origin + new Vector3(0f, 1.13f, -.19f), .055f, .30f, brass, false);
            BeamBetween("FaucetSpout", origin + new Vector3(0f, 1.27f, -.19f), origin + new Vector3(0f, 1.27f, .07f), .035f, brass, false);
            // A layered arched mirror reads as a real framed object and keeps every
            // surface at a distinct depth to prevent shimmer.
            RoundedBox("BathroomMirrorBacking", origin + new Vector3(0f, 1.94f, -.285f),
                new Vector3(1.10f, 1.50f, .055f), deepWood, .20f, false);
            RoundedBox("BathroomMirror", origin + new Vector3(0f, 1.94f, -.225f),
                new Vector3(.93f, 1.32f, .018f), mirror, .22f, false);
            Box("MirrorFrameTop", origin + new Vector3(0f, 2.67f, -.19f), new Vector3(.90f, .06f, .07f), brass, false);
            Box("MirrorFrameBottom", origin + new Vector3(0f, 1.21f, -.19f), new Vector3(.90f, .06f, .07f), brass, false);
            Box("MirrorFrameLeft", origin + new Vector3(-.49f, 1.94f, -.19f), new Vector3(.06f, 1.34f, .07f), brass, false);
            Box("MirrorFrameRight", origin + new Vector3(.49f, 1.94f, -.19f), new Vector3(.06f, 1.34f, .07f), brass, false);
            for (int side = -1; side <= 1; side += 2)
            {
                CylinderByDimensions("MirrorWallSconce", origin + new Vector3(side * .76f, 2.03f, -.17f),
                    .11f, .24f, brass, false);
                Sphere("MirrorSconceGlow", origin + new Vector3(side * .76f, 2.20f, -.13f),
                    new Vector3(.16f, .22f, .12f), warmGlow);
            }
            Box("VanityBacksplash", origin + new Vector3(0f, 1.12f, -.315f), new Vector3(1.24f, .34f, .028f), bathroomTile, false);
        }

        private void CreateShower(Vector3 origin)
        {
            Box("ShowerGlassA", origin + new Vector3(-.46f, 1.12f, .10f), new Vector3(.035f, 2.12f, .86f), frostedGlass ?? glass, true);
            Box("ShowerGlassB", origin + new Vector3(.17f, 1.12f, -.51f), new Vector3(.62f, 2.12f, .035f), frostedGlass ?? glass, true);
            CylinderByDimensions("ShowerPipe", origin + new Vector3(.38f, 1.45f, .42f), .045f, 1.45f, brass, false);
            Sphere("ShowerHead", origin + new Vector3(.38f, 2.13f, .25f), new Vector3(.18f, .06f, .18f), brass);
            RoundedBox("ShowerTray", origin + new Vector3(0f, .16f, 0f), new Vector3(1.02f, .10f, 1.02f), porcelain, .10f, true);
            CylinderByDimensions("ShowerDrain", origin + new Vector3(-.22f, .225f, .22f), .14f, .018f, blackMetal, false);
        }

        private void CreateRoomLighting(float roomX, float baseY, int roomNumber)
        {
            CylinderByDimensions("GuestRoomPendant", new Vector3(roomX - .65f, baseY + 3.37f, 9.55f), .055f, .58f, blackMetal, false);
            Sphere("GuestRoomLampShade", new Vector3(roomX - .65f, baseY + 3.04f, 9.55f), new Vector3(.38f, .18f, .38f), warmGlow);
            CreatePointLight(new Vector3(roomX - .65f, baseY + 2.86f, 9.55f), new Color(1f, .84f, .72f), roomNumber == 203 ? 3.05f : 3.35f, 10.0f);
            RoundedBox("GuestRoomCeilingPanel", new Vector3(roomX + 1.34f, baseY + 3.52f, 7.05f), new Vector3(1.18f, .045f, .54f), warmGlow, .08f, false);
            CreatePointLight(new Vector3(roomX + 1.34f, baseY + 2.84f, 7.05f), new Color(1f, .88f, .77f), 2.15f, 7.25f);
            RoundedBox("GuestRoomCeilingPanel", new Vector3(roomX - 1.42f, baseY + 3.52f, 12.72f), new Vector3(1.34f, .045f, .54f), warmGlow, .08f, false);
            CreatePointLight(new Vector3(roomX - 1.42f, baseY + 2.84f, 12.72f), new Color(1f, .84f, .72f), 2.10f, 7.35f);
            Box("BathroomCeilingPanel", new Vector3(roomX, baseY + 3.52f, 15.05f), new Vector3(1.25f, .035f, .40f), coolGlow, false);
            CreatePointLight(new Vector3(roomX, baseY + 2.72f, 15.05f), new Color(.82f, .92f, .96f), 1.62f, 6.2f);
            CreatePointLight(new Vector3(roomX - 2.15f, baseY + 2.08f, 13.72f), new Color(1f, .86f, .74f), .36f, 2.15f);
        }

        private void CreateGuestAmenities(float roomX, float baseY, int variant, bool singleRoom, int roomNumber)
        {
            // Two compact nightstands flank the front-right bed ends, outside
            // the mattress footprint and clear of the bathroom partition.
            float[] tableZ = { 7.02f, 9.78f };
            for (int i = 0; i < tableZ.Length; i++)
            {
                float x = roomX + .48f;
                float z = tableZ[i];
                RoundedBox("BedsideTable", new Vector3(x, baseY + .48f, z), new Vector3(.46f, .74f, .50f), deepWood, .06f, true);
                Box("BedsideDrawer", new Vector3(x, baseY + .58f, z - .275f), new Vector3(.34f, .22f, .045f), oldWood, false);
                Sphere("BedsideKnob", new Vector3(x, baseY + .58f, z - .33f), Vector3.one * .038f, brass);
                if (i == 1) CylinderByDimensions("WaterGlass", new Vector3(x + .08f, baseY + .93f, z), .12f, .20f, glass, false);
                CreateInteractiveBedsideLamp(new Vector3(x, baseY + .91f, z + .17f), roomNumber, i);
                if (i == 0) CreateBedsideRoomInformation(new Vector3(x, baseY + .88f, z - .16f), roomNumber);
            }
            // A paired 3D slipper set beside the bed aisle. Soles and uppers use
            // actual geometry; no flat sticker is projected onto the floor.
            CreateGuestSlipper("GuestSlipperLeft", new Vector3(roomX + .18f, baseY + .18f, 8.18f), -5f - variant);
            CreateGuestSlipper("GuestSlipperRight", new Vector3(roomX + .49f, baseY + .18f, 8.20f), 5f + variant);
            Box("PowerOutlet", new Vector3(roomX + 2.84f, baseY + .48f, 10.95f), new Vector3(.24f, .18f, .025f), softWhite, false);
            RoundedBox("UpholsteredFootBench", new Vector3(roomX + .50f, baseY + .38f, 11.05f),
                new Vector3(singleRoom ? 1.50f : 2.00f, .46f, .48f), softBlue, .10f, true);
        }

        private void CreateBedsideRoomInformation(Vector3 position, int roomNumber)
        {
            GameObject stand = RoundedBox($"RoomInformationStand_{roomNumber}", position + Vector3.up * .17f,
                new Vector3(.34f, .32f, .035f), deepWood, .035f, false);
            stand.transform.rotation = Quaternion.Euler(-8f, 180f, 0f);
            Texture2D informationTexture = Resources.Load<Texture2D>("ChapterThree/RoomInformationCard");
            if (informationTexture == null) return;
            GameObject sticker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sticker.name = $"RoomInformationSticker_{roomNumber}";
            sticker.transform.SetParent(stand.transform, false);
            sticker.transform.localPosition = new Vector3(0f, 0f, -.56f);
            sticker.transform.localRotation = Quaternion.identity;
            sticker.transform.localScale = new Vector3(.44f, .88f, 1f);
            Renderer renderer = sticker.GetComponent<Renderer>();
            renderer.sharedMaterial = UnlitTextureMaterial($"RoomInformationCard_{roomNumber}", informationTexture, Color.white);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 9;
            RemoveCollider(sticker.GetComponent<Collider>());
        }

        private void NormalizeRoomLayout()
        {
            if (root == null) return;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform item = all[i];
                if (item == null) continue;
                if (item.name == "WritingDeskTop" || item.name == "WritingDeskLeg" ||
                    item.name == "DeskChairSeat" || item.name == "DeskChairBack" ||
                    item.name == "DeskChairLeg" || item.name == "ArchiveTable" ||
                    item.name == "ArchiveTableLeg" || item.name == "SingleGuestBed" ||
                    item.name == "TwinBedLeft" || item.name == "TwinBedRight" ||
                    item.name == "ArchiveGuestBed" || item.name == "TvCabinet" ||
                    item.name == "TvScreen" || item.name == "TvStand" || item.name == "ScreenScanLine")
                {
                    if (Application.isPlaying) Destroy(item.gameObject);
                    else DestroyImmediate(item.gameObject);
                }
                else if (item.name == "InteractiveWardrobe")
                    item.localRotation = Quaternion.Euler(0f, -90f, 0f);
                else if (item.name == "LuggageBench" && item.localPosition.z > 8.00f)
                    item.localPosition += Vector3.forward * -.10f;
                else if (item.name == "LuggageBenchSlat" && item.localPosition.z > 8.00f)
                    item.localPosition += Vector3.forward * -.10f;
                else if (item.name == "UpholsteredFootBench" && item.localPosition.z < 8.50f)
                    item.localPosition += Vector3.forward * .30f;
            }
            EnsureWindowTransparency();
        }

        private void EnsureWindowTransparency()
        {
            if (root == null) return;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null ||
                    (!renderer.name.Contains("WindowGlass") && !renderer.name.Contains("WindowDaylight"))) continue;
                Material material = renderer.sharedMaterial;
                if (material == null) continue;
                Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
                color.a = renderer.name.Contains("WindowDaylight") ? .025f : .10f;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                material.color = color;
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", renderer.name.Contains("WindowGlass") ? 1 : 0);
                if (material.HasProperty("_Cull")) material.SetInt("_Cull", (int)CullMode.Off);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void BuildRoomDetailPass()
        {
            // Baked scenes predate the suitcase pass. Add the same small luggage
            // vignette at runtime so the authored layout and the shipped scene agree.
            for (int floor = 0; floor < 2; floor++)
            for (int room = 0; room < RoomCenters.Length; room++)
            {
                float baseY = floor * StoreyHeight;
                float x = RoomCenters[room] - 2.70f;
                const float z = 6.10f;
                RoundedBox("LuggageCase", new Vector3(x, baseY + .78f, z), new Vector3(.78f, .32f, .34f), softBlue, .07f, false);
                Box("LuggageCaseBand", new Vector3(x, baseY + .80f, z), new Vector3(.10f, .36f, .36f), brass, false);
                Box("LuggageCaseHandle", new Vector3(x, baseY + 1.00f, z), new Vector3(.32f, .055f, .055f), blackMetal, false);
                for (int side = -1; side <= 1; side += 2)
                    CylinderByDimensions("LuggageCaseWheel", new Vector3(x + side * .26f, baseY + .60f, z - .12f), .055f, .035f, blackMetal, false,
                        Quaternion.Euler(90f, 0f, 0f));
            }
        }

        private void CreateInteractiveBedsideLamp(Vector3 position, int roomNumber, int fixtureIndex)
        {
            GameObject fixture = new("InteractiveBedsideLamp");
            fixture.transform.SetParent(root, false);
            fixture.transform.position = position;
            BoxCollider proxy = fixture.AddComponent<BoxCollider>();
            proxy.center = new Vector3(0f, .31f, 0f);
            proxy.size = new Vector3(.42f, .68f, .42f);
            ChapterThreeInteractable interactable = fixture.AddComponent<ChapterThreeInteractable>();
            interactable.Configure(ChapterThreeInteractionKind.BedsideLamp, roomNumber * 10 + fixtureIndex,
                "切换床头灯", "独立控制床边阅读灯");
            interactable.SetToggleState(true);
            ChildRoundedBox("BedsideLampBase", fixture.transform, new Vector3(0f, .045f, 0f), new Vector3(.28f, .09f, .28f), brass, .06f, false);
            ChildBox("BedsideLampStem", fixture.transform, new Vector3(0f, .26f, 0f), new Vector3(.045f, .43f, .045f), blackMetal, false);
            ChildRoundedBox("BedsideLampGlow", fixture.transform, new Vector3(0f, .50f, 0f), new Vector3(.36f, .27f, .36f), warmGlow, .10f, false);
            GameObject lightObject = new("Chapter3_InteriorFixtureLight");
            lightObject.transform.SetParent(fixture.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, .48f, -.08f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, .72f, .50f);
            light.intensity = .52f;
            light.range = 2.65f;
            light.shadows = LightShadows.None;
        }

        private void CreateArchiveCorner(float roomX, float baseY)
        {
            RoundedBox("ArchiveTable", new Vector3(roomX + 1.52f, baseY + .80f, 8.12f), new Vector3(1.90f, .12f, .82f), oldWood, .06f, true);
            for (int side = -1; side <= 1; side += 2)
                Box("ArchiveTableLeg", new Vector3(roomX + 1.52f + side * .75f, baseY + .39f, 8.12f), new Vector3(.10f, .78f, .62f), deepWood, true);
            for (int i = 0; i < 5; i++)
                RoundedBox("ArchiveFolder", new Vector3(roomX + 1.05f + i * .21f, baseY + .91f + i * .025f, 8.12f),
                    new Vector3(.64f, .032f, .45f), i % 2 == 0 ? linen : softBlue, .04f, false)
                    .transform.rotation = Quaternion.Euler(0f, -11f + i * 5f, 0f);
            RoundedBox("EmptyNameCardTray", new Vector3(roomX + 1.52f, baseY + .98f, 8.54f), new Vector3(.72f, .10f, .42f), brass, .05f, false);
        }

        private void BuildRefinedCourtyardDetails()
        {
            CreateStoneTeaTable(new Vector3(5.45f, .02f, -4.85f));
            CreateReceptionCounter(new Vector3(-5.3f, .05f, -3.8f));
            CreateCoveredNoticeBoard(new Vector3(-7.7f, .08f, 2.25f));
            CreateBambooScreen(new Vector3(13.30f, .05f, -1.7f));
            CreateLaundryLine(new Vector3(7.8f, .05f, 1.15f));
            CreateDrainageChannel();
            CreateCourtyardLantern(new Vector3(-3.15f, .02f, .6f));
            CreateCourtyardLantern(new Vector3(3.15f, .02f, .6f));
            CreateCourtyardLantern(new Vector3(-7.9f, .02f, -9.1f));
            BuildCourtyardDecorationPass();
        }

        private void BuildCourtyardDecorationPass()
        {
            // Dense but route-safe planting: all clusters stay against the wall or
            // inside existing furniture islands, leaving the gate-to-gallery axis
            // and stair approach unobstructed.
            Vector3[] planters =
            {
                new(-13.65f, .05f, -11.8f), new(-13.65f, .05f, -6.2f), new(-13.65f, .05f, .25f),
                new(13.65f, .05f, -10.1f), new(13.65f, .05f, -5.1f), new(13.65f, .05f, 1.2f)
            };
            for (int i = 0; i < planters.Length; i++)
            {
                Vector3 p = planters[i];
                CylinderByDimensions("CourtyardCeramicPlanter", p + Vector3.up * .29f, .54f, .56f,
                    i % 2 == 0 ? porcelain : bathroomTile, true);
                CylinderByDimensions("PlanterSoilInset", p + Vector3.up * .58f, .43f, .035f, dampGround, false);
                CreateHomestayFoliageCross("CourtyardPlantCluster", p + Vector3.up * 1.02f,
                    new Vector2(.92f, 1.12f), i % 4, i % 2, Quaternion.Euler(0f, i * 17f, 0f));
            }

            // Rain-water and garden-service details enrich the less-used rear
            // corners without placing colliders in the player's interaction lanes.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 barrel = new(side * 11.55f, .48f, 16.85f);
                CylinderByDimensions("CourtyardRainBarrel", barrel, .72f, .92f, deepWood, true);
                for (int ring = 0; ring < 3; ring++)
                    CylinderByDimensions("RainBarrelIronBand", barrel + Vector3.up * (-.30f + ring * .30f), .75f, .045f,
                        blackMetal, false);
                RoundedBox("GardenToolCrate", new Vector3(side * 10.65f, .28f, 16.70f),
                    new Vector3(.78f, .45f, .54f), oldWood, .045f, true);
            }

            // A suspended lantern string adds depth above the courtyard. The cable
            // and lanterns are visual-only and stay well above player height.
            BeamBetween("CourtyardLanternCable", new Vector3(-12.8f, 3.18f, -8.6f),
                new Vector3(12.8f, 3.18f, -8.6f), .018f, blackMetal, false);
            for (int i = 0; i < 7; i++)
            {
                float x = -10.5f + i * 3.5f;
                BeamBetween("HangingLanternCord", new Vector3(x, 3.18f, -8.6f),
                    new Vector3(x, 2.83f, -8.6f), .014f, blackMetal, false);
                RoundedBox("HangingCourtyardLantern", new Vector3(x, 2.72f, -8.6f),
                    new Vector3(.24f, .30f, .24f), warmGlow, .06f, false);
            }

            CreateDecorPanel("CourtyardInkWashPanel", new Vector3(-14.742f, 1.48f, -1.2f),
                Quaternion.Euler(0f, 90f, 0f), new Vector2(1.65f, 1.10f), 0, 0, 3, 3);
            CreateDecorPanel("CourtyardRattanPanel", new Vector3(14.742f, 1.42f, -6.8f),
                Quaternion.Euler(0f, -90f, 0f), new Vector2(1.45f, 1.05f), 2, 0, 3, 3);

            // Transparent aging decals use a 24 mm stand-off from the plaster.
            CreateArchitectureDecal("CourtyardSubtleDampA", new Vector3(-14.744f, .90f, -7.4f),
                Quaternion.Euler(0f, 90f, 0f), new Vector2(1.15f, .82f), 1, 0);
            CreateArchitectureDecal("CourtyardSubtleDampB", new Vector3(14.744f, .78f, -2.5f),
                Quaternion.Euler(0f, -90f, 0f), new Vector2(1.05f, .72f), 2, 0);
        }

        private void CreateStoneTeaTable(Vector3 position)
        {
            CylinderByDimensions("StoneTeaTablePedestal", position + Vector3.up * .40f, .62f, .78f, wetStone, true);
            CylinderByDimensions("StoneTeaTableTop", position + Vector3.up * .82f, 1.65f, .16f, wetStone, true);
            CreateCompactKettleSet(position + new Vector3(0f, .99f, 0f));
            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2f / 3f;
                Vector3 stool = position + new Vector3(Mathf.Sin(angle) * 1.35f, .29f, Mathf.Cos(angle) * 1.35f);
                CylinderByDimensions("StoneTeaStool", stool, .58f, .55f, wetStone, true);
            }
        }

        private void CreateReceptionCounter(Vector3 position)
        {
            RoundedBox("OutdoorReceptionCounter", position + new Vector3(0f, .68f, 0f), new Vector3(2.75f, 1.22f, .82f), deepWood, .045f, true);
            RoundedBox("ReceptionCounterTop", position + new Vector3(0f, 1.34f, 0f), new Vector3(2.95f, .14f, .96f), oldWood, .06f, false);
            for (int i = -1; i <= 1; i++)
                Box("ReceptionPanel", position + new Vector3(i * .82f, .71f, -.43f), new Vector3(.64f, .84f, .045f), oldWood, false);
            RoundedBox("GuestRegister", position + new Vector3(.55f, 1.46f, -.05f), new Vector3(.72f, .045f, .48f), linen, .06f, false)
                .transform.rotation = Quaternion.Euler(0f, 12f, 0f);
            CreateDeskLamp(position + new Vector3(-.70f, 1.46f, .08f));
        }

        private void CreateCoveredNoticeBoard(Vector3 position)
        {
            Box("NoticeBoardPostLeft", position + new Vector3(-1.25f, 1.15f, 0f), new Vector3(.14f, 2.3f, .14f), deepWood, true);
            Box("NoticeBoardPostRight", position + new Vector3(1.25f, 1.15f, 0f), new Vector3(.14f, 2.3f, .14f), deepWood, true);
            RoundedBox("NoticeBoardBacking", position + new Vector3(0f, 1.52f, -.03f), new Vector3(2.58f, 1.36f, .12f), oldWood, .04f, false);
            Box("NoticeBoardPaper", position + new Vector3(0f, 1.52f, -.11f), new Vector3(2.18f, 1.05f, .025f), linen, false);
            CreatePlaque("广告栏 · 报纸 / 明信片", position + new Vector3(0f, 2.13f, -.23f), Quaternion.identity,
                new Vector2(2.12f, .24f), .020f, new Color(.26f, .16f, .08f), false);
            CreateMediaThumbnail("广告栏报纸缩略图", position + new Vector3(-.58f, 1.54f, -.18f),
                new Vector3(1.00f, .76f, .026f), "ChapterThree/NoticeNewspaper", 0,
                "报纸 · 2026年8月20日", "门口广告栏中的报纸，按 E 查看完整页面");
            CreateMediaThumbnail("广告栏明信片缩略图", position + new Vector3(.58f, 1.54f, -.18f),
                new Vector3(1.00f, .76f, .026f), "ChapterThree/NoticePostcard", 1,
                "明信片 · 闽清县樟洋村", "门口广告栏中的明信片，按 E 查看完整图片");
            GameObject hood = Box("NoticeBoardRainHood", position + new Vector3(0f, 2.42f, 0f), new Vector3(3.0f, .16f, .92f), roofTiles, false);
            hood.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
        }

        private void CreateMediaThumbnail(string name, Vector3 position, Vector3 size, string resourcePath, int id,
            string title, string story)
        {
            // Use a front-facing quad instead of a cube. The old cube mapped the
            // portrait newspaper across a landscape face and made the image look
            // mirrored/upside-down from the approach side.
            GameObject thumbnail = GameObject.CreatePrimitive(PrimitiveType.Quad);
            thumbnail.name = name;
            thumbnail.transform.SetParent(root, false);
            thumbnail.transform.position = position;
            // Unity's Quad front face points toward local -Z. The entrance path
            // is on the board's -Z side, so identity rotation presents the image
            // to the player without back-face culling or mirroring.
            thumbnail.transform.rotation = Quaternion.identity;
            float aspect = id == 0 ? (2160f / 3850f) : (1200f / 750f);
            float height = size.y;
            thumbnail.transform.localScale = new Vector3(height * aspect, height, 1f);
            Renderer renderer = thumbnail.GetComponent<Renderer>();
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                // Unlit media remains readable at night and in the shadow of the
                // rain hood. Derive the geometry from the imported texture itself.
                float actualAspect = texture.width / (float)Mathf.Max(1, texture.height);
                thumbnail.transform.localScale = new Vector3(height * actualAspect, height, 1f);
                Material mediaMaterial = UnlitTextureMaterial(name + "材质", texture, Color.white);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                renderer.sharedMaterial = mediaMaterial;
            }
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 12;
            RemoveCollider(thumbnail.GetComponent<Collider>());
            BoxCollider collider = thumbnail.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            // Keep a generous, invisible aim area around portrait media.
            collider.size = new Vector3(Mathf.Max(1.35f, 1.18f / Mathf.Max(.12f, thumbnail.transform.localScale.x)), 1.50f, .40f);
            ChapterThreeInteractable interactable = thumbnail.AddComponent<ChapterThreeInteractable>();
            interactable.Configure(ChapterThreeInteractionKind.NoticeMedia, id, title, story);
        }

        private void CreateBambooScreen(Vector3 position)
        {
            for (int i = 0; i < 12; i++)
            {
                float z = position.z - 2.2f + i * .40f;
                CylinderByDimensions("BambooScreenStem", new Vector3(position.x, 1.28f + (i % 3) * .08f, z), .075f, 2.5f + (i % 3) * .16f, oldWood, true);
                if (i % 2 == 0) CreateAtlasCross("BambooLeafCluster", new Vector3(position.x - .12f, 2.15f, z), new Vector2(.85f, .72f), 1 + (i % 3), i % 2, 4, 2, Quaternion.Euler(0f, 90f, 0f));
            }
            Box("BambooScreenTopRail", position + new Vector3(0f, 2.34f, 0f), new Vector3(.11f, .10f, 4.85f), deepWood, false);
        }

        private void CreateLaundryLine(Vector3 position)
        {
            CylinderByDimensions("LaundryPostA", position + new Vector3(-1.8f, 1.28f, 0f), .09f, 2.55f, deepWood, true);
            CylinderByDimensions("LaundryPostB", position + new Vector3(1.8f, 1.28f, 0f), .09f, 2.55f, deepWood, true);
            BeamBetween("LaundryCord", position + new Vector3(-1.8f, 2.24f, 0f), position + new Vector3(1.8f, 2.24f, 0f), .018f, blackMetal, false);
            for (int i = 0; i < 4; i++)
            {
                Color clothColor = i % 2 == 0 ? new Color(.50f, .32f, .23f) : new Color(.16f, .28f, .31f);
                Material cloth = Material("Chapter3_LaundryCloth_" + i, clothColor, .04f);
                RoundedBox("HangingLaundry", position + new Vector3(-1.2f + i * .78f, 1.58f - (i % 2) * .08f, .04f),
                    new Vector3(.55f, 1.12f, .035f), cloth, .06f, false);
            }
        }

        private void CreateDrainageChannel()
        {
            // Read as an inset metal channel, but keep its visible top clearly
            // above the bluestone rather than 5 mm below the same plane.
            Box("CourtyardDrain", new Vector3(13.18f, .035f, -3.2f), new Vector3(.52f, .06f, 22.2f), blackMetal, true);
            for (int i = 0; i < 26; i++)
                Box("DrainGrate", new Vector3(13.18f, .078f, -13.6f + i * .82f), new Vector3(.48f, .018f, .055f), brass, false);
        }

        private void CreateCourtyardLantern(Vector3 position)
        {
            CylinderByDimensions("CourtyardLanternPost", position + Vector3.up * 1.28f, .10f, 2.55f, deepWood, true);
            RoundedBox("CourtyardLanternHousing", position + Vector3.up * 2.55f, new Vector3(.48f, .62f, .48f), blackMetal, .08f, false);
            RoundedBox("CourtyardLanternGlow", position + Vector3.up * 2.55f, new Vector3(.34f, .45f, .34f), warmGlow, .12f, false);
            Box("LanternCap", position + Vector3.up * 2.92f, new Vector3(.62f, .08f, .62f), roofTiles, false);
            CreatePointLight(position + Vector3.up * 2.52f, new Color(1f, .38f, .14f), 1.18f, 4.6f);
        }

        private void CreateCompactKettleSet(Vector3 position)
        {
            Sphere("KettleBody", position, new Vector3(.28f, .23f, .28f), porcelain);
            CylinderByDimensions("KettleLid", position + Vector3.up * .22f, .18f, .055f, brass, false);
            BeamBetween("KettleSpout", position + new Vector3(.18f, .05f, 0f), position + new Vector3(.42f, .12f, 0f), .045f, porcelain, false);
            for (int i = 0; i < 2; i++)
                CylinderByDimensions("TeaCup", position + new Vector3(-.38f + i * .22f, -.08f, .18f), .12f, .16f, porcelain, false);
        }

        private void CreateDoorFrame(Vector3 origin, float width, float height)
        {
            float jambHeight = height - .09f;
            float jambCenter = .09f + jambHeight * .5f;
            Box("ExteriorDoorJambLeft", origin + new Vector3(-width * .5f - .07f, jambCenter, 0f), new Vector3(.14f, jambHeight, .22f), deepWood, true);
            Box("ExteriorDoorJambRight", origin + new Vector3(width * .5f + .07f, jambCenter, 0f), new Vector3(.14f, jambHeight, .22f), deepWood, true);
            Box("ExteriorDoorLintel", origin + new Vector3(0f, height + .10f, 0f), new Vector3(width, .16f, .22f), deepWood, true);
            // A readable 7 cm sill keeps its top away from the gallery plane.
            Box("ExteriorDoorThreshold", origin + new Vector3(0f, .02f, -.02f), new Vector3(width, .10f, .30f), wetStone, true);
        }

        private void CreateOutwardOpeningDoor(Vector3 origin, float width, float height, float yaw, bool hingeRight)
        {
            GameObject pivot = new("OutwardOpeningGuestDoor");
            pivot.transform.SetParent(root, false);
            float hingeSign = hingeRight ? 1f : -1f;
            float leafSign = hingeRight ? -1f : 1f;
            pivot.transform.position = origin + new Vector3(hingeSign * width * .5f, 0f, 0f);
            // The leaf is aligned with the frame while closed. The interactable
            // owns the separate swing angle so baking cannot collapse open/closed
            // into the same serialized rotation.
            pivot.transform.localRotation = Quaternion.identity;
            ChildBox("SolidWoodDoorLeaf", pivot.transform, new Vector3(leafSign * width * .5f, height * .5f, 0f),
                new Vector3(width, height, .09f), oldWood, true);
            for (int row = 0; row < 3; row++)
                ChildBox("DoorRaisedPanel", pivot.transform, new Vector3(leafSign * width * .5f, .52f + row * .63f, -.078f),
                    new Vector3(width * .68f, .39f, .025f), deepWood, false);
            ChildSphere("DoorLever", pivot.transform, new Vector3(leafSign * width * .84f, height * .52f, -.09f), Vector3.one * .055f, brass);
            ChapterThreeInteractable door = pivot.AddComponent<ChapterThreeInteractable>();
            door.ConfigureGuestDoor(hingeRight ? -Mathf.Abs(yaw) : Mathf.Abs(yaw));
        }

        private void CreateDeepWindow(Vector3 center, Vector2 size)
        {
            // A solid "recess" cube used to sit directly behind the glass, making
            // every window look black and opaque. Model the reveal as four pieces
            // around a genuinely empty opening instead.
            const float revealDepth = .18f;
            const float revealThickness = .12f;
            Box("WindowRevealTop", center + new Vector3(0f, size.y * .5f + revealThickness * .5f, .12f),
                new Vector3(size.x + .24f, revealThickness, revealDepth), deepWood, false);
            Box("WindowRevealBottom", center + new Vector3(0f, -size.y * .5f - revealThickness * .5f, .12f),
                new Vector3(size.x + .24f, revealThickness, revealDepth), deepWood, false);
            Box("WindowRevealLeft", center + new Vector3(-size.x * .5f - revealThickness * .5f, 0f, .12f),
                new Vector3(revealThickness, size.y, revealDepth), deepWood, false);
            Box("WindowRevealRight", center + new Vector3(size.x * .5f + revealThickness * .5f, 0f, .12f),
                new Vector3(revealThickness, size.y, revealDepth), deepWood, false);
            // One transparent pane is enough. The old daylight card and glass
            // were two sorted transparent planes in the same opening and could
            // swap draw order while the camera moved.
            CreateSingleSidedWindowGlass(center + Vector3.forward * .035f, size);
            Box("WindowTopFrame", center + new Vector3(0f, size.y * .5f + .08f, -.06f), new Vector3(size.x, .12f, .15f), oldWood, false);
            Box("WindowBottomFrame", center + new Vector3(0f, -size.y * .5f - .08f, -.06f), new Vector3(size.x, .12f, .15f), oldWood, false);
            // Side rails stop at the horizontal rails; the two mullions use
            // different depths so their central crossing has no coplanar skin.
            Box("WindowLeftFrame", center + new Vector3(-size.x * .5f - .08f, 0f, -.06f), new Vector3(.12f, size.y, .15f), oldWood, false);
            Box("WindowRightFrame", center + new Vector3(size.x * .5f + .08f, 0f, -.06f), new Vector3(.12f, size.y, .15f), oldWood, false);
            // Split the vertical mullion around the horizontal one. No two frame
            // members now occupy the central crossing volume or share a skin.
            Box("WindowVerticalMullionLower", center + new Vector3(0f, -size.y * .25f, -.09f),
                new Vector3(.075f, size.y * .50f, .11f), oldWood, false);
            Box("WindowVerticalMullionUpper", center + new Vector3(0f, size.y * .25f + .05f, -.09f),
                new Vector3(.075f, size.y * .50f - .10f, .11f), oldWood, false);
            Box("WindowHorizontalMullion", center + new Vector3(0f, .05f, -.09f),
                new Vector3(size.x, .075f, .11f), oldWood, false);
            RoundedBox("WindowCurtainLeft", center + new Vector3(-size.x * .60f, -.02f, .24f), new Vector3(.25f, size.y + .38f, .10f), softBlue, .12f, false);
            RoundedBox("WindowCurtainRight", center + new Vector3(size.x * .60f, -.02f, .24f), new Vector3(.25f, size.y + .38f, .10f), softBlue, .12f, false);
            CreatePointLight(center + Vector3.forward * .22f, new Color(.52f, .78f, .88f), .22f, 2.8f);
        }

        private void CreateSingleSidedWindowGlass(Vector3 position, Vector2 size)
        {
            EnsureSingleSidedPaneMesh();
            GameObject pane = new("WindowGlass");
            pane.transform.SetParent(root, false);
            pane.transform.position = position;
            pane.transform.localScale = new Vector3(size.x, size.y, 1f);
            MeshFilter filter = pane.AddComponent<MeshFilter>();
            filter.sharedMesh = windowPaneMesh;
            MeshRenderer renderer = pane.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = stableWindowGlass ?? glass;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = -2;
        }

        private void EnsureSingleSidedPaneMesh()
        {
            if (windowPaneMesh == null)
            {
                windowPaneMesh = new Mesh { name = "Chapter3_SingleSidedWindowPane" };
                windowPaneMesh.vertices = new[]
                {
                    new Vector3(-.5f, -.5f, 0f), new Vector3(.5f, -.5f, 0f),
                    new Vector3(.5f, .5f, 0f), new Vector3(-.5f, .5f, 0f)
                };
                windowPaneMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                windowPaneMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                windowPaneMesh.RecalculateNormals();
                windowPaneMesh.RecalculateBounds();
            }
        }

        private void ChildSingleSidedPane(string name, Transform parent, Vector3 localPosition,
            Vector2 size, Quaternion localRotation, Material material)
        {
            EnsureSingleSidedPaneMesh();
            GameObject pane = new(name);
            pane.transform.SetParent(parent, false);
            pane.transform.localPosition = localPosition;
            pane.transform.localRotation = localRotation;
            pane.transform.localScale = new Vector3(size.x, size.y, 1f);
            MeshFilter filter = pane.AddComponent<MeshFilter>();
            filter.sharedMesh = windowPaneMesh;
            MeshRenderer renderer = pane.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void CreateGuestSlipper(string name, Vector3 position, float yaw)
        {
            GameObject rootObject = new(name);
            rootObject.transform.SetParent(root, false);
            rootObject.transform.position = position;
            rootObject.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            ChildRoundedBox("SlipperOutsole", rootObject.transform, Vector3.zero,
                new Vector3(.23f, .055f, .50f), deepWood, .20f, false);
            ChildRoundedBox("SlipperFootbed", rootObject.transform, new Vector3(0f, .052f, -.015f),
                new Vector3(.205f, .040f, .46f), linen, .20f, false);
            ChildRoundedBox("SlipperUpper", rootObject.transform, new Vector3(0f, .105f, .10f),
                new Vector3(.225f, .11f, .21f), softBlue, .18f, false);
            ChildBox("SlipperUpperSeam", rootObject.transform, new Vector3(0f, .166f, .10f),
                new Vector3(.012f, .012f, .17f), softWhite, false);
        }

        private Material CreateColdAirParticleMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            Material material = new(shader) { name = "Chapter3_ColdAirParticle" };
            Texture2D texture = new(32, 32, TextureFormat.RGBA32, false)
            {
                name = "Chapter3_ColdAirSoftDisc",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float nx = (x + .5f) / 16f - 1f;
                float ny = (y + .5f) / 16f - 1f;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny)), 2.2f);
                pixels[y * 32 + x] = new Color(.76f, .95f, 1f, alpha * .34f);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            Color tint = new(.72f, .94f, 1f, .30f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)RenderQueue.Transparent + 5;
            return material;
        }

        private void CreateSideDoorFrame(Vector3 origin, float clearWidth)
        {
            float half = clearWidth * .5f;
            Box("BathroomDoorFrameFront", origin + new Vector3(-.03f, 1.15f, -half - .06f), new Vector3(.22f, 2.30f, .12f), deepWood, false);
            Box("BathroomDoorFrameRear", origin + new Vector3(-.03f, 1.15f, half + .06f), new Vector3(.22f, 2.30f, .12f), deepWood, false);
            Box("BathroomDoorFrameTop", origin + new Vector3(-.03f, 2.33f, 0f), new Vector3(.22f, .14f, clearWidth + .24f), deepWood, false);
        }

        private void CreateSideOpeningDoor(Vector3 origin, float width)
        {
            GameObject pivot = new("BathroomDoorPivot");
            pivot.transform.SetParent(root, false);
            pivot.transform.position = origin;
            // Open sliding leaf: visually present in the wall pocket, but its
            // collision proxy is disabled so it cannot form another hidden choke.
            ChildBox("BathroomDoorLeaf", pivot.transform, new Vector3(0f, 1.14f, 0f), new Vector3(.08f, 2.28f, width), oldWood, false);
            ChildSphere("BathroomDoorHandle", pivot.transform, new Vector3(-.08f, 1.10f, -.42f), Vector3.one * .045f, brass);
            for (int row = 0; row < 3; row++)
                ChildBox("BathroomDoorInset", pivot.transform, new Vector3(-.048f, .50f + row * .62f, 0f),
                    new Vector3(.025f, .37f, width * .72f), deepWood, false);
        }

        private void CreatePlaque(string value, Vector3 position, Quaternion rotation, Vector2 size, float characterSize, Color textColor, bool backing = true)
        {
            GameObject plaque = new("ProtectedPlaque_" + value);
            plaque.transform.SetParent(root, false);
            plaque.transform.position = position;
            plaque.transform.rotation = rotation;
            if (backing)
            {
                ChildRoundedBox("PlaqueBacking", plaque.transform, Vector3.zero, new Vector3(size.x, size.y, .075f), deepWood, .08f, false);
                ChildBox("PlaqueInnerFace", plaque.transform, new Vector3(0f, 0f, -.043f), new Vector3(size.x - .12f, size.y - .10f, .018f), oldWood, false);
            }
            GameObject textObject = new("PlaqueText_" + value);
            textObject.transform.SetParent(plaque.transform, false);
            // TextMesh faces local -Z. A 42 mm stand-off plus the plaque thickness
            // keeps glyph geometry clear at oblique first-person viewing angles.
            textObject.transform.localPosition = new Vector3(0f, 0f, backing ? -.064f : -.028f);
            Font font = worldFont ?? Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 64);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = value;
            text.font = font;
            text.fontSize = 64;
            text.characterSize = characterSize;
            text.color = textColor;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = worldTextMaterial != null ? worldTextMaterial : font.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 24;
        }

        private GameObject RoundedBox(string name, Vector3 position, Vector3 scale, Material material, float bevel, bool collider)
        {
            GameObject value = new(name);
            value.transform.SetParent(root, false);
            value.transform.position = position;
            value.transform.localScale = scale;
            MeshFilter filter = value.AddComponent<MeshFilter>();
            filter.sharedMesh = GetRoundedBoxMesh();
            MeshRenderer renderer = value.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            if (collider) value.AddComponent<BoxCollider>();
            return value;
        }

        private GameObject ChildRoundedBox(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, float bevel, bool collider)
        {
            GameObject value = new(name);
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            value.transform.localScale = localScale;
            MeshFilter filter = value.AddComponent<MeshFilter>();
            filter.sharedMesh = GetRoundedBoxMesh();
            MeshRenderer renderer = value.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            if (collider) value.AddComponent<BoxCollider>();
            return value;
        }

        private GameObject ChildBox(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool collider)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Cube);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            value.transform.localScale = localScale;
            value.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) RemoveCollider(value.GetComponent<Collider>());
            return value;
        }

        private GameObject ChildSphere(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            value.transform.localScale = localScale;
            value.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(value.GetComponent<Collider>());
            return value;
        }

        private GameObject AddInteractionProxy(GameObject parent, Vector3 localCenter, Vector3 worldSize, string name)
        {
            if (parent == null) return null;
            GameObject proxy = new(name);
            proxy.transform.SetParent(parent.transform, false);
            Vector3 parentScale = parent.transform.localScale;
            proxy.transform.localPosition = new Vector3(
                Mathf.Abs(parentScale.x) > .001f ? localCenter.x / parentScale.x : localCenter.x,
                Mathf.Abs(parentScale.y) > .001f ? localCenter.y / parentScale.y : localCenter.y,
                Mathf.Abs(parentScale.z) > .001f ? localCenter.z / parentScale.z : localCenter.z);
            BoxCollider collider = proxy.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(
                Mathf.Abs(parentScale.x) > .001f ? worldSize.x / Mathf.Abs(parentScale.x) : worldSize.x,
                Mathf.Abs(parentScale.y) > .001f ? worldSize.y / Mathf.Abs(parentScale.y) : worldSize.y,
                Mathf.Abs(parentScale.z) > .001f ? worldSize.z / Mathf.Abs(parentScale.z) : worldSize.z);
            return proxy;
        }

        private GameObject CylinderByDimensions(string name, Vector3 position, float diameter, float height, Material material, bool collider, Quaternion? rotation = null)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(root, false);
            cylinder.transform.position = position;
            cylinder.transform.localScale = new Vector3(diameter * .5f, height * .5f, diameter * .5f);
            cylinder.transform.rotation = rotation ?? Quaternion.identity;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) RemoveCollider(cylinder.GetComponent<Collider>());
            return cylinder;
        }

        private GameObject BeamBetween(string name, Vector3 from, Vector3 to, float diameter, Material material, bool collider)
        {
            Vector3 delta = to - from;
            return CylinderByDimensions(name, (from + to) * .5f, diameter, delta.magnitude, material, collider,
                Quaternion.FromToRotation(Vector3.up, delta.normalized));
        }

        private void CreateDetailedTree(Vector3 position, float scale, int variant)
        {
            GameObject tree = new("DetailedCourtyardTree");
            tree.transform.SetParent(root, false);
            tree.transform.position = position;
            CapsuleCollider collision = tree.AddComponent<CapsuleCollider>();
            collision.radius = .42f * scale;
            collision.height = 3.5f * scale;
            collision.center = Vector3.up * 1.75f * scale;
            CylinderByDimensions("DetailedTreeTrunk", position + Vector3.up * 1.72f * scale, .42f * scale, 3.45f * scale, deepWood, false);
            Vector3 crown = position + Vector3.up * 3.35f * scale;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 1.047f + variant * .31f;
                Vector3 end = crown + new Vector3(Mathf.Cos(angle) * 1.25f, .35f + (i % 2) * .42f, Mathf.Sin(angle) * 1.25f) * scale;
                BeamBetween("NaturalTreeBranch", crown - Vector3.up * .25f * scale, end, .12f * scale, deepWood, false);
                CreateAtlasCross("LeafCanopy", end, new Vector2(1.85f, 1.35f) * scale, (variant + i) % 4, (variant + i) % 2, 4, 2,
                    Quaternion.Euler(0f, i * 23f, 0f));
            }
            CreateAtlasCross("LeafCanopyCore", crown + Vector3.up * .75f * scale, new Vector2(2.35f, 1.70f) * scale,
                variant % 4, variant % 2, 4, 2, Quaternion.identity);
        }

        private void CreateAtlasCross(string name, Vector3 position, Vector2 size, int column, int row, int columns, int rows, Quaternion rotation)
        {
            GameObject holder = new(name);
            holder.transform.SetParent(root, false);
            holder.transform.position = position;
            holder.transform.rotation = rotation;
            for (int i = 0; i < 3; i++)
            {
                GameObject card = new(name + "_Card");
                card.transform.SetParent(holder.transform, false);
                card.transform.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
                card.transform.localScale = new Vector3(size.x, size.y, 1f);
                MeshFilter filter = card.AddComponent<MeshFilter>();
                filter.sharedMesh = GetAtlasQuadMesh(column, row, columns, rows);
                MeshRenderer renderer = card.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = foliageCards;
                renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                renderer.receiveShadows = true;
            }
        }

        private void CreateHomestayFoliageCross(string name, Vector3 position, Vector2 size,
            int column, int row, Quaternion rotation)
        {
            GameObject holder = new(name);
            holder.transform.SetParent(root, false);
            holder.transform.position = position;
            holder.transform.rotation = rotation;
            for (int i = 0; i < 3; i++)
            {
                GameObject card = new(name + "_Card");
                card.transform.SetParent(holder.transform, false);
                card.transform.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
                card.transform.localScale = new Vector3(size.x, size.y, 1f);
                MeshFilter filter = card.AddComponent<MeshFilter>();
                filter.sharedMesh = GetAtlasQuadMesh(column, row, 4, 2);
                MeshRenderer renderer = card.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = homestayFoliageCards ?? foliageCards;
                renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                renderer.receiveShadows = true;
            }
        }

        private void CreateDecorPanel(string name, Vector3 position, Quaternion rotation,
            Vector2 size, int column, int row, int columns, int rows)
        {
            GameObject backing = RoundedBox(name + "_Frame", position, new Vector3(size.x + .12f, size.y + .12f, .055f),
                deepWood, .035f, false);
            backing.transform.rotation = rotation;
            GameObject image = new(name + "_Image");
            image.transform.SetParent(root, false);
            image.transform.position = position + rotation * new Vector3(0f, 0f, -.038f);
            image.transform.rotation = rotation;
            image.transform.localScale = new Vector3(size.x, size.y, 1f);
            MeshFilter filter = image.AddComponent<MeshFilter>();
            filter.sharedMesh = GetAtlasQuadMesh(column, row, columns, rows);
            MeshRenderer renderer = image.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = interiorDecorCards;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 5;
        }

        private void CreateArchitectureDecal(string name, Vector3 position, Quaternion rotation,
            Vector2 size, int column, int row)
        {
            CreateAtlasDecal(name, position, rotation, size, architectureDecals ?? villageDecals,
                column, row, 4, 4);
        }

        private void CreateGroundDecal(string name, Vector3 position, Vector2 size, int column, int row, int columns, int rows)
        {
            CreateAtlasDecal(name, position, Quaternion.Euler(90f, 0f, 0f), size, surfaceDecals, column, row, columns, rows);
        }

        private void CreateWallDecal(string name, Vector3 position, Quaternion rotation, Vector2 size, int column, int row, int columns, int rows)
        {
            CreateAtlasDecal(name, position, rotation, size, villageDecals, column, row, columns, rows);
        }

        private void CreateAtlasDecal(string name, Vector3 position, Quaternion rotation, Vector2 size, Material material, int column, int row, int columns, int rows)
        {
            GameObject decal = new(name);
            decal.transform.SetParent(root, false);
            decal.transform.position = position;
            decal.transform.rotation = rotation;
            decal.transform.localScale = new Vector3(size.x, size.y, 1f);
            MeshFilter filter = decal.AddComponent<MeshFilter>();
            filter.sharedMesh = GetAtlasQuadMesh(column, row, columns, rows);
            MeshRenderer renderer = decal.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private Mesh GetAtlasQuadMesh(int column, int row, int columns, int rows)
        {
            int key = column | (row << 4) | (columns << 8) | (rows << 12);
            if (atlasQuadMeshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;
            float u0 = (float)column / columns;
            float u1 = (float)(column + 1) / columns;
            float v0 = 1f - (float)(row + 1) / rows;
            float v1 = 1f - (float)row / rows;
            Mesh mesh = new() { name = $"Chapter3_AtlasQuad_{column}_{row}_{columns}_{rows}" };
            mesh.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f), new Vector3(.5f, .5f), new Vector3(-.5f, .5f) };
            mesh.uv = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            atlasQuadMeshes[key] = mesh;
            return mesh;
        }

        private Mesh GetRoundedBoxMesh()
        {
            if (roundedBoxMesh != null) return roundedBoxMesh;
            const int segments = 4;
            const float radius = .075f;
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();
            Vector3[] faceNormals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            Vector3[] faceUs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.right, Vector3.right, Vector3.left };
            foreach (Vector3 normal in faceNormals)
            {
                Vector3 axisU = faceUs[System.Array.IndexOf(faceNormals, normal)];
                Vector3 axisV = Vector3.Cross(normal, axisU);
                int start = vertices.Count;
                for (int y = 0; y <= segments; y++)
                for (int x = 0; x <= segments; x++)
                {
                    float u = x / (float)segments;
                    float v = y / (float)segments;
                    Vector3 p = normal * .5f + axisU * (u - .5f) + axisV * (v - .5f);
                    Vector3 inner = new(.5f - radius, .5f - radius, .5f - radius);
                    Vector3 clamped = new(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    Vector3 delta = p - clamped;
                    Vector3 rounded = delta.sqrMagnitude > .000001f ? clamped + delta.normalized * radius : p;
                    vertices.Add(rounded);
                    normals.Add(delta.sqrMagnitude > .000001f ? delta.normalized : normal);
                    uvs.Add(new Vector2(u, v));
                }
                int stride = segments + 1;
                for (int y = 0; y < segments; y++)
                for (int x = 0; x < segments; x++)
                {
                    int a = start + y * stride + x;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(b); triangles.Add(d); triangles.Add(c);
                }
            }
            roundedBoxMesh = new Mesh { name = "Chapter3_RoundedBoxMesh" };
            roundedBoxMesh.SetVertices(vertices);
            roundedBoxMesh.SetNormals(normals);
            roundedBoxMesh.SetUVs(0, uvs);
            roundedBoxMesh.SetTriangles(triangles, 0);
            roundedBoxMesh.RecalculateBounds();
            return roundedBoxMesh;
        }

        private GameObject InstantiateVillageAsset(string resourceName, Vector3 position, float yaw, float targetHeight, Material overrideMaterial)
        {
            GameObject asset = Resources.Load<GameObject>("Models/Village/" + resourceName);
            if (asset == null) return null;
            GameObject holder = new("Distant_" + resourceName);
            holder.transform.SetParent(root, false);
            holder.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            GameObject model = Instantiate(asset, holder.transform);
            model.name = resourceName;
            foreach (Collider importedCollider in model.GetComponentsInChildren<Collider>(true)) RemoveCollider(importedCollider);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = overrideMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            Bounds bounds = CalculateRendererBounds(holder);
            float scale = bounds.size.y > .001f ? targetHeight / bounds.size.y : 1f;
            holder.transform.localScale = Vector3.one * scale;
            Bounds scaled = CalculateRendererBounds(holder);
            holder.transform.position = position + Vector3.up * -scaled.min.y;
            return holder;
        }

        private void CreateReflectionProbe(string name, Vector3 position, Vector3 size)
        {
            GameObject probeObject = new(name);
            probeObject.transform.SetParent(root, false);
            probeObject.transform.position = position;
            ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = 64;
            probe.size = size;
            probe.boxProjection = true;
            probe.intensity = .82f;
            probe.shadowDistance = 28f;
        }

        private static Bounds CalculateRendererBounds(GameObject value)
        {
            Renderer[] renderers = value.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(value.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Material TexturedMaterial(string name, Texture2D texture, Color tint, float smoothness, Vector2 tiling)
        {
            Material value = Material(name, tint, smoothness);
            if (texture == null) return value;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 8;
            if (value.HasProperty("_BaseMap"))
            {
                value.SetTexture("_BaseMap", texture);
                value.SetTextureScale("_BaseMap", tiling);
            }
            else value.mainTexture = texture;
            return value;
        }

        private static Material CutoutMaterial(string name, Texture2D texture, float cutoff)
        {
            Material value = Material(name, Color.white, .12f, false);
            if (texture != null)
            {
                if (value.HasProperty("_BaseMap")) value.SetTexture("_BaseMap", texture);
                else value.mainTexture = texture;
            }
            if (value.HasProperty("_AlphaClip")) value.SetFloat("_AlphaClip", 1f);
            if (value.HasProperty("_Cutoff")) value.SetFloat("_Cutoff", cutoff);
            if (value.HasProperty("_Cull")) value.SetFloat("_Cull", 0f);
            if (value.HasProperty("_Surface")) value.SetFloat("_Surface", 0f);
            if (value.HasProperty("_ZWrite")) value.SetFloat("_ZWrite", 1f);
            if (value.HasProperty("_SrcBlend")) value.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (value.HasProperty("_DstBlend")) value.SetFloat("_DstBlend", (float)BlendMode.Zero);
            value.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            value.EnableKeyword("_ALPHATEST_ON");
            value.renderQueue = (int)RenderQueue.AlphaTest;
            return value;
        }

        private static Material TransparentAtlasMaterial(string name, Texture2D atlas, Vector2 offset,
            Color tint, float smoothness)
        {
            Material value = Material(name, tint, smoothness, true);
            if (atlas != null && value.HasProperty("_BaseMap"))
            {
                value.SetTexture("_BaseMap", atlas);
                value.SetTextureScale("_BaseMap", new Vector2(.5f, .5f));
                value.SetTextureOffset("_BaseMap", offset);
            }
            if (value.HasProperty("_Cull")) value.SetFloat("_Cull", 0f);
            return value;
        }

        private static void SetMetallic(Material value, float metallic)
        {
            if (value != null && value.HasProperty("_Metallic")) value.SetFloat("_Metallic", metallic);
        }

        private Material chromeMaterial()
        {
            return blackMetal != null ? blackMetal : brass;
        }
    }
}
