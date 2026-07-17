namespace MeixiEcho
{
    /// <summary>
    /// Scene-serializable procedural pose component. Keeping the concrete type in
    /// its own matching script file prevents Unity from treating it as the player
    /// controller and adding an unwanted CharacterController to every visual model.
    /// </summary>
    public sealed class ProceduralHumanoidFallback : ProceduralHumanoidFallbackBase
    {
    }
}
