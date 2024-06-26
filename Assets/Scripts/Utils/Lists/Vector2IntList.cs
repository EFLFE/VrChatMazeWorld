﻿using System;
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Vector2IntList : UdonSharpBehaviour {
    public int Count => _count;

    private Vector2Int[] _storage;
    private int _count;

    public Vector2Int[] Items => _storage;

    public void Init(int initialCapacity) {
        _storage = new Vector2Int[initialCapacity];
    }

    public void EnsureCapacity(int targetCapacity) {
        int diff = targetCapacity - _storage.Length;
        if (0 >= diff) return;
        ExpandCapacity(diff);
    }

    private void ExpandCapacity(int extraCapacity) {
        if (ListManager.MIN_CAPACITY_EXPAND_SIZE > extraCapacity) {
            extraCapacity = ListManager.MIN_CAPACITY_EXPAND_SIZE;
        } else if ((_storage.Length / 8) > extraCapacity) {
            extraCapacity = _storage.Length / 8;
        }
        Vector2Int[] newBuffer = new Vector2Int[_storage.Length + extraCapacity];
        Array.Copy(_storage, newBuffer, _count);
        _storage = newBuffer;
    }

    /// <summary>
    /// Try get item or default.
    /// </summary>
    public Vector2Int this[int index] {
        get {
            if (index >= _count || 0 > index) {
                return Vector2Int.zero;
            }
            return _storage[index];
        }
    }

    public void Add(Vector2Int item) {
        if (_count == _storage.Length) {
            ExpandCapacity(1);
        }
        _storage[_count] = item;
        _count++;
    }

    public void Add(Vector2Int[] items) {
        if (items == null) return;
        int newSize = _count + items.Length;
        if (newSize > _storage.Length) {
            ExpandCapacity(items.Length);
        }
        Array.Copy(items, 0, _storage, _count, items.Length);
        _count += items.Length;
    }

    /// <returns> True if item is successfully removed; otherwise, false. </returns>
    public void RemoveLast() {
        if (_count == 0) return;
        _count--;
        _storage[_count] = default;
    }

    public void Swap(int firstIndex, int secondIndex) {
        if (firstIndex == secondIndex || firstIndex < 0 || secondIndex < 0 || firstIndex >= _count || secondIndex >= _count)
            return;

        Vector2Int firstItem = _storage[firstIndex];
        _storage[firstIndex] = _storage[secondIndex];
        _storage[secondIndex] = firstItem;
    }

    /// <summary> Swaps element awaiting for removal with last element, and then removes newly swapped last element </summary>
    public void RemoveAt(int index) {
        if (0 > index || index >= _count) {
            return;
        }
        _count--;
        _storage[index] = _storage[_count];
        _storage[_count] = default;
    }

    /// <param name="shrinkBuffer"> Decrease internal storage size after clearing the list? </param>
    public void Clear(bool shrinkBuffer = false) {
        Array.Clear(_storage, 0, _count);
        _count = 0;
        if (shrinkBuffer) {
            _storage = new Vector2Int[ListManager.MIN_CAPACITY_EXPAND_SIZE];
        }
    }

    /// <returns> True if element is present in the list, otherwise false </returns>
    public bool Contains(Vector2Int item) {
        for (int index = 0; _count > index; index++) {
            if (_storage[index] == item) return true;
        }
        return false;
    }

    /// <returns> Index of given element if element was found, -1 if not </returns>
    public int IndexOf(Vector2Int item) {
        for (int index = 0; _count > index; index++) {
            if (_storage[index] == item) return index;
        }
        return -1;
    }
}
