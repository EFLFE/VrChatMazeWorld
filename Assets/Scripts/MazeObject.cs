using UdonSharp;

/// <summary>
/// Abstract class.
/// </summary>
public class MazeObject : UdonSharpBehaviour {
    protected MazeController Controller { get; private set; }

    public virtual void Init(MazeController controller) {
        Controller = controller;
    }

    public virtual void OnReturnToPool() { }
}
