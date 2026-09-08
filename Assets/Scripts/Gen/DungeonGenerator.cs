using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    /// Carves a castle floor: rectangular halls joined by two-cell-wide corridors, with a
    /// guaranteed route from the entry hall to the wayshrine. Corridors are deliberately wide -
    /// a one-cell maze would strangle the dodge-and-strafe combat the game is built around.
    public static class DungeonGenerator
    {
        public static DungeonMap Generate(int seed, int floorNumber)
        {
            var rng = new System.Random(seed);

            int size = Mathf.Clamp(34 + floorNumber * 3, 34, 58);
            var map = new DungeonMap(size, size);

            int targetRooms = Mathf.Clamp(6 + floorNumber, 6, 13);
            int attempts = 0;

            while (map.Rooms.Count < targetRooms && attempts < 600)
            {
                attempts++;
                int rw = rng.Next(6, 13);
                int rh = rng.Next(6, 13);
                int rx = rng.Next(2, Mathf.Max(3, size - rw - 2));
                int ry = rng.Next(2, Mathf.Max(3, size - rh - 2));
                var rect = new RectInt(rx, ry, rw, rh);

                bool clashes = false;
                foreach (var other in map.Rooms)
                {
                    var padded = new RectInt(other.x - 3, other.y - 3, other.width + 6, other.height + 6);
                    if (padded.Overlaps(rect)) { clashes = true; break; }
                }
                if (clashes) continue;

                map.Rooms.Add(rect);
                CarveRect(map, rect);
            }

            ConnectRooms(map, rng);
            PickLandmarks(map, rng);
            PlaceTorches(map);

            return map;
        }

        static void CarveRect(DungeonMap map, RectInt r)
        {
            for (int y = r.yMin; y < r.yMax; y++)
            {
                for (int x = r.xMin; x < r.xMax; x++)
                {
                    if (!map.InBounds(x, y)) continue;
                    map.Open[map.Index(x, y)] = true;
                    map.IsRoom[map.Index(x, y)] = true;
                }
            }
        }

        /// Nearest-neighbour spanning walk over room centres, plus a few extra links so the floor
        /// contains loops. Loops matter: dead ends turn a shmup into a corridor shooter.
        static void ConnectRooms(DungeonMap map, System.Random rng)
        {
            if (map.Rooms.Count < 2) return;

            var connected = new List<int> { 0 };
            var remaining = new List<int>();
            for (int i = 1; i < map.Rooms.Count; i++) remaining.Add(i);

            while (remaining.Count > 0)
            {
                int bestFrom = 0, bestTo = 0, bestIdx = 0;
                float bestDist = float.MaxValue;

                for (int a = 0; a < connected.Count; a++)
                {
                    for (int b = 0; b < remaining.Count; b++)
                    {
                        float d = Vector2.Distance(Center(map.Rooms[connected[a]]), Center(map.Rooms[remaining[b]]));
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestFrom = connected[a];
                            bestTo = remaining[b];
                            bestIdx = b;
                        }
                    }
                }

                CarveCorridor(map, map.Rooms[bestFrom], map.Rooms[bestTo], rng);
                connected.Add(bestTo);
                remaining.RemoveAt(bestIdx);
            }

            int extra = Mathf.Max(1, map.Rooms.Count / 3);
            for (int i = 0; i < extra; i++)
            {
                int a = rng.Next(map.Rooms.Count);
                int b = rng.Next(map.Rooms.Count);
                if (a != b) CarveCorridor(map, map.Rooms[a], map.Rooms[b], rng);
            }
        }

        static Vector2 Center(RectInt r) => new Vector2(r.center.x, r.center.y);

        static void CarveCorridor(DungeonMap map, RectInt a, RectInt b, System.Random rng)
        {
            var pa = new Vector2Int(Mathf.RoundToInt(a.center.x), Mathf.RoundToInt(a.center.y));
            var pb = new Vector2Int(Mathf.RoundToInt(b.center.x), Mathf.RoundToInt(b.center.y));

            if (rng.Next(2) == 0)
            {
                CarveLineX(map, pa.x, pb.x, pa.y);
                CarveLineY(map, pa.y, pb.y, pb.x);
            }
            else
            {
                CarveLineY(map, pa.y, pb.y, pa.x);
                CarveLineX(map, pa.x, pb.x, pb.y);
            }
        }

        static void CarveLineX(DungeonMap map, int x0, int x1, int y)
        {
            int step = x0 <= x1 ? 1 : -1;
            for (int x = x0; x != x1 + step; x += step)
                for (int dy = 0; dy < 2; dy++)
                    if (map.InBounds(x, y + dy)) map.Open[map.Index(x, y + dy)] = true;
        }

        static void CarveLineY(DungeonMap map, int y0, int y1, int x)
        {
            int step = y0 <= y1 ? 1 : -1;
            for (int y = y0; y != y1 + step; y += step)
                for (int dx = 0; dx < 2; dx++)
                    if (map.InBounds(x + dx, y)) map.Open[map.Index(x + dx, y)] = true;
        }

        /// Entry hall is room zero; the shrine goes in whichever room is furthest away, so the
        /// player is always pulled across the whole floor before they are allowed to descend.
        static void PickLandmarks(DungeonMap map, System.Random rng)
        {
            if (map.Rooms.Count == 0)
            {
                map.StartCell = new Vector2Int(map.Width / 2, map.Height / 2);
                map.ShrineCell = map.StartCell;
                map.Open[map.Index(map.StartCell.x, map.StartCell.y)] = true;
                map.SpawnCells.Add(map.StartCell);
                return;
            }

            var start = map.Rooms[0];
            map.StartCell = new Vector2Int(Mathf.RoundToInt(start.center.x), Mathf.RoundToInt(start.center.y));

            int farthest = 0;
            float bestDist = -1f;
            for (int i = 1; i < map.Rooms.Count; i++)
            {
                float d = Vector2.Distance(Center(start), Center(map.Rooms[i]));
                if (d > bestDist) { bestDist = d; farthest = i; }
            }

            var shrineRoom = map.Rooms[farthest];
            map.ShrineCell = new Vector2Int(Mathf.RoundToInt(shrineRoom.center.x), Mathf.RoundToInt(shrineRoom.center.y));

            for (int i = 0; i < map.Rooms.Count; i++)
            {
                var r = map.Rooms[i];
                int pads = Mathf.Clamp((r.width * r.height) / 22, 1, 4);
                for (int p = 0; p < pads; p++)
                {
                    int x = rng.Next(r.xMin + 1, r.xMax - 1);
                    int y = rng.Next(r.yMin + 1, r.yMax - 1);
                    var c = new Vector2Int(x, y);
                    if (i == 0 && Vector2Int.Distance(c, map.StartCell) < 4f) continue;
                    if (map.IsOpen(c)) map.SpawnCells.Add(c);
                }
            }

            if (map.SpawnCells.Count == 0) map.SpawnCells.Add(map.ShrineCell);
        }

        /// Torches are picked deterministically from the cell coordinates rather than randomly,
        /// so lighting stays evenly spread instead of clumping the way pure noise does.
        static void PlaceTorches(DungeonMap map)
        {
            for (int y = 1; y < map.Height - 1; y++)
            {
                for (int x = 1; x < map.Width - 1; x++)
                {
                    if (!map.IsOpen(x, y)) continue;

                    bool wallAdjacent = map.IsSolid(x + 1, y) || map.IsSolid(x - 1, y) ||
                                        map.IsSolid(x, y + 1) || map.IsSolid(x, y - 1);
                    if (!wallAdjacent) continue;
                    if (((x * 7) + (y * 13)) % 9 != 0) continue;

                    map.TorchCells.Add(new Vector2Int(x, y));
                }
            }
        }
    }
}
