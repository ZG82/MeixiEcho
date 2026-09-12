using System.Collections.Generic;
using UnityEngine;

namespace MeixiEcho
{
    /// <summary>Replace visual geometry without moving authored interactions or collision volumes.</summary>
    public static class ChapterThreeCraftedFurniture
    {
        public static int Apply(Transform root)
        {
            var meshes = new Dictionary<string, Mesh>();
            int count = 0;
            foreach (MeshFilter target in root.GetComponentsInChildren<MeshFilter>(true))
            {
                string model = target.name switch
                {
                    "IndigoQuilt" => "TailoredQuilt",
                    "GuestPillow" => "SeamedPillow",
                    "WashBasin" => "PorcelainBasin",
                    "GuestRoomLampShade" => "PleatedLampshade",
                    "BedsideLampGlow" => "PleatedLampshade",
                    "CourtyardCeramicPlanter" => "CeramicPlanter",
                    _ => null
                };
                if (model == null) continue;
                if (!meshes.TryGetValue(model, out Mesh replacement))
                {
                    var asset = Resources.Load<GameObject>("ChapterThree/Models/" + model);
                    if (asset == null) continue;
                    var parts = asset.GetComponentsInChildren<MeshFilter>();
                    var combine = new CombineInstance[parts.Length];
                    for (int i=0;i<parts.Length;i++) combine[i] = new CombineInstance
                    { mesh=parts[i].sharedMesh, transform=asset.transform.worldToLocalMatrix*parts[i].transform.localToWorldMatrix };
                    replacement = new Mesh { name = "Crafted_"+model };
                    replacement.CombineMeshes(combine,true,true);
                    Bounds b=replacement.bounds;
                    var vertices=replacement.vertices;
                    for(int i=0;i<vertices.Length;i++) vertices[i]=new Vector3(
                        (vertices[i].x-b.center.x)/b.size.x,(vertices[i].y-b.center.y)/b.size.y,(vertices[i].z-b.center.z)/b.size.z);
                    replacement.vertices=vertices; replacement.RecalculateBounds(); replacement.RecalculateNormals();
                    meshes.Add(model,replacement);
                }
                target.sharedMesh=replacement; count++;
            }
            // The old solid insert filled the wash basin; the crafted mesh has a real cavity.
            if(meshes.ContainsKey("PorcelainBasin"))
                foreach(Transform t in root.GetComponentsInChildren<Transform>(true))
                    if(t.name=="WashBasinInner") t.gameObject.SetActive(false);
            return count;
        }
    }
}
