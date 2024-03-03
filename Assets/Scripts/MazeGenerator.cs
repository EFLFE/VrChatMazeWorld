using UdonSharp;
using UnityEngine;

public class MazeGenerator : UdonSharpBehaviour
{
    [SerializeField, Range(30, 512)]
    private int size = 49;

    private MazeController controller;
    private int[][] rooms_ids;
    private RoomTypeEnum[][] rooms_types;

    public void Init(MazeController controller)
    {
        this.controller = controller;
    }

    public RoomTypeEnum[][] Generate(int seed)
    {
        Random.InitState(seed);
        // 0 = nothing
        // 1 - room, 2 - corridor
        controller.Utils.CreateJaggedArrayOfarrays(out rooms_ids, size, size);
        controller.Utils.CreateJaggedArrayOfarrays(out rooms_types, size, size);

        // центральная комната
        int room_center_x = size / 2;
        int room_center_y = size / 2;
        int room_size_x = 5;
        int room_size_y = 5;
        //MakeRoom(1, 1, room_start_x, room_start_y, room_size_x, room_size_y);

        GenerateRoomsAndPassagesRecirsevly(room_center_x, room_center_y, room_size_x, room_size_y);

        // случайная точка внутри комнаты
        //int x = Random.Range(room_start_x, room_start_x + room_size_x);
        //int y = Random.Range(room_start_y, room_start_y + room_size_y);

        // случайное направление
        // int direction = Random.Range(1, 4);

        return rooms_types;
    }

    private void MakeRoom(int id, RoomTypeEnum type, int start_x, int start_y, int size_x, int size_y)
    {
        for (int x = start_x; x < start_x + size_x; x++)
        {
            for (int y = start_y; y < start_y + size_y; y++)
            {
                rooms_ids[x][y] = id;
                rooms_types[x][y] = type;
            }
        }
    }

    private bool TryToMakeRoomFromCenter(int id, RoomTypeEnum type, int center_x, int center_y, int size_x, int size_y)
    {
        return TryToMakeRoom(id, type, center_x - size_x / 2, center_y - size_y / 2, size_x, size_y);
    }

    private bool TryToMakeRoom(int id, RoomTypeEnum type, int start_x, int start_y, int size_x, int size_y)
    {
        if (start_x <= size_x) return false;
        if (start_y <= size_y) return false;

        if (start_x + size_x >= size) return false;
        if (start_y + size_y >= size) return false;

        for (int x = start_x; x < start_x + size_x; x++)
        {
            for (int y = start_y; y < start_y + size_y; y++)
            {
                if (rooms_types[x][y] == RoomTypeEnum.Room) return false;
            }
        }
        for (int x = start_x; x < start_x + size_x; x++)
        {
            for (int y = start_y; y < start_y + size_y; y++)
            {
                rooms_ids[x][y] = id;
                rooms_types[x][y] = type;
            }
        }
        return true;
    }

    private int RandomSign()
    {
        return (Random.Range(0, 2) == 0) ? +1 : -1;
    }

    private bool GenerateRoomsAndPassagesRecirsevly(
       int room_center_x,
       int room_center_y,
       int room_size_x,
       int room_size_y
       )
    {
        bool result = TryToMakeRoomFromCenter(1, RoomTypeEnum.Room, room_center_x, room_center_y, room_size_x, room_size_y);
        if (!result) return false;

        int room_start_x = room_center_x - room_size_x / 2;
        int room_start_y = room_center_y - room_size_y / 2;

        for (int direction = 1; direction <= 4; direction++)
        {

            //int[][] temp_rooms_ids = new int[size, size];
            //int[][] temp_rooms_types = new int[size, size];
            //Array.Copy(rooms_ids, temp_rooms_ids, size);

            int[][] temp_rooms_ids = (int[][])rooms_ids.Clone();
            int[][] temp_rooms_types = (int[][])rooms_types.Clone();

            bool result_of_passage = GeneratePassagesRecurcively(
                        direction,
                        room_start_x,
                        room_start_y,
                        room_size_x,
                        room_size_y
                    );

            if (!result_of_passage)
            {
                rooms_ids = (int[][])temp_rooms_ids.Clone();
                rooms_types = (RoomTypeEnum[][])temp_rooms_types.Clone();
            }
        }

        return true;
    }

    private bool GeneratePassagesRecurcively(
           int direction,
           int room_start_x,
           int room_start_y,
           int room_size_x,
           int room_size_y
       )
    {
        int dx = (direction == 1) ? 1 : (direction == 2) ? -1 : 0;
        int dy = (direction == 3) ? 1 : (direction == 4) ? -1 : 0;

        // случайная точка на периметре стороне комнаты
        int x = (dx == 1) ? room_start_x + room_size_x - 1 : (dx == -1) ? room_start_x : Random.Range(room_start_x, room_start_x + room_size_x);
        int y = (dy == 1) ? room_start_y + room_size_y - 1 : (dy == -1) ? room_start_y : Random.Range(room_start_y, room_start_y + room_size_y);

        // случайная длинна коридора
        int passage_sublen = Random.Range(4, 7);


        for (int i = 0; i < passage_sublen; i++)
        {
            x += dx;
            y += dy;
            if (x <= 0) break;
            if (y <= 0) break;
            if (x >= size) break;
            if (y >= size) break;
            if (rooms_types[x][y] == RoomTypeEnum.Room) return false;
            if (rooms_types[x][y] == RoomTypeEnum.Corridor) return false;
            //if (rooms_types[x, y] == 0) {
            rooms_ids[x][y] = 99;
            rooms_types[x][y] = RoomTypeEnum.Corridor;
            //}
        }

        // смена направления
        int dx_short = dy * RandomSign();
        int dy_short = dx * RandomSign();
        // укороченный корридор
        passage_sublen = Random.Range(2, 4);

        for (int i = 0; i < passage_sublen; i++)
        {
            x += dx_short;
            y += dy_short;
            if (x <= 0) break;
            if (y <= 0) break;
            if (x >= size) break;
            if (y >= size) break;
            if (rooms_types[x][y] == RoomTypeEnum.Room) return false;
            if (rooms_types[x][y] == RoomTypeEnum.Corridor) return false;
            //if (rooms_types[x, y] == 0) {
            rooms_ids[x][y] = 99;
            rooms_types[x][y] = RoomTypeEnum.Corridor;
            //}
        }


        // случайная длинна коридора
        passage_sublen = Random.Range(4, 7);
        for (int i = 0; i < passage_sublen; i++)
        {
            x += dx;
            y += dy;
            if (x <= 0) break;
            if (y <= 0) break;
            if (x >= size) break;
            if (y >= size) break;
            if (rooms_types[x][y] == RoomTypeEnum.Room) return false;
            if (rooms_types[x][y] == RoomTypeEnum.Corridor) return false;
            //if (rooms_types[x, y] == 0) {
            rooms_ids[x][y] = 99;
            rooms_types[x][y] = RoomTypeEnum.Corridor;
            //}
        }

        return GenerateRoomsAndPassagesRecirsevly(x, y, Random.Range(3, 6), Random.Range(3, 6));
    }
}
