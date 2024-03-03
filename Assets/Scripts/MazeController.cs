
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MazeController : UdonSharpBehaviour
{
    public MazeBuilder Builder;
    public MazeGenerator Generator;
    public Utils Utils;

    public TMPro.TextMeshProUGUI debugText;

    [UdonSynced] private int seed;

    private RoomTypeEnum[][] maze;

    private void Start()
    {
        Generator.Init(this);

        bool isOwner = Networking.LocalPlayer.IsOwner(gameObject);
        if (isOwner)
        {
            debugText.text += "Owner\n";
            int newSeed = Random.Range(0, 9999);
            seed = newSeed;
            Build(newSeed);
        }
        RequestSerialization();
    }

    public override void OnDeserialization()
    {
        base.OnDeserialization();
        if (!Networking.LocalPlayer.IsOwner(gameObject))
        {
            Build(seed);
        }
    }

    private void Build(int seed)
    {
        debugText.text += $"Build seed: {seed}\n";

        maze = Generator.Generate(seed);
        //PrintRooms(rooms);
        Builder.BuildRooms(maze);

        //Vector2 pos = Builder.GetMainRoomPos(rooms);
        //Networking.LocalPlayer.TeleportTo(new Vector3(pos.x, 1, pos.y), Quaternion.identity);
    }

}
