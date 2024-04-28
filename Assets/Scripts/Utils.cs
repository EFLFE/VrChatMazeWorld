using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Utils : UdonSharpBehaviour {
    private int spiralX;
    private int spiralY;
    private int spiralDX;
    private int spiralDY;
    private int spiralMaxSteps;
    private int spiralHalfSize;
    private int spiralSteps;

    public void Spiral(int size, int step, out int x, out int y) {
        x = 0;
        y = 0;
        int dx = 0, dy = -1;
        int maxI = size * size;

        for (int i = 0; i < maxI && i < step; i++) {
            if ((x == y) || ((x < 0) && (x == -y)) || ((x > 0) && (x == 1 - y))) {
                int t = dx;
                dx = -dy;
                dy = t;
            }
            x += dx;
            y += dy;
        }

        x += size / 2;
        y += size / 2;
    }

    public void ResetSpiral(int size) {
        spiralX = 0;
        spiralY = 0;
        spiralDX = 0;
        spiralDY = -1;
        spiralSteps = -1;
        spiralMaxSteps = size * size;
        spiralHalfSize = size / 2;
    }

    public void NextSpiral(out int x, out int y) {
        if (spiralSteps >= 0 && spiralSteps < spiralMaxSteps) {
            if ((spiralX == spiralY) || ((spiralX < 0) && (spiralX == -spiralY)) || ((spiralX > 0) && (spiralX == 1 - spiralY))) {
                int t = spiralDX;
                spiralDX = -spiralDY;
                spiralDY = t;
            }
            spiralX += spiralDX;
            spiralY += spiralDY;
        }

        spiralSteps++;
        x = spiralX + spiralHalfSize;
        y = spiralY + spiralHalfSize;
    }

    // from maze generator ids
    public Color GetFloorColor(int id) {
        Color clr;
        if (id == 1) {
            clr = Color.black;
        } else {
            const float D = 255f;
            switch (id % 7) {
                case 0: clr = new Color(225 / D, 155 / D, 155 / D); break;
                case 1: clr = new Color(225 / D, 205 / D, 158 / D); break;
                case 2: clr = new Color(175 / D, 225 / D, 158 / D); break;
                case 3: clr = new Color(158 / D, 223 / D, 225 / D); break;
                case 4: clr = new Color(158 / D, 158 / D, 225 / D); break;
                case 5: clr = new Color(225 / D, 158 / D, 225 / D); break;
                case 6: clr = new Color(158 / D, 158 / D, 158 / D); break;
                default: clr = Color.white; break;
            }
        }
        return clr;
    }

}
