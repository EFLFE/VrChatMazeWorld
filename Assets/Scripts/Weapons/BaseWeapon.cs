using UdonSharp;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BaseWeapon : MazeObject
{
    public bool CanDamage { get; protected set; }

}
