using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MazeGenerator : UdonSharpBehaviour {
    [SerializeField] private int size = 49;

    private MazeController controller;
    private int[][] rooms_ids;
    private RoomTypeEnum[][] rooms_types;

    private int[][] temp_rooms_ids;
    private RoomTypeEnum[][] temp_rooms_types;

    private int OldRandom(int min_inclusive, int max_exclusive) {
        return Random.Range(min_inclusive, max_exclusive - 1);
    }

    public void Init(MazeController controller) {
        this.controller = controller;
    }

    public RoomTypeEnum[][] Generate(int seed) {
        Random.InitState(seed);
        // 0 = nothing
        // 1 - room, 2 - corridor
        controller.Utils.CreateJaggedArrayOfarrays(out rooms_ids, size, size);
        controller.Utils.CreateJaggedArrayOfarrays(out rooms_types, size, size);
        controller.Utils.CreateJaggedArrayOfarrays(out temp_rooms_ids, size, size);
        controller.Utils.CreateJaggedArrayOfarrays(out temp_rooms_types, size, size);

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

    private void MakeRoom(int id, RoomTypeEnum type, int start_x, int start_y, int size_x, int size_y) {
        for (int x = start_x; x < start_x + size_x; x++) {
            for (int y = start_y; y < start_y + size_y; y++) {
                rooms_ids[x][y] = id;
                rooms_types[x][y] = type;
            }
        }
    }

    private bool TryToMakeRoomFromCenter(int id, RoomTypeEnum type, int center_x, int center_y, int size_x, int size_y) {
        return TryToMakeRoom(id, type, center_x - size_x / 2, center_y - size_y / 2, size_x, size_y);
    }

    private bool TryToMakeRoom(int id, RoomTypeEnum type, int start_x, int start_y, int size_x, int size_y) {
        if (start_x <= 0) return false;
        if (start_y <= 0) return false;

        if (start_x + size_x >= size) return false;
        if (start_y + size_y >= size) return false;

        for (int x = start_x; x < start_x + size_x; x++) {
            for (int y = start_y; y < start_y + size_y; y++) {
                if (rooms_types[x][y] == RoomTypeEnum.Room) return false;
            }
        }
        for (int x = start_x; x < start_x + size_x; x++) {
            for (int y = start_y; y < start_y + size_y; y++) {
                rooms_ids[x][y] = id;
                rooms_types[x][y] = type;
            }
        }
        return true;
    }

    private int RandomSign() {
        return (OldRandom(0, 2) == 0) ? +1 : -1;
    }

    private void Save() {
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                temp_rooms_ids[x][y] = rooms_ids[x][y];
                temp_rooms_types[x][y] = rooms_types[x][y];
            }
        }
    }

    private void Restore() {
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                rooms_ids[x][y] = temp_rooms_ids[x][y];
                rooms_types[x][y] = temp_rooms_types[x][y];
            }
        }
    }

    [RecursiveMethod]
    private bool GenerateRoomsAndPassagesRecirsevly(
       int room_center_x,
       int room_center_y,
       int room_size_x,
       int room_size_y
       ) {
        bool result = TryToMakeRoomFromCenter(1, RoomTypeEnum.Room, room_center_x, room_center_y, room_size_x, room_size_y);
        if (!result) return false;

        int room_start_x = room_center_x - room_size_x / 2;
        int room_start_y = room_center_y - room_size_y / 2;

        for (int direction = 1; direction <= 4; direction++) {
            Save();
            bool result_of_passage = GeneratePassagesRecurcively(
                        direction,
                        room_start_x,
                        room_start_y,
                        room_size_x,
                        room_size_y
                    );
            if (!result_of_passage) {
                Restore();
            }
        }

        return true;
    }

    [RecursiveMethod]
    private bool GeneratePassagesRecurcively(
           int direction,
           int room_start_x,
           int room_start_y,
           int room_size_x,
           int room_size_y
       ) {
        int dx = (direction == 1) ? 1 : (direction == 2) ? -1 : 0;
        int dy = (direction == 3) ? 1 : (direction == 4) ? -1 : 0;

        int room_end_x = room_start_x + room_size_x;
        int room_end_y = room_start_y + room_size_y;

        // случайная точка на периметре стороне комнаты
        int x = (dx == 1) ? room_start_x + room_size_x - 1 : (dx == -1) ? room_start_x : OldRandom(room_start_x, room_start_x + room_size_x);
        int y = (dy == 1) ? room_start_y + room_size_y - 1 : (dy == -1) ? room_start_y : OldRandom(room_start_y, room_start_y + room_size_y);

        // случайная длинна коридора
        int passage_sublen = OldRandom(4, 7);


        for (int i = 0; i < passage_sublen; i++) {
            x += dx;
            y += dy;
            if (x <= 0) return false;
            if (y <= 0) return false;
            if (x >= size) return false;
            if (y >= size) return false;
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
        passage_sublen = OldRandom(2, 4);

        for (int i = 0; i < passage_sublen; i++) {
            x += dx_short;
            y += dy_short;
            if (x <= 0) return false;
            if (y <= 0) return false;
            if (x >= size) return false;
            if (y >= size) return false;
            if (rooms_types[x][y] == RoomTypeEnum.Room) return false;
            if (rooms_types[x][y] == RoomTypeEnum.Corridor) return false;
            //if (rooms_types[x, y] == 0) {
            rooms_ids[x][y] = 99;
            rooms_types[x][y] = RoomTypeEnum.Corridor;
            //}
        }


        // случайная длинна коридора
        passage_sublen = OldRandom(4, 7);
        for (int i = 0; i < passage_sublen; i++) {
            x += dx;
            y += dy;
            if (x <= 0) return false;
            if (y <= 0) return false;
            if (x >= size) return false;
            if (y >= size) return false;
            if (rooms_types[x][y] == RoomTypeEnum.Room) return false;
            if (rooms_types[x][y] == RoomTypeEnum.Corridor) return false;
            //if (rooms_types[x, y] == 0) {
            rooms_ids[x][y] = 99;
            rooms_types[x][y] = RoomTypeEnum.Corridor;
            //}
        }

        return GenerateRoomsAndPassagesRecirsevly(x, y, OldRandom(3, 6), OldRandom(3, 6));
    }
}
