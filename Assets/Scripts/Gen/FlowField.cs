using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    /// Shared navigation for every walking enemy on the floor.
    ///
    /// Rather than give each enemy its own pathfinder, one breadth-first flood from the player is
    /// recomputed a few times a second and every enemy simply walks downhill. With dozens of
    /// enemies alive at once this is the difference between a stable frame rate and a slideshow,
    /// and it gives the horde the coordinated, funnelling feel the genre wants.
    public class FlowField
    {
        readonly DungeonMap _map;
        readonly int[] _dist;
        readonly Vector2Int[] _queue;

        static readonly Vector2Int[] Neighbours =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        public const int Unreachable = int.MaxValue;

        public FlowField(DungeonMap map)
        {
            _map = map;
            _dist = new int[map.Width * map.Height];
            _queue = new Vector2Int[map.Width * map.Height];
        }

        public void Rebuild(Vector3 targetWorld)
        {
            for (int i = 0; i < _dist.Length; i++) _dist[i] = Unreachable;

            Vector2Int start = _map.NearestOpen(targetWorld);
            if (!_map.IsOpen(start)) return;

            int head = 0, tail = 0;
            _dist[_map.Index(start.x, start.y)] = 0;
            _queue[tail++] = start;

            while (head < tail)
            {
                Vector2Int c = _queue[head++];
                int d = _dist[_map.Index(c.x, c.y)];

                for (int n = 0; n < Neighbours.Length; n++)
                {
                    Vector2Int t = c + Neighbours[n];
                    if (!_map.IsOpen(t)) continue;

                    int idx = _map.Index(t.x, t.y);
                    if (_dist[idx] != Unreachable) continue;

                    _dist[idx] = d + 1;
                    if (tail < _queue.Length) _queue[tail++] = t;
                }
            }
        }

        public int DistanceAt(Vector3 world)
        {
            Vector2Int c = _map.WorldToCell(world);
            if (!_map.IsOpen(c)) return Unreachable;
            return _dist[_map.Index(c.x, c.y)];
        }

        /// Direction that most reduces distance to the player, smoothed toward the centre of the
        /// chosen cell so enemies stop grinding along wall corners.
        public Vector3 DirectionAt(Vector3 world)
        {
            Vector2Int c = _map.NearestOpen(world);
            int here = _dist[_map.Index(c.x, c.y)];
            if (here == Unreachable) return Vector3.zero;

            int best = here;
            Vector2Int bestCell = c;

            for (int n = 0; n < Neighbours.Length; n++)
            {
                Vector2Int t = c + Neighbours[n];
                if (!_map.IsOpen(t)) continue;

                int d = _dist[_map.Index(t.x, t.y)];
                if (d < best) { best = d; bestCell = t; }
            }

            if (bestCell == c) return Vector3.zero;

            Vector3 target = _map.CellToWorld(bestCell, world.y);
            Vector3 dir = target - world;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        }
    }
}
