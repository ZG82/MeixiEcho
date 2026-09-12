#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeixiEcho.Editor.Tests
{
    public sealed class ChapterThreeDetailedSceneTests
    {
        private const string ScenePath = "Assets/Scenes/MeixiChapterThree.unity";

        [Test]
        public void DetailedScene_HasTwoStoreysFacilitiesOpenDoorwaysAndDepthTestedLabels()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                List<Transform> all = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .ToList();

                Assert.That(all.Count(t => t.name == "GroundFloorExteriorGallery"), Is.EqualTo(1));
                Assert.That(all.Count(t => t.name == "SecondFloorExteriorGallery"), Is.EqualTo(1));
                Assert.That(all.Count(t => t.name == "OutwardOpeningGuestDoor"), Is.EqualTo(6));
                Assert.That(all.Where(t => t.name == "OutwardOpeningGuestDoor")
                    .Count(t => t.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "ChapterThreeInteractable")), Is.EqualTo(6));
                Assert.That(all.Where(t => t.name == "OutwardOpeningGuestDoor")
                    .All(t => Mathf.Abs(t.localEulerAngles.y) < .5f), Is.True,
                    "Guest doors must be serialized closed and let the interactable drive the swing");
                Assert.That(all.Where(t => t.name == "OutwardOpeningGuestDoor")
                    .All(t => t.GetComponents<Component>().Any(c => c != null &&
                        c.GetType().GetProperty("GuestDoorOpenAngle") != null)), Is.True,
                    "Guest doors need a non-zero animation target angle property");
                Assert.That(all.Where(t => t.name == "SolidWoodDoorLeaf")
                    .Count(t => t.GetComponent<Collider>() != null), Is.EqualTo(6));
                Assert.That(all.Count(t => t.name == "TelevisionHousing"), Is.EqualTo(6));
                Assert.That(all.Count(t => t.name == "InteractiveWardrobe"), Is.EqualTo(6));
                Assert.That(all.Count(t => t.name == "GuestRoomCeilingPanel"), Is.EqualTo(12));
                Assert.That(all.Count(t => t.name == "ApproachFencePost"), Is.EqualTo(12));
                Assert.That(all.Count(t => t.name == "InteractiveBedsideLamp"), Is.EqualTo(12));
                Assert.That(all.Count(t => t.name == "BedsideTable"), Is.EqualTo(12));
                Assert.That(all.Count(t => t.name == "WritingDeskTop"), Is.EqualTo(0));
                Assert.That(all.Count(t => t.name == "WindowDaylight"), Is.EqualTo(0),
                    "Windows must not contain a second transparent daylight plane that can flicker against the glass");
                Assert.That(all.Count(t => t.name == "WindowGlass"), Is.GreaterThanOrEqualTo(6),
                    "Every facade window needs one real transparent glass pane");
                Assert.That(all.Where(t => t.name == "BathroomFloor").All(t =>
                    t.GetComponent<Renderer>() != null && t.GetComponent<Renderer>().bounds.size.x < 6.6f &&
                    t.GetComponent<Renderer>().bounds.size.z < 4.2f), Is.True,
                    "The bathroom floor must stay within the one-quarter rear wet-room footprint");
                Assert.That(all.Count(t => t.name == "AirConditioner"), Is.EqualTo(6));
                Assert.That(all.Where(t => t.name == "AirConditioner")
                    .Count(t => t.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "ChapterThreeInteractable")), Is.EqualTo(6));
                Assert.That(all.Count(t => t.name == "AcStatusDisplay"), Is.EqualTo(6));
                Assert.That(all.Count(t => t.name == "BathroomFloor"), Is.EqualTo(6));
                Assert.That(all.Count(t => t.name == "ToiletCistern"), Is.EqualTo(6));
                Assert.That(all.Count(t => t.name == "ShowerTray"), Is.EqualTo(6));
                Assert.That(all.Where(t => t.name == "TelevisionHousing")
                    .Count(t => t.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "ChapterThreeInteractable")), Is.EqualTo(6));
                Assert.That(all.Where(t => t.name == "InteractiveWardrobe")
                    .Count(t => t.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "ChapterThreeInteractable")), Is.EqualTo(6));
                Assert.That(all.Where(t => t.name == "InteractiveWardrobe").All(t => t.GetComponent<BoxCollider>() != null), Is.True,
                    "Every wardrobe needs a front-facing interaction proxy");
                Assert.That(all.Where(t => t.name == "InteractiveWardrobe")
                    .All(t => Vector3.Dot(-t.forward, Vector3.right) > .99f), Is.True,
                    "Wardrobe doors must face the bed on the right side of the room");
                Assert.That(all.Where(t => t.name == "InteractiveWardrobe")
                    .All(t => Mathf.Abs(Mathf.DeltaAngle(t.localEulerAngles.y, 270f)) < .5f), Is.True,
                    "Wardrobe carcasses must keep the ninety-degree bed-facing orientation");
                Assert.That(all.Where(t => t.name == "InteractiveBedsideLamp")
                    .Count(t => t.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "ChapterThreeInteractable")), Is.EqualTo(12));
                Assert.That(all.Where(t => t.name == "ToiletCistern").All(t => t.GetComponent<Collider>() != null), Is.True,
                    "Every toilet cistern needs a collision proxy");
                Assert.That(all.Where(t => t.name == "ShowerTray").All(t => t.GetComponent<Collider>() != null), Is.True,
                    "Every shower tray needs a collision proxy");
                Renderer[] wetFloors = all.Where(t => t.name == "BathroomFloor")
                    .Select(t => t.GetComponent<Renderer>()).Where(r => r != null).ToArray();
                Collider[] guestBeds = all.Where(t => t.name == "BedFrame" || t.name == "DoubleGuestBed")
                    .Select(t => t.GetComponent<Collider>()).Where(c => c != null).ToArray();
                foreach (Collider bed in guestBeds)
                foreach (Renderer wetFloor in wetFloors)
                {
                    Bounds bedXZ = new Bounds(new Vector3(bed.bounds.center.x, 0f, bed.bounds.center.z),
                        new Vector3(bed.bounds.size.x, .01f, bed.bounds.size.z));
                    Bounds floorXZ = new Bounds(new Vector3(wetFloor.bounds.center.x, 0f, wetFloor.bounds.center.z),
                        new Vector3(wetFloor.bounds.size.x, .01f, wetFloor.bounds.size.z));
                    Assert.That(bedXZ.Intersects(floorXZ), Is.False, "Bed and bathroom floor footprints must not overlap");
                }
                Assert.That(all.Any(t => t.name == "LandscapeGround"), Is.True);
                Assert.That(all.Any(t => t.name == "RearServiceBluestone"), Is.True);
                Assert.That(all.Any(t => t.name == "HomestayApproachLane"), Is.True);
                Assert.That(all.Any(t => t.name == "FacadeLower"), Is.False, "Old solid facade would seal the entrance");

                BoxCollider[] facade = all.Where(t => t.name.StartsWith("Facade"))
                    .Select(t => t.GetComponent<BoxCollider>()).Where(c => c != null).ToArray();
                float[] rooms = { -6.90f, 0f, 6.90f };
                foreach (float baseY in new[] { 0f, 4.05f })
                foreach (float roomX in rooms)
                {
                    Vector3 doorwayCenter = new(roomX - 1.55f, baseY + 1.15f, 5.25f);
                    Assert.That(facade.Any(c => c.bounds.Contains(doorwayCenter)), Is.False,
                        $"Facade collider seals doorway at {doorwayCenter}");
                }

                BoxCollider floor = all.First(t => t.name == "GroundFloorContinuousSlab").GetComponent<BoxCollider>();
                Assert.That(floor.bounds.size.x, Is.GreaterThanOrEqualTo(20.6f));
                Assert.That(floor.bounds.size.z, Is.GreaterThanOrEqualTo(11.9f));
                Assert.That(all.Where(t => t.name == "BathroomDoorLeaf").All(t => t.GetComponent<Collider>() == null), Is.True,
                    "An open bathroom leaf must not block the 1.35m capsule route");
                BoxCollider[] luggageBenches = all.Where(t => t.name == "LuggageBench")
                    .Select(t => t.GetComponent<BoxCollider>()).Where(c => c != null).ToArray();
                BoxCollider[] footBenches = all.Where(t => t.name == "UpholsteredFootBench")
                    .Select(t => t.GetComponent<BoxCollider>()).Where(c => c != null).ToArray();
                foreach (BoxCollider luggage in luggageBenches)
                foreach (BoxCollider foot in footBenches)
                    if (Mathf.Abs(luggage.transform.position.y - foot.transform.position.y) < .8f)
                        Assert.That(luggage.bounds.Intersects(foot.bounds), Is.False,
                            "Luggage and foot benches must leave a clear movement lane");
                string[] bedNames = { "BedFrame", "DoubleGuestBed" };
                Assert.That(all.Count(t => bedNames.Contains(t.name) &&
                    t.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "ChapterThreeInteractable")), Is.EqualTo(6));

                TextMesh[] labels = all.Select(t => t.GetComponent<TextMesh>()).Where(t => t != null).ToArray();
                Assert.That(labels.Length, Is.GreaterThanOrEqualTo(9));
                foreach (TextMesh label in labels)
                {
                    MeshRenderer renderer = label.GetComponent<MeshRenderer>();
                    Assert.That(renderer, Is.Not.Null);
                    Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("MeixiEcho/WorldTextDepth"));
                }
            }
            finally
            {
                if (openedForTest && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
#endif
