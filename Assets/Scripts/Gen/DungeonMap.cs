using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    /// The logical floor plan. Purely data: the builder turns it into geometry, the AI turns it
    /// into a flow field, and the wave director turns it into spawn points.
    public class DungeonMap
    {
        public const float CellSize = 4f;
        public const float WallHeight = 6f;

        public int Width;
        public int Height;
        public bool[] Open;
        public bool[] IsRoom;

        public List<RectInt> Rooms = new List<RectInt>();
        public Vector2Int StartCell;
        public Vector2Int ShrineCell;
        public List<Vector2Int> SpawnCells = new List<Vector2Int>();
        public List<Vector2Int> TorchCells = new List<Vector2Int>();

        public DungeonMap(int w, int h)
        {
            Width = w;
            Height = h;
            Open = new bool[w * h];
            IsRoom = new bool[w * h];
        }

        public int Index(int x, int y) => y * Width + x;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool IsOpen(int x, int y) => InBounds(x, y) && Open[y * Width + x];

        public bool IsOpen(Vector2Int c) => IsOpen(c.x, c.y);

        public bool IsSolid(int x, int y) => !IsOpen(x, y);

        public Vector3 CellToWorld(Vector2Int c, float y = 0f)
            => new Vector3((c.x + 0.5f) * CellSize, y, (c.y + 0.5f) * CellSize);

        public Vector3 CellToWorld(int x, int y, float yPos = 0f)
            => new Vector3((x + 0.5f) * CellSize, yPos, (y + 0.5f) * CellSize);

        public Vector2Int WorldToCell(Vector3 p)
            => new Vector2Int(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.z / CellSize));

        public Vector3 Center => new Vector3(Width * CellSize * 0.5f, 0f, Height * CellSize * 0.5f);

        /// Nearest open cell to a world point, searched outward. Used whenever something needs to
        /// be snapped back onto the walkable graph after a knockback or a bad spawn roll.
        public Vector2Int NearestOpen(Vector3 world)
        {
            Vector2Int c = WorldToCell(world);
            if (IsOpen(c)) return c;

            for (int r = 1; r < 12; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        var t = new Vector2Int(c.x + dx, c.y + dy);
                        if (IsOpen(t)) return t;
                    }
                }
            }
            return StartCell;
        }
    }
}
