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

public enum MazeType {
    Snail,
    Tree
}

[RequireComponent(typeof(UdonRandom))]
public class MazeGenerator : UdonSharpBehaviour {
    [SerializeField] UdonRandom udonRandom;

    public int Size => size;
    public int[][] Ids => ids;
    public Cell[][] Cells => cells;
    public Room[] Rooms => rooms;
    public int ChestsAmount => chests_amount;
    public int[] ChestsX => chests_x;
    public int[] ChestsY => chests_y;
    public int CurrentId => current_id;

    // ================================================================= //

    private int current_id = 0;

    private int size;
    private int max_rooms;
    private MazeType maze_type;

    private int[][] ids;
    private Cell[][] cells;
    private Room[] rooms;

    private int[][] ids_backup;
    private Cell[][] cells_backup;

    private int chests_amount;
    private int[] chests_x;
    private int[] chests_y;

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


    public void Init(int seed, int size, int rooms, int chests, MazeType maze_type) {
        this.seed = seed;
        udonRandom.SetSeed(seed);
        //rnd = new Random(seed);
        this.size = size;
        this.max_rooms = rooms;
        this.maze_type = maze_type;

        cache_cells_x = new int[rooms + 1][];
        cache_cells_y = new int[rooms + 1][];
        cache_cells_ammounts = new int[rooms + 1];

        possible_doors2_x = new int[rooms * 5];
        possible_doors2_y = new int[rooms * 5];
        possible_doors2_d = new int[rooms * 5]; // d - direction, 0 - up, 1 - right, 2 - down, 3 - left
        possible_doors2_head = 0;
        possible_doors2_tail = 0;

        this.rooms = new Room[rooms + chests];

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

        chests_amount = chests;
        chests_x = new int[chests];
        chests_y = new int[chests];

        GenerateFirstRoom();

        int halfSize = size / 2;

        if (maze_type == MazeType.Snail) {
            TryToSpawnPossibleDoor(halfSize + 0, halfSize - 2, 0);
            TryToSpawnPossibleDoor(halfSize + 2, halfSize + 0, 1);
            TryToSpawnPossibleDoor(halfSize + 0, halfSize + 2, 2);
            TryToSpawnPossibleDoor(halfSize - 2, halfSize + 0, 3);
        } else if (maze_type == MazeType.Tree) {
            TryToSpawnPossibleDoor(halfSize + 0, halfSize - 2, 0);
            tree_branch_iterator1 = 0;
            tree_branch_iterator2 = 0;
        }
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

    /** make central room 5 by 5 with no doors yet */
    public void GenerateFirstRoom() {
        current_id = 1;
        int halfSize = size / 2;
        for (int x = halfSize - 2; x <= halfSize + 2; x++) {
            for (int y = halfSize - 2; y <= halfSize + 2; y++) {
                ids[x][y] = 1;
                cells[x][y] = Cell.Passage;
            }
        }
        current_id++;
    }

    public bool Generate() {
        if (maze_type == MazeType.Snail) {
            return GenerateSnail();
        } else if (maze_type == MazeType.Tree) {
            return GenerateTree();
        }
        return true;
    }

    public bool GenerateSnail() {
        ReSeed();
        if (PossibleDoorsAmont() > 0 && current_id < max_rooms) {
            PossibleDoorsPopFromHead(out int x, out int y, out int d);
            if (TryToGenerateRoom(x, y, d)) {
                current_id++;
            }
            return false;
        }

        GenerateSnailFinish();
        return true;
    }

    public void GenerateSnailFinish() {
        // remove all possible doors
        RemoveAllUnusedDoors();

        // spawn final chests
        for (int i = 0; i < chests_amount; i++) {
            int room_id = current_id - 1 - i * 4;
            GetRandomCellFromRoomByID(room_id, out int room_x, out int room_y);
            chests_x[i] = room_x;
            chests_y[i] = room_y;
            if (cells[room_x][room_y] == Cell.Hole) {
                cells[room_x][room_y] = Cell.Passage;
            }
        }
    }

    public void RemoveAllUnusedDoors() {
        while (PossibleDoorsAmont() > 0) {
            PossibleDoorsPopFromHead(out int x, out int y, out int d);
            GetDirectionsVector(d, out int dx, out int dy);
            if (ids[x][y] == 0) {
                cells[x][y] = Cell.Wall;
                cells[x - dx][y - dy] = Cell.DoorDeadEnd;
            } else {
                cells[x][y] = Cell.Passage;
                cells[x - dx][y - dy] = Cell.Passage;
            }
        }
    }

    private int tree_branch_length1 = 10; // длинна ветки
    private int tree_branch_iterator1 = 0; // текущий итератор вдоль ветки (не может превышать длинну ветки)

    private int tree_branch_length2 = 4; // длинна микро-ветки
    private int tree_branch_iterator2 = 0; // текущий итератор вдоль микро-ветки (не может превышать длинну ветки)

    public bool GenerateTree() {
        ReSeed();

        if (current_id < max_rooms) {

            if (PossibleDoorsAmont() == 0) {
                // в прошлых циклах дверь не заспавнилась, ищем новую
                int possible_room_id = current_id - 1;
                while (TryToSpawnRandomDoorsInRoomByID(possible_room_id, 1) == 0) {
                    possible_room_id--;
                    // подушка безопасности
                    if (possible_room_id <= 1) {
                        GenerateTreeFinish();
                        return true;
                    }
                }
            }

            PossibleDoorsPopFromHead(out int x, out int y, out int d);

            if (TryToGenerateRoomForTree(x, y, d)) {
                tree_branch_iterator1++;
                tree_branch_iterator2++;
                if (tree_branch_iterator1 >= tree_branch_length1) {
                    TryToSpawnRandomDoorsInRoomByID(current_id - 1 - (tree_branch_length1 / 2), 1);
                    tree_branch_iterator1 = 0;
                } else if (tree_branch_iterator2 >= tree_branch_length2) {
                    TryToSpawnRandomDoorsInRoomByID(current_id - 1 - (tree_branch_length2 / 2), 1);
                    tree_branch_iterator2 = 0;
                } else {
                    TryToSpawnRandomDoorsInRoomByID(current_id - 1, 1);
                }
                return false;
            }
            // ничего не удалось сгенерировать? ничего страшного, в следующем цикле извлечется следующая дверь (наверное)
            return false;
        }

        GenerateTreeFinish();
        return true;
    }

    bool TryToGenerateRoomForTree(int x, int y, int d) {
        if (TryToGenerateRoomSquare(x, y, d)) {
            rooms[current_id] = Room.Square;
            current_id++;
            return true;
        }

        if (TryToGenerateRoomCave(x, y, d)) {
            rooms[current_id] = Room.Cave;
            current_id++;
            return true;
        }
        return false;
    }

    public void GenerateTreeFinish() {
        RemoveAllUnusedDoors();

        // spawn final chests
        for (int i = 0; i < chests_amount; i++) {
            int room_id = current_id - 1 - i;
            GetRandomCellFromRoomByID(room_id, out int room_x, out int room_y);
            chests_x[i] = room_x;
            chests_y[i] = room_y;
            if (cells[room_x][room_y] == Cell.Hole) {
                cells[room_x][room_y] = Cell.Passage;
            }
        }
    }



    private int TryToSpawnRandomDoorsInRoomByID(int room_id, int amount = 0) {
        int amount_of_doors = amount;
        if (amount_of_doors == 0) {
            int min_amount_of_doors = current_id <= 5 ? 3 : 2;
            amount_of_doors = RandomInclusive(min_amount_of_doors, 4);
        }
        int amount_of_doors_spawned = 0;
        for (int i = 0; i < amount_of_doors; i++) {
            bool result = TryToGetRandomCellFromRoomByIDOnTheEdge(room_id, out int door_x, out int door_y, out int door_d);
            if (!result) break;
            if (TryToSpawnPossibleDoor(door_x, door_y, door_d)) {
                amount_of_doors_spawned++;
            }
        }
        return amount_of_doors_spawned;
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
                TryToSpawnRandomDoorsInRoomByID(current_id);
                return true;
            }
        } else {
            // 80% chance
            if (TryToGenerateRoomSquare(start_x, start_y, forward_direction)) {
                rooms[current_id] = Room.Square;
                TryToSpawnRandomDoorsInRoomByID(current_id);
                return true;
            }
        }

        if (TryToGenerateRoomCave(start_x, start_y, forward_direction)) {
            rooms[current_id] = Room.Cave;
            TryToSpawnRandomDoorsInRoomByID(current_id);
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

            /*
            TryToSpawnPossibleDoor(
                dx2 + end_x,
                dy2 + end_y,
                dx2 + end_x + dx2,
                dy2 + end_y + dy2,
                dir2
            );
            */

            return true;

        }
        return false;
    }

    /**
     * Пытается заспавнить дверь:
     * в x1 y1 = вход
     * в x2 y2 = выход + запись в стак будущих дверей
     */
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

    /**
     * Пытается заспавнить дверь:
     * в указанных координатах = вход
     * в указанных координатах + смещение вдоль направления = выход + запись в стак будущих дверей
     */
    bool TryToSpawnPossibleDoor(int x, int y, int dir) {
        GetDirectionsVector(dir, out int dx, out int dy);
        return TryToSpawnPossibleDoor(x, y, x + dx, y + dy, dir);
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
            if (cells[x][y] == Cell.DoorExit || cells[x][y] == Cell.DoorEnterance) {
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