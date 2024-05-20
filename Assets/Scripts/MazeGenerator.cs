using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;

public enum Cell {
    Wall,
    Passage,
    Hole,
    Treasure,
    DoorEnterance,
    DoorExit,
    DoorDeadEnd,
    Stairs0, // 0 - up
    Stairs1, // 1 - right
    Stairs2, // 2 - down
    Stairs3, // 3 - left
}

public enum Room {
    Square,
    Cave,
    Stairs,
}

[RequireComponent(typeof(UdonRandom))]
public class MazeGenerator : UdonSharpBehaviour {
    [SerializeField] UdonRandom udonRandom;
    
    public int Size => size;
    public int Height => height;
    public int RoomsAmount => max_rooms;
    public int[][][] Ids => ids;
    public Cell[][][] Cells => cells;
    public Room[] Rooms => rooms;
    public int ChestsAmount => chests_amount;
    public int[] ChestsX => chests_x;
    public int[] ChestsY => chests_y;
    public int[] ChestsZ => chests_z;
    public int CurrentId => current_id;
    
    // ================================================================= //
    
    private int current_id = 0;
    
    private int size;
    private int height = 5;
    public int middle_floor_index = 2; // 01234
    
    private int max_rooms;
    
    private int[][][] ids;
    private Cell[][][] cells;
    private Room[] rooms;
    
    private int chests_amount;
    private int[] chests_x;
    private int[] chests_y;
    private int[] chests_z;
    
    // ----------- PossibleDoors Stack
    private int[] possible_doors2_x;
    private int[] possible_doors2_y;
    private int[] possible_doors2_z;
    private int[] possible_doors2_d; // d - direction, 0 - up, 1 - right, 2 - down, 3 - left
    private int possible_doors2_head = 0;
    private int possible_doors2_tail = 0;
    
    private int[][] cache_cells_x;
    private int[][] cache_cells_y;
    private int[][] cache_cells_z;
    private int[] cache_cells_ammounts;
    
    public int GetId(int x, int y, int z) {
        if (x >= 0 && y >= 0 && x < size && y < size && z >= 0 && z < size) {
            return ids[x][y][z];
        } else {
            return 0;
        }
    }
    
    public Cell GetCell(int x, int y, int z) {
        if (x >= 0 && y >= 0 && x < size && y < size && z >= 0 && z < size) {
            return cells[x][y][z];
        } else {
            return Cell.Wall;
        }
    }
    
    public void PossibleDoorsPushToTail(int x, int y, int z, int forward_direction) {
        possible_doors2_x[possible_doors2_tail] = x;
        possible_doors2_y[possible_doors2_tail] = y;
        possible_doors2_z[possible_doors2_tail] = z;
        possible_doors2_d[possible_doors2_tail] = forward_direction;
        possible_doors2_tail++;
    }
    
    public void PossibleDoorsPopFromHead(out int x, out int y, out int z, out int forward_direction) {
        x = possible_doors2_x[possible_doors2_head];
        y = possible_doors2_y[possible_doors2_head];
        z = possible_doors2_z[possible_doors2_head];
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
    
    
    public void Init(int seed, int size, int rooms, int chests) {
        this.seed = seed;
        udonRandom.SetSeed(seed);
        //rnd = new Random(seed);
        this.size = size;
        this.max_rooms = rooms;
        
        cache_cells_x = new int[rooms + 1][];
        cache_cells_y = new int[rooms + 1][];
        cache_cells_z = new int[rooms + 1][];
        cache_cells_ammounts = new int[rooms + 1];
        
        possible_doors2_x = new int[rooms * 5];
        possible_doors2_y = new int[rooms * 5];
        possible_doors2_z = new int[rooms * 5];
        possible_doors2_d = new int[rooms * 5]; // d - direction, 0 - up, 1 - right, 2 - down, 3 - left
        possible_doors2_head = 0;
        possible_doors2_tail = 0;
        
        this.rooms = new Room[rooms + chests];
        
        ids = new int[size][][];
        for (int i = 0; i < size; i++) {
            ids[i] = new int[size][];
            for (int j = 0; j < size; j++) {
                ids[i][j] = new int[height];
            }
        }
        
        cells = new Cell[size][][];
        for (int i = 0; i < size; i++) {
            cells[i] = new Cell[size][];
            for (int j = 0; j < size; j++) {
                cells[i][j] = new Cell[height];
            }
        }
        
        chests_amount = chests;
        chests_x = new int[chests];
        chests_y = new int[chests];
        chests_z = new int[chests];
        
        GenerateFirstRoom();
        
        int halfSize = size / 2;
        
        TryToSpawnPossibleDoor(halfSize + 0, halfSize - 2, middle_floor_index, 0);
        tree_branch_iterator1 = 0;
        tree_branch_iterator2 = 0;
    }
    
    /** make central room 5 by 5 with no doors yet */
    public void GenerateFirstRoom() {
        current_id = 1;
        int halfSize = size / 2;
        for (int x = halfSize - 2; x <= halfSize + 2; x++) {
            for (int y = halfSize - 2; y <= halfSize + 2; y++) {
                ids[x][y][middle_floor_index] = 1;
                cells[x][y][middle_floor_index] = Cell.Passage;
                ids[x][y][middle_floor_index + 1] = 1;
                cells[x][y][middle_floor_index + 1] = Cell.Passage;
                ids[x][y][middle_floor_index + 2] = 1;
                cells[x][y][middle_floor_index + 2] = Cell.Passage;
            }
        }
        current_id++;
    }
    
    public bool Generate() {
        return GenerateTree();
    }
    
    public void RemoveAllUnusedDoors() {
        while (PossibleDoorsAmont() > 0) {
            PossibleDoorsPopFromHead(out int x, out int y, out int z, out int d);
            GetDirectionsVector(d, out int dx, out int dy);
            if (ids[x][y][z] == 0) {
                cells[x][y][z] = Cell.Wall;
                cells[x - dx][y - dy][z] = Cell.DoorDeadEnd;
            } else {
                cells[x][y][z] = Cell.Passage;
                cells[x - dx][y - dy][z] = Cell.Passage;
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
            
            PossibleDoorsPopFromHead(out int x, out int y, out int z, out int d);
            
            if (TryToGenerateRoomForTree(x, y, z, d)) {
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
    
    bool TryToGenerateRoomForTree(int x, int y, int z, int d) {
        int random_index = RandomInclusive(0, 3);
        
        if (random_index == 0) {
            if (TryToGenerateRoomStairs(x, y, z, d)) {
                rooms[current_id] = Room.Stairs;
                current_id++;
                // return true; // lets just spawn next room immideatly
                PossibleDoorsPopFromHead(out x, out y, out z, out d);
            }
        }
        
        if (TryToGenerateRoomSquare(x, y, z, d)) {
            rooms[current_id] = Room.Square;
            current_id++;
            return true;
        }
        
        if (TryToGenerateRoomCave(x, y, z, d)) {
            rooms[current_id] = Room.Cave;
            current_id++;
            return true;
        }
        return false;
    }
    
    public void GenerateTreeFinish() {
        RemoveAllUnusedDoors();
        
        // spawn final chests
        int treasures_left = chests_amount;
        int room_id = current_id;
        int i = 0;
        while (treasures_left > 0) {
            room_id--;
            if (room_id <= 1) break;
            if (rooms[room_id] == Room.Cave || rooms[room_id] == Room.Square) {
                GetRandomCellFromRoomByID(room_id, out int x, out int y, out int z);
                chests_x[i] = x;
                chests_y[i] = y;
                chests_z[i] = z;
                if (cells[x][y][z] != Cell.DoorEnterance && cells[x][y][z] != Cell.DoorExit) {
                    cells[x][y][z] = Cell.Treasure;
                }
                i++;
                treasures_left--;
            }
        }
    }
    
    private int TryToSpawnRandomDoorsInRoomByID(int room_id, int amount) {
        int amount_of_doors_spawned = 0;
        for (int i = 0; i < amount; i++) {
            bool result = TryToGetRandomCellFromRoomByIDOnTheEdge(room_id, out int door_x, out int door_y, out int door_z, out int door_d);
            if (!result) break;
            if (TryToSpawnPossibleDoor(door_x, door_y, door_z, door_d)) {
                amount_of_doors_spawned++;
            }
        }
        return amount_of_doors_spawned;
    }
    
    /**
     * Пытается заспавнить дверь:
     * в x1 y1 z = вход
     * в x2 y2 z = выход + запись в стак будущих дверей
     */
    bool TryToSpawnPossibleDoor(int x1, int y1, int x2, int y2, int z, int dir) {
        if (
            x1 < 0 || x1 >= size
                   || y1 < 0 || y1 >= size
                   || x2 < 0 || x2 >= size
                   || y2 < 0 || y2 >= size
                   || z < 0 || z >= height
        ) {
            return false;
        }
        
        cells[x1][y1][z] = Cell.DoorEnterance;
        cells[x2][y2][z] = Cell.DoorExit;
        PossibleDoorsPushToTail(x2, y2, z, (dir));
        return true;
    }
    
    /**
     * Пытается заспавнить дверь:
     * в указанных координатах = вход
     * в указанных координатах + смещение вдоль направления = выход + запись в стак будущих дверей
     */
    bool TryToSpawnPossibleDoor(int x, int y, int z, int dir) {
        GetDirectionsVector(dir, out int dx, out int dy);
        return TryToSpawnPossibleDoor(x, y, x + dx, y + dy, z, dir);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetRandomTurn(int dir) {
        int diff = RandomInclusive(0, 1) == 0 ? -1 : +1;
        return (dir + 4 + diff) % 4;
    }
    
    private bool TryToGenerateRoomSquare(int start_x, int start_y, int z, int dir) {
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
                    if (ids[x][y][z] > 0) {
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
            cache_cells_z[current_id] = new int[x_length * y_length];
            for (int x = room_x_start; x < room_x_start + x_length; x++) {
                for (int y = room_y_start; y < room_y_start + y_length; y++) {
                    ids[x][y][z] = current_id;
                    if (cells[x][y][z] == Cell.Wall) {
                        cells[x][y][z] = Cell.Passage;
                    }
                    // cache
                    int cache_id = cache_cells_ammounts[current_id];
                    cache_cells_x[current_id][cache_id] = x;
                    cache_cells_y[current_id][cache_id] = y;
                    cache_cells_z[current_id][cache_id] = z;
                    cache_cells_ammounts[current_id]++;
                }
            }
            
            
            return true;
        }
        return false;
    }
    
    private bool TryToGenerateRoomCave(int start_x, int start_y, int start_z, int forward_dir) {
        // this method will ALWAYS generate the room, maybe with only 1 cell
        int amount_of_maximum_desired_cells = RandomInclusive(20, 40);
        int amount_of_tries_left = amount_of_maximum_desired_cells * 3;
        int amount_of_cells_generated = 1;
        
        cells[start_x][start_y][start_z] = Cell.DoorExit;
        ids[start_x][start_y][start_z] = current_id;
        cache_cells_ammounts[current_id] = 0;
        cache_cells_x[current_id] = new int[amount_of_maximum_desired_cells];
        cache_cells_y[current_id] = new int[amount_of_maximum_desired_cells];
        cache_cells_z[current_id] = new int[amount_of_maximum_desired_cells];
        cache_cells_x[current_id][cache_cells_ammounts[current_id]] = start_x;
        cache_cells_y[current_id][cache_cells_ammounts[current_id]] = start_y;
        cache_cells_z[current_id][cache_cells_ammounts[current_id]] = start_z;
        cache_cells_ammounts[current_id]++;
        
        while (amount_of_cells_generated < amount_of_maximum_desired_cells && amount_of_tries_left > 0) {
            amount_of_tries_left--;
            
            GetRandomCellFromRoomByID(current_id, out int x, out int y, out int z);
            int dir = GetRandomDirectionExceptProvided(GetOppositeDirection(forward_dir));
            GetDirectionsVector(dir, out int dx, out int dy);
            x += dx;
            y += dy;
            
            if (x < 0 || y < 0 || z < 0 || x >= size || y >= size || z >= height) {
                continue;
            }
            
            if (cells[x][y][z] == Cell.Wall) {
                // вошли в стену
                cells[x][y][z] = Cell.Passage;
                ids[x][y][z] = current_id;
                cache_cells_x[current_id][cache_cells_ammounts[current_id]] = x;
                cache_cells_y[current_id][cache_cells_ammounts[current_id]] = y;
                cache_cells_z[current_id][cache_cells_ammounts[current_id]] = z;
                cache_cells_ammounts[current_id]++;
                amount_of_cells_generated++;
            }
        }
        
        return true;
    }
    
    // генерирует комнату 2*1*2 с лестницей в случайную сторону: вверх или вниз
    private bool TryToGenerateRoomStairs(int start_x, int start_y, int start_z, int forward_dir) {
        if (start_x < 3 || start_x > size - 4 || start_y < 3 || start_y > size - 4) {
            return false;
        }
        
        if (ids[start_x][start_y][start_z] != 0) return false;
        int dz;
        if (start_z == 0) dz = 1;
        else if (start_z == height - 1) dz = -1;
        else dz = RandomInclusive(0, 1) * 2 - 1;
        
        if (ids[start_x][start_y][start_z + dz] != 0) return false;
        GetDirectionsVector(forward_dir, out int dx, out int dy);
        if (GetId(start_x + dx, start_y + dy, start_z) != 0) return false;
        if (GetId(start_x + dx, start_y + dy, start_z + dz) != 0) return false;
        if (GetId(start_x + dx + dx, start_y + dy + dy, start_z + dz) != 0) return false;
        
        // it is possible to spawn vertical 2x2 room
        
        ids[start_x][start_y][start_z] = current_id;
        ids[start_x][start_y][start_z + dz] = current_id;
        ids[start_x + dx][start_y + dy][start_z] = current_id;
        ids[start_x + dx][start_y + dy][start_z + dz] = current_id;
        
        cells[start_x][start_y][start_z + dz] = Cell.Passage;
        cells[start_x + dx][start_y + dy][start_z] = Cell.Passage;
        
        int stairs_dir = dz > 0 ? forward_dir : GetOppositeDirection(forward_dir);
        Cell stairs_cell = Cell.Stairs0;
        if (stairs_dir == 0) stairs_cell = Cell.Stairs0;
        if (stairs_dir == 1) stairs_cell = Cell.Stairs1;
        if (stairs_dir == 2) stairs_cell = Cell.Stairs2;
        if (stairs_dir == 3) stairs_cell = Cell.Stairs3;
        if (dz > 0) {
            // up: stairs are on original level (no +dz) with offset (+dx +dy)
            cells[start_x + dx][start_y + dy][start_z] = stairs_cell;
        } else {
            // down: stairs are on different level (+dz) with no offset (no +dx +dy)
            cells[start_x][start_y][start_z + dz] = stairs_cell;
        }
        
        // TODO fill with proper data
        cache_cells_ammounts[current_id] = 0;
        cache_cells_x[current_id] = new int[0];
        cache_cells_y[current_id] = new int[0];
        cache_cells_z[current_id] = new int[0];
        
        TryToSpawnPossibleDoor(start_x + dx, start_y + dy, start_z + dz, forward_dir);
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetOppositeDirection(int dir) {
        return (dir + 4 - 2) % 4;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetDirectionsVector(int dir, out int dx, out int dy) {
        if (dir == 0) {
            dx = +0;
            dy = -1;
        } else if (dir == 1) {
            dx = +1;
            dy = +0;
        } else if (dir == 2) {
            dx = +0;
            dy = +1;
        } else if (dir == 3) {
            dx = -1;
            dy = +0;
        } else {
            dx = 0;
            dy = 0;
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
    private void GetAllCellsOfRoomByID(int room_id, out int[] cells_x, out int[] cells_y, out int[] cells_z, out int cells_amount) {
        cells_x = cache_cells_x[room_id];
        cells_y = cache_cells_y[room_id];
        cells_z = cache_cells_z[room_id];
        cells_amount = cache_cells_ammounts[room_id];
    }
    
    private void GetRandomCellFromRoomByID(int room_id, out int room_x, out int room_y, out int room_z) {
        GetAllCellsOfRoomByID(room_id, out int[] cells_x, out int[] cells_y, out int[] cells_z, out int cells_amount);
        
        int random_index = RandomInclusive(0, cells_amount - 1);
        room_x = cells_x[random_index];
        room_y = cells_y[random_index];
        room_z = cells_z[random_index];
    }
    
    private bool TryToGetRandomCellFromRoomByIDOnTheEdge(int room_id, out int door_x, out int door_y, out int door_z, out int door_d) {
        GetAllCellsOfRoomByID(room_id, out int[] cells_x, out int[] cells_y, out int[] cells_z, out int cells_amount);
        if (cells_amount < 2) {
            door_x = -1;
            door_y = -1;
            door_z = -1;
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
            int z = cells_z[random_index];
            if (x < 1 || y < 1 || x >= size - 1 || y >= size - 1) {
                continue;
            }
            if (cells[x][y][z] == Cell.DoorExit || cells[x][y][z] == Cell.DoorEnterance) {
                continue;
            }
            int random_direction = RandomInclusive(0, 3);
            for (int fantom_direction = random_direction; fantom_direction < random_direction + 4; fantom_direction++) {
                int real_direction = fantom_direction % 4;
                GetDirectionsVector(real_direction, out int dx, out int dy);
                if (cells[x + dx][y + dy][z] == Cell.Wall) {
                    door_x = x;
                    door_y = y;
                    door_z = z;
                    door_d = real_direction;
                    return true;
                }
            }
        }
        
        door_x = -1;
        door_y = -1;
        door_z = -1;
        door_d = -1;
        return false;
    }
}