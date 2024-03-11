using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public enum Cell {
    Wall,
    Passage,
    Hole,
    Something,
    DoorEnterance,
    DoorExit
}

public class MazeV2 : UdonSharpBehaviour {

    public Cell[][] GetCells => types;
    public int[][] GetIds => ids;
    public int Size => size;

    private int size = 19;
    private int max_rooms;

    private int[][] ids;
    private Cell[][] types;

    private int[][] ids2;
    private Cell[][] types2;

    // ----------- PossibleDoors Stack
    private int[] possible_doors2_x = new int[10000];
    private int[] possible_doors2_y = new int[10000];
    private int[] possible_doors2_d = new int[10000]; // d - direction, 0 - up, 1 - right, 2 - down, 3 - left
    private int possible_doors2_head = 0;
    private int possible_doors2_tail = 0;

    private int[][] cache_cells_x;
    private int[][] cache_cells_y;
    private int[] cache_cells_ammounts;

    public void PossibleDoorsPushToTail(int x, int y, int d) {
        possible_doors2_x[possible_doors2_tail] = x;
        possible_doors2_y[possible_doors2_tail] = y;
        possible_doors2_d[possible_doors2_tail] = d;
        possible_doors2_tail++;
    }

    public void PossibleDoorsPopFromHead(out int x, out int y, out int d) {
        x = possible_doors2_x[possible_doors2_head];
        y = possible_doors2_y[possible_doors2_head];
        d = possible_doors2_d[possible_doors2_head];
        possible_doors2_head++;
    }

    public int PossibleDoorsAmont() {
        return possible_doors2_tail - possible_doors2_head;
    }
    // ----------- PossibleDoors Stack


    public int RandomSign() {
        return (RandomInclusive(0,1) % 2 == 0) ? +1 : -1;
    }

    public int RandomInclusive(int min_inclusive, int max_inclusive) {
        //return rnd.Next(min_inclusive, max_inclusive + 1);
        return UnityEngine.Random.Range(min_inclusive, max_inclusive + 1);
    }

    private int seed;
    public void ReSeed() {
        UnityEngine.Random.InitState(seed);
        seed = RandomInclusive(100000, 999999);
        UnityEngine.Random.InitState(seed);
    }


    public void Init(int max_rooms, int seed) {
        this.seed = seed;
        UnityEngine.Random.InitState(seed);
        this.max_rooms = max_rooms;

        cache_cells_x = new int[max_rooms + 1][];
        cache_cells_y = new int[max_rooms + 1][];
        cache_cells_ammounts = new int[max_rooms + 1];

        possible_doors2_x = new int[10000];
        possible_doors2_y = new int[10000];
        possible_doors2_d = new int[10000]; // d - direction, 0 - up, 1 - right, 2 - down, 3 - left
        possible_doors2_head = 0;
        possible_doors2_tail = 0;

        ids = new int[size][];
        for (int i = 0; i < size; i++) ids[i] = new int[size];
        types = new Cell[size][];
        for (int i = 0; i < size; i++) types[i] = new Cell[size];

        ids2 = new int[size][];
        for (int i = 0; i < size; i++) ids2[i] = new int[size];
        types2 = new Cell[size][];
        for (int i = 0; i < size; i++) types2[i] = new Cell[size];

        GenerateFirstRoom();
    }


    public void Backup() {
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                ids2[x][y] = ids[x][y];
                types2[x][y] = types[x][y];
            }
        }
    }

    public void Restore() {
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                ids[x][y] = ids2[x][y];
                types[x][y] = types2[x][y];
            }
        }
    }

    public void GenerateFirstRoom() {
        // make central room 5 by 5 with 4 doors
        current_id = 1;
        for (int x = size / 2 - 2; x <= size / 2 + 2; x++) {
            for (int y = size / 2 - 2; y <= size / 2 + 2; y++) {
                ids[x][y] = 1;
                types[x][y] = Cell.Passage;
            }
        }
        types[size / 2 - 2][size / 2] = Cell.DoorEnterance;
        types[size / 2 + 2][size / 2] = Cell.DoorEnterance;
        types[size / 2][size / 2 - 2] = Cell.DoorEnterance;
        types[size / 2][size / 2 + 2] = Cell.DoorEnterance;

        types[size / 2 - 3][size / 2] = Cell.DoorExit;
        types[size / 2 + 3][size / 2] = Cell.DoorExit;
        types[size / 2][size / 2 - 3] = Cell.DoorExit;
        types[size / 2][size / 2 + 3] = Cell.DoorExit;

        PossibleDoorsPushToTail(size / 2 + 0, size / 2 - 3, 2);
        PossibleDoorsPushToTail(size / 2 + 3, size / 2 + 0, 3);
        PossibleDoorsPushToTail(size / 2 + 0, size / 2 + 3, 0);
        PossibleDoorsPushToTail(size / 2 - 3, size / 2 + 0, 1);

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
        }
        return current_id >= max_rooms || PossibleDoorsAmont() <= 0;
    }

    public int current_id = 0;

    private void TryToSpawnRandomDoorsInRoomByID(int room_id) {
        int amount_of_doors = RandomInclusive(1, 3);
        for (int i = 0; i < amount_of_doors; i++) {
            TryToGetRandomCellFromRoomByIDOnTheEdge(room_id, out int door_x, out int door_y, out int door_d);
            if (door_x == -1) break;
            GetDirectionsVector(door_d, out int dx, out int dy);

            types[door_x][door_y] = Cell.DoorEnterance;
            types[door_x + dx][door_y + dy] = Cell.DoorExit;
            PossibleDoorsPushToTail(door_x + dx, door_y + dy, GetOppositeDirection(door_d));
        }
    }

    private bool TryToGenerateRoom(int start_x, int start_y, int except_dir) {
        if (types[start_x][start_y] != Cell.DoorExit || ids[start_x][start_y] != 0) {
            return false;
        }

        if (TryToGenerateRoomSquare(start_x, start_y, except_dir)) return true;
        if (TryToGenerateRoomCave(start_x, start_y, except_dir)) return true;
        return false;
    }

    private bool TryToGenerateRoomSquare(int start_x, int start_y, int except_dir) {
        int tries = 5;
        while (tries > 0) {
            tries--;
            Backup();

            int x_length = RandomInclusive(3, 5);
            int y_length = RandomInclusive(3, 5);
            int dir = GetOppositeDirection(except_dir);

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
            cache_cells_x[current_id] = new int[100];
            cache_cells_y[current_id] = new int[100];
            for (int x = room_x_start; x < room_x_start + x_length; x++) {
                for (int y = room_y_start; y < room_y_start + y_length; y++) {
                    ids[x][y] = current_id;
                    if (types[x][y] == Cell.Wall) {
                        types[x][y] = Cell.Passage;
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

    private bool TryToGenerateRoomCave(int start_x, int start_y, int except_dir) {

        int amount_of_tries_left_to_generate_room = 0;
        int x = start_x;
        int y = start_y;
        bool result_of_room_generation = false;
        int amount_of_desired_cells = RandomInclusive(20, 40);

        cache_cells_x[current_id] = new int[amount_of_desired_cells + 1];
        cache_cells_y[current_id] = new int[amount_of_desired_cells + 1];
        cache_cells_ammounts[current_id] = 0;

        while (amount_of_tries_left_to_generate_room < amount_of_desired_cells) {
            amount_of_tries_left_to_generate_room++;
            cache_cells_ammounts[current_id] = 0;
            Backup();

            types[start_x][start_y] = Cell.DoorExit;
            ids[start_x][start_y] = current_id;
            cache_cells_x[current_id][0] = start_x;
            cache_cells_y[current_id][0] = start_y;
            cache_cells_ammounts[current_id]++;

            int amount_of_cells = amount_of_desired_cells - amount_of_tries_left_to_generate_room;
            x = start_x;
            y = start_y;

            int fails = 0;

            for (int i = 1; i < amount_of_cells; i++) {

                GetRandomCellFromRoomByID(current_id, out x, out y);
                int dir = GetRandomDirectionExceptProvided(except_dir);
                GetDirectionsVector(dir, out int dx, out int dy);
                x += dx;
                y += dy;

                if (x < 0 || y < 0 || x >= size || y >= size) {
                    i--;
                    fails++;
                    if (fails > 10) break;
                    continue;
                }

                if (types[x][y] == Cell.Wall) {
                    // вошли в стену
                    types[x][y] = Cell.Passage;
                    ids[x][y] = current_id;
                    fails = 0;

                    int cache_id = cache_cells_ammounts[current_id];
                    cache_cells_x[current_id][cache_id] = x;
                    cache_cells_y[current_id][cache_id] = y;
                    cache_cells_ammounts[current_id]++;

                } else if (ids[x][y] == current_id) {
                    // вошли в текущую комнату
                    i--; fails++; if (fails > 10) break;

                } else {
                    // вошли в чужую комнату
                    i--; fails++; if (fails > 10) break;
                }

            }

            //ShowInConsole();

            if (fails == 0) {
                // генерация комнаты успешна
                result_of_room_generation = true;
                Backup();
                break;
            } else {
                // генерация комнаты провалилась
                Restore();
                continue;
            }
        }

        if (!result_of_room_generation) {
            return false;
        }

        TryToSpawnRandomDoorsInRoomByID(current_id);

        Backup();
        return true;
    }

    private int GetOppositeDirection(int dir) {
        return (dir + 4 - 2) % 4;
    }

    private void GetDirectionsVector(int dir, out int dx, out int dy) {
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

    private int GetRandomDirectionExceptProvided(int dir) {
        int answer = RandomInclusive(0, 3);
        while (answer == dir) {
            answer = RandomInclusive(0, 3);
        }
        return answer;
    }

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

    private bool TryToGetRandomCellFromRoomByIDOnTheEdge(int room_id, out int room_x, out int room_y, out int room_d) {
        GetAllCellsOfRoomByID(room_id, out int[] cells_x, out int[] cells_y, out int cells_amount);

        int counts = 100;
        while (counts > 0) {
            counts--;
            int random_index = RandomInclusive(0, cells_amount - 1);
            int x = cells_x[random_index];
            int y = cells_y[random_index];
            if (x < 1 || y < 1 || x >= size - 1 || y >= size - 1) {
                continue;
            }
            if (types[x][y] == Cell.DoorExit) {
                continue;
            }
            int random_direction = RandomInclusive(0, 3);
            for (int fantom_direction = random_direction; fantom_direction < random_direction + 4; fantom_direction++) {
                int real_direction = fantom_direction % 4;
                GetDirectionsVector(real_direction, out int dx, out int dy);
                if (types[x + dx][y + dy] == Cell.Wall) {
                    room_x = x;
                    room_y = y;
                    room_d = real_direction;
                    return true;
                }
            }
        }

        room_x = -1;
        room_y = -1;
        room_d = -1;
        return false;
    }
}
