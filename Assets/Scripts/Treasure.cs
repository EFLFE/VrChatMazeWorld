public class Treasure : MazeObject {
    public int value = 100;

    public void Despawn() {
        Controller.OnTreasureGathered();
        Controller.GetChestPool.Return(this);
    }
}
