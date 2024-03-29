using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X500;
using System;
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;

public enum Cell {
    Wall,
    Passage,
    Hole,
    Something,
    DoorEnterance,
    DoorExit,
    DoorDeadEnd,
}

public enum Room {
    Square,
    Cave,
    Turn
}

[RequireComponent(typeof(URandom))]
public class MazeV2 : UdonSharpBehaviour {
    [SerializeField] URandom udonRandom;

    public int Size => size;
    public int[][] GetIds => ids;
    public Cell[][] GetCells => cells;
    public Room[] GetRooms => rooms;

    public int current_id = 0;

    public int size = 49;
    private int max_rooms;

    private int[][] ids;
    private Cell[][] cells;
    private Room[] rooms;

    private int[][] ids_backup;
    private Cell[][] cells_backup;

    public int chests_amount;
    public int[] chests_x;
    public int[] chests_y;

    // ----------- PossibleDoors Stack
    private int[] possible_doors2_x;
    private int[] possible_doors2_y;
    private int[] possible_doors2_d; // d - direction, 0 - up, 1 - right, 2 - down, 3 - left
    private int possible_doors2_head = 0;
    private int possible_doors2_tail = 0;

    private int[][] cache_cells_x;
    private int[][] cache_cells_y;
    private int[] cache_cells_ammounts;

    public void PossibleDoorsPushToTail(int x, int y, int forward_direction) {
        possible_doors2_x[possible_doors2_tail] = x;
        possible_doors2_y[possible_doors2_tail] = y;
        possible_doors2_d[possible_doors2_tail] = forward_direction;
        possible_doors2_tail++;
    }

    public void PossibleDoorsPopFromHead(out int x, out int y, out int forward_direction) {
        x = possible_doors2_x[possible_doors2_head];
        y = possible_doors2_y[possible_doors2_head];
        forward_direction = possible_doors2_d[possible_doors2_head];
        possible_doors2_head++;
    }

    public int PossibleDoorsAmont() {
        return possible_doors2_tail - possible_doors2_head;
    }
    // ----------- PossibleDoors Stack

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RandomSign() {
        return (RandomInclusive(0, 1) % 2 == 0) ? +1 : -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RandomInclusive(int min_inclusive, int max_inclusive) {
        //return rnd.Next(min_inclusive, max_inclusive + 1);
        //return UnityEngine.Random.Range(min_inclusive, max_inclusive + 1);
        return udonRandom.Next(min_inclusive, max_inclusive + 1);
    }

    private int seed;
    public void ReSeed() {
        //udonRandom.SetSeed(seed);
        //seed = RandomInclusive(100000, 999999);
        //udonRandom.SetSeed(seed);
    }


    public void Init(int max_rooms, int seed, int chests_amount = 4) {
        this.seed = seed;
        udonRandom.SetSeed(seed);
        //rnd = new Random(seed);
        this.max_rooms = max_rooms;

        cache_cells_x = new int[max_rooms + 1][];
        cache_cells_y = new int[max_rooms + 1][];
        cache_cells_ammounts = new int[max_rooms + 1];

        possible_doors2_x = new int[max_rooms * 5];
        possible_doors2_y = new int[max_rooms * 5];
        possible_doors2_d = new int[max_rooms * 5]; // d - direction, 0 - up, 1 - right, 2 - down, 3 - left
        possible_doors2_head = 0;
        possible_doors2_tail = 0;

        rooms = new Room[max_rooms];

        ids = new int[size][];
        for (int i = 0; i < size; i++) ids[i] = new int[size];
        cells = new Cell[size][];
        for (int i = 0; i < size; i++) cells[i] = new Cell[size];

        /*
        ids_backup = new int[size][];
        for (int i = 0; i < size; i++) ids_backup[i] = new int[size];
        cells_backup = new Cell[size][];
        for (int i = 0; i < size; i++) cells_backup[i] = new Cell[size];
        */

        this.chests_amount = chests_amount;
        chests_x = new int[chests_amount];
        chests_y = new int[chests_amount];

        GenerateFirstRoom();
    }


    public void Backup() {
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                ids_backup[x][y] = ids[x][y];
                cells_backup[x][y] = cells[x][y];
            }
        }
    }

    public void Restore() {
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                ids[x][y] = ids_backup[x][y];
                cells[x][y] = cells_backup[x][y];
            }
        }
    }

    public void GenerateFirstRoom() {
        // make central room 5 by 5 with 4 doors
        current_id = 1;
        int halfSize = size / 2;

        for (int x = halfSize - 2; x <= halfSize + 2; x++) {
            for (int y = halfSize - 2; y <= halfSize + 2; y++) {
                ids[x][y] = 1;
                cells[x][y] = Cell.Passage;
            }
        }
        cells[halfSize - 2][halfSize] = Cell.DoorEnterance;
        cells[halfSize + 2][halfSize] = Cell.DoorEnterance;
        cells[halfSize][halfSize - 2] = Cell.DoorEnterance;
        cells[halfSize][halfSize + 2] = Cell.DoorEnterance;

        cells[halfSize - 3][halfSize] = Cell.DoorExit;
        cells[halfSize + 3][halfSize] = Cell.DoorExit;
        cells[halfSize][halfSize - 3] = Cell.DoorExit;
        cells[halfSize][halfSize + 3] = Cell.DoorExit;

        PossibleDoorsPushToTail(halfSize + 0, halfSize - 3, 0);
        PossibleDoorsPushToTail(halfSize + 3, halfSize + 0, 1);
        PossibleDoorsPushToTail(halfSize + 0, halfSize + 3, 2);
        PossibleDoorsPushToTail(halfSize - 3, halfSize + 0, 3);

        current_id++;
    }

    public bool Generate() {
        ReSeed();
        int steps = 1;
        while (PossibleDoorsAmont() > 0 && steps > 0 && current_id < max_rooms) {
            steps--;
            PossibleDoorsPopFromHead(out int x, out int y, out int d);
            if (TryToGenerateRoom(x, y, d)) {
                current_id++;
            }

            //ShowInConsole();
            //Console.ReadLine();

            // finishing
            if (current_id >= max_rooms) {
                // remove all possible doors
                while (PossibleDoorsAmont() > 0) {
                    PossibleDoorsPopFromHead(out x, out y, out d);
                    GetDirectionsVector(d, out int dx, out int dy);
                    if (ids[x][y] == 0) {
                        cells[x][y] = Cell.Wall;
                        cells[x - dx][y - dy] = Cell.DoorDeadEnd;
                    } else {
                        cells[x][y] = Cell.Passage;
                        cells[x - dx][y - dy] = Cell.Passage;
                    }
                }

                // spawn final chests
                for (int i = 0; i < chests_amount; i++) {
                    int room_id = max_rooms - 1 - i * 4;
                    GetRandomCellFromRoomByID(room_id, out int room_x, out int room_y);
                    chests_x[i] = room_x;
                    chests_y[i] = room_y;
                    if (cells[room_x][room_y] == Cell.Hole) {
                        cells[room_x][room_y] = Cell.Passage;
                    }
                }
            }
        }

        bool genIsReady = current_id >= max_rooms || PossibleDoorsAmont() <= 0;
        return genIsReady;
    }

    private void TryToSpawnRandomDoorsInRoomByID(int room_id) {
        int min_amount_of_doors = current_id <= 5 ? 3 : 2;
        int amount_of_doors = RandomInclusive(min_amount_of_doors, 4);
        for (int i = 0; i < amount_of_doors; i++) {
            bool result = TryToGetRandomCellFromRoomByIDOnTheEdge(room_id, out int door_x, out int door_y, out int door_d);
            if (!result) break;
            GetDirectionsVector(door_d, out int dx, out int dy);

            cells[door_x][door_y] = Cell.DoorEnterance;
            cells[door_x + dx][door_y + dy] = Cell.DoorExit;
            PossibleDoorsPushToTail(door_x + dx, door_y + dy, (door_d));
        }
    }

    private bool TryToGenerateRoom(int start_x, int start_y, int forward_direction) {
        if (cells[start_x][start_y] != Cell.DoorExit || ids[start_x][start_y] != 0) {
            return false;
        }

        int room_type = RandomInclusive(1, 5);
        if (room_type == 1 && current_id > 10) {
            // 20% chance
            if (TryToGenerateRoomTurn(start_x, start_y, forward_direction)) {
                rooms[current_id] = Room.Turn;
                return true;
            }
        } else {
            // 80% chance
            if (TryToGenerateRoomSquare(start_x, start_y, forward_direction)) {
                rooms[current_id] = Room.Square;
                return true;
            }
        }

        if (TryToGenerateRoomCave(start_x, start_y, forward_direction)) {
            rooms[current_id] = Room.Cave;
            return true;
        }
        return false;
    }

    private bool TryToGenerateRoomTurn(int start_x, int start_y, int dir1) {
        int tries = 2;
        GetDirectionsVector(dir1, out int dx1, out int dy1);

        while (tries > 0) {
            tries--;

            int len1 = RandomInclusive(2, 4);
            int middle_x = start_x + dx1 * len1;
            int middle_y = start_y + dy1 * len1;

            int start_middle_corner1_x = (start_x == middle_x) ? start_x - 1 : Math.Min(start_x, middle_x);
            int start_middle_corner1_y = (start_y == middle_y) ? start_y - 1 : Math.Min(start_y, middle_y);
            int start_middle_corner2_x = (start_x == middle_x) ? start_x + 1 : Math.Max(start_x, middle_x);
            int start_middle_corner2_y = (start_y == middle_y) ? start_y + 1 : Math.Max(start_y, middle_y);

            if (start_middle_corner1_x < 0 || start_middle_corner1_y < 0 || start_middle_corner2_x >= size || start_middle_corner2_y >= size) {
                continue; // next try
            }

            bool can_spawn_room = true;
            for (int x = start_middle_corner1_x; x <= start_middle_corner2_x; x++) {
                for (int y = start_middle_corner1_y; y <= start_middle_corner2_y; y++) {
                    if (ids[x][y] != 0) {
                        can_spawn_room = false;
                        x = start_middle_corner2_x + 1;
                        y = start_middle_corner2_y + 1;
                    }
                }
            }
            if (!can_spawn_room) {
                continue; // next try
            }

            int dir2 = GetRandomTurn(dir1);
            GetDirectionsVector(dir2, out int dx2, out int dy2);
            int len2 = RandomInclusive(2, 4);
            int end_x = middle_x + dx2 * len2;
            int end_y = middle_y + dy2 * len2;

            int middle_end_corner1_x = (end_x == middle_x) ? middle_x - 1 : Math.Min(end_x, middle_x) - 1;
            int middle_end_corner1_y = (end_y == middle_y) ? middle_y - 1 : Math.Min(end_y, middle_y) - 1;
            int middle_end_corner2_x = (end_x == middle_x) ? middle_x + 1 : Math.Max(end_x, middle_x) + 1;
            int middle_end_corner2_y = (end_y == middle_y) ? middle_y + 1 : Math.Max(end_y, middle_y) + 1;

            if (middle_end_corner1_x < 0 || middle_end_corner1_y < 0 || middle_end_corner2_x >= size || middle_end_corner2_y >= size) {
                continue; // next try
            }

            can_spawn_room = true;
            for (int x = middle_end_corner1_x; x <= middle_end_corner2_x; x++) {
                for (int y = middle_end_corner1_y; y <= middle_end_corner2_y; y++) {
                    if (ids[x][y] != 0) {
                        can_spawn_room = false;
                        x = middle_end_corner2_x + 1;
                        y = middle_end_corner2_y + 1;
                    }
                }
            }
            if (!can_spawn_room) {
                continue; // next try
            }

            // it is possible to spawn the room
            cache_cells_ammounts[current_id] = 0;
            int cache_cells_ammount = 100;
            cache_cells_x[current_id] = new int[cache_cells_ammount];
            cache_cells_y[current_id] = new int[cache_cells_ammount];

            for (int x = start_middle_corner1_x; x <= start_middle_corner2_x; x++) {
                for (int y = start_middle_corner1_y; y <= start_middle_corner2_y; y++) {
                    if (cells[x][y] == Cell.Wall) {
                        cells[x][y] = Cell.Passage;
                    }
                    ids[x][y] = current_id;
                    if (x != middle_x && y != middle_y) {
                        cells[x][y] = Cell.Hole;
                    }
                    // cache
                    cache_cells_x[current_id][cache_cells_ammounts[current_id]] = x;
                    cache_cells_y[current_id][cache_cells_ammounts[current_id]] = y;
                    cache_cells_ammounts[current_id]++;
                }
            }
            for (int x = middle_end_corner1_x; x <= middle_end_corner2_x; x++) {
                for (int y = middle_end_corner1_y; y <= middle_end_corner2_y; y++) {
                    if (cells[x][y] == Cell.Wall) {
                        cells[x][y] = Cell.Passage;
                    }
                    ids[x][y] = current_id;
                    if (x != middle_x && y != middle_y) {
                        cells[x][y] = Cell.Hole;
                    }
                    // cache
                    cache_cells_x[current_id][cache_cells_ammounts[current_id]] = x;
                    cache_cells_y[current_id][cache_cells_ammounts[current_id]] = y;
                    cache_cells_ammounts[current_id]++;
                }
            }

            TryToSpawnPossibleDoor(
                dx2 + end_x,
                dy2 + end_y,
                dx2 + end_x + dx2,
                dy2 + end_y + dy2,
                dir2
            );

            return true;

        }
        return false;
    }

    bool TryToSpawnPossibleDoor(int x1, int y1, int x2, int y2, int dir) {
        if (
            x1 < 0 || x1 >= size
            || y1 < 0 || y1 >= size
            || x2 < 0 || x2 >= size
            || y2 < 0 || y2 >= size
            ) {
            return false;
        }

        cells[x1][y1] = Cell.DoorEnterance;
        cells[x2][y2] = Cell.DoorExit;
        PossibleDoorsPushToTail(x2, y2, (dir));
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetRandomTurn(int dir) {
        int diff = RandomInclusive(0, 1) == 0 ? -1 : +1;
        return (dir + 4 + diff) % 4;
    }

    private bool TryToGenerateRoomSquare(int start_x, int start_y, int dir) {
        int tries = 5;
        while (tries > 0) {
            tries--;
            //Backup();

            int x_length = RandomInclusive(3, 5);
            int y_length = RandomInclusive(3, 5);

            int room_x_start;
            int room_y_start;
            if (dir == 0) { // up
                room_x_start = start_x - x_length / 2;
                room_y_start = start_y - y_length + 1;
            } else if (dir == 1) { // right
                room_x_start = start_x;
                room_y_start = start_y - y_length / 2;
            } else if (dir == 2) { // down
                room_x_start = start_x - x_length / 2;
                room_y_start = start_y;
            } else if (dir == 3) { // left
                room_x_start = start_x - x_length + 1;
                room_y_start = start_y - y_length / 2;
            } else {
                return false; // not reachable
            }

            if (room_x_start < 0 || room_y_start < 0 || room_x_start + x_length >= size || room_y_start + y_length >= size) {
                continue; // next try
            }

            bool check_if_possible_to_place = true;
            for (int x = room_x_start; x < room_x_start + x_length; x++) {
                for (int y = room_y_start; y < room_y_start + y_length; y++) {
                    if (ids[x][y] > 0) {
                        check_if_possible_to_place = false;
                        break;
                    }
                    //ids[x][y] = 999;
                    //types[x][y] = Cell.Hole;
                    //ShowInConsole();
                    //Console.ReadLine();
                }
                if (!check_if_possible_to_place) break;
            }
            if (!check_if_possible_to_place) continue; // next try

            // it is possible to spawn the room
            cache_cells_ammounts[current_id] = 0;
            cache_cells_x[current_id] = new int[x_length * y_length];
            cache_cells_y[current_id] = new int[x_length * y_length];
            for (int x = room_x_start; x < room_x_start + x_length; x++) {
                for (int y = room_y_start; y < room_y_start + y_length; y++) {
                    ids[x][y] = current_id;
                    if (cells[x][y] == Cell.Wall) {
                        cells[x][y] = Cell.Passage;
                    }
                    // cache
                    int cache_id = cache_cells_ammounts[current_id];
                    cache_cells_x[current_id][cache_id] = x;
                    cache_cells_y[current_id][cache_id] = y;
                    cache_cells_ammounts[current_id]++;
                }
            }

            TryToSpawnRandomDoorsInRoomByID(current_id);

            return true;
        }
        return false;
    }

    private bool TryToGenerateRoomCave(int start_x, int start_y, int forward_dir) {
        // this method will ALWAYS generate the room, maybe with only 1 cell
        int amount_of_maximum_desired_cells = RandomInclusive(20, 40);
        int amount_of_tries_left = amount_of_maximum_desired_cells * 3;
        int amount_of_cells_generated = 1;

        cells[start_x][start_y] = Cell.DoorExit;
        ids[start_x][start_y] = current_id;
        cache_cells_ammounts[current_id] = 0;
        cache_cells_x[current_id] = new int[amount_of_maximum_desired_cells];
        cache_cells_y[current_id] = new int[amount_of_maximum_desired_cells];
        cache_cells_x[current_id][cache_cells_ammounts[current_id]] = start_x;
        cache_cells_y[current_id][cache_cells_ammounts[current_id]] = start_y;
        cache_cells_ammounts[current_id]++;

        while (amount_of_cells_generated < amount_of_maximum_desired_cells && amount_of_tries_left > 0) {
            amount_of_tries_left--;

            GetRandomCellFromRoomByID(current_id, out int x, out int y);
            int dir = GetRandomDirectionExceptProvided(GetOppositeDirection(forward_dir));
            GetDirectionsVector(dir, out int dx, out int dy);
            x += dx;
            y += dy;

            if (x < 0 || y < 0 || x >= size || y >= size) {
                continue;
            }

            if (cells[x][y] == Cell.Wall) {
                // вошли в стену
                cells[x][y] = Cell.Passage;
                ids[x][y] = current_id;
                cache_cells_x[current_id][cache_cells_ammounts[current_id]] = x;
                cache_cells_y[current_id][cache_cells_ammounts[current_id]] = y;
                cache_cells_ammounts[current_id]++;
                amount_of_cells_generated++;
            }
        }
        TryToSpawnRandomDoorsInRoomByID(current_id);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetOppositeDirection(int dir) {
        return (dir + 4 - 2) % 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetDirectionsVector(int dir, out int dx, out int dy) {
        if (dir == 0) {
            dx = +0; dy = -1;
        } else if (dir == 1) {
            dx = +1; dy = +0;
        } else if (dir == 2) {
            dx = +0; dy = +1;
        } else if (dir == 3) {
            dx = -1; dy = +0;
        } else {
            dx = 0; dy = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetRandomDirectionExceptProvided(int dir) {
        int answer = RandomInclusive(0, 3);
        while (answer == dir) {
            answer = RandomInclusive(0, 3);
        }
        return answer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetAllCellsOfRoomByID(int room_id, out int[] cells_x, out int[] cells_y, out int cells_amount) {
        cells_x = cache_cells_x[room_id];
        cells_y = cache_cells_y[room_id];
        cells_amount = cache_cells_ammounts[room_id];
    }

    private void GetRandomCellFromRoomByID(int room_id, out int room_x, out int room_y) {
        GetAllCellsOfRoomByID(room_id, out int[] cells_x, out int[] cells_y, out int cells_amount);

        int random_index = RandomInclusive(0, cells_amount - 1);
        room_x = cells_x[random_index];
        room_y = cells_y[random_index];
    }

    private bool TryToGetRandomCellFromRoomByIDOnTheEdge(int room_id, out int door_x, out int door_y, out int door_d) {
        GetAllCellsOfRoomByID(room_id, out int[] cells_x, out int[] cells_y, out int cells_amount);
        if (cells_amount < 2) {
            door_x = -1;
            door_y = -1;
            door_d = -1;
            return false;
        }

        // create indexes of all cells
        int[] indexes = new int[cells_amount];
        for (int i = 0; i < cells_amount; i++) {
            indexes[i] = i;
        }
        // shuffle indexes
        int n = indexes.Length;
        while (n > 1) {
            n--;
            int k = RandomInclusive(0, n - 1);
            //(indexes[k], indexes[n]) = (indexes[n], indexes[k]);
            int temp = indexes[n];
            indexes[n] = indexes[k];
            indexes[k] = temp;
        }


        for (int i = 0; i < indexes.Length; i++) {
            int random_index = indexes[i];
            int x = cells_x[random_index];
            int y = cells_y[random_index];
            if (x < 1 || y < 1 || x >= size - 1 || y >= size - 1) {
                continue;
            }
            if (cells[x][y] == Cell.DoorExit) {
                continue;
            }
            int random_direction = RandomInclusive(0, 3);
            for (int fantom_direction = random_direction; fantom_direction < random_direction + 4; fantom_direction++) {
                int real_direction = fantom_direction % 4;
                GetDirectionsVector(real_direction, out int dx, out int dy);
                if (cells[x + dx][y + dy] == Cell.Wall) {
                    door_x = x;
                    door_y = y;
                    door_d = real_direction;
                    return true;
                }
            }
        }

        door_x = -1;
        door_y = -1;
        door_d = -1;
        return false;
    }
}

