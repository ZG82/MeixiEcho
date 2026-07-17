namespace MeixiEcho
{
    /// <summary>
    /// Scene-serializable camera component. Unity requires a MonoBehaviour used in
    /// a saved scene to have a script asset whose filename matches the class name.
    /// The implementation remains in the player camera module for compatibility.
    /// </summary>
    [UnityEngine.DisallowMultipleComponent]
    [UnityEngine.RequireComponent(typeof(UnityEngine.Camera))]
    public sealed partial class ThirdPersonOrbitCamera
    {
    }
}
