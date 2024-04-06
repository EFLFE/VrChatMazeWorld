using UdonSharp;

public class Treasure : MazeObject {
    public int value = 100;

    [UdonSynced] private bool Taked;

    public override void OnReturnToPool() {
        base.OnReturnToPool();
        Taked = false;
    }

    public override void OnDeserialization() {
        base.OnDeserialization();
        if (Taked)
            Despawn();
    }

    public void Despawn() {
        Taked = true;
        Controller.OnTreasureGathered();
        Controller.GetChestPool.Return(this);
    }
}
