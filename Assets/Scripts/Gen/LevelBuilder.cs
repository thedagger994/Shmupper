using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    /// Turns a DungeonMap into playable castle geometry.
    ///
    /// Two decisions drive this class. First, geometry is welded into 8x8-cell chunks rather than
    /// spawned as thousands of cubes, so a floor is a few dozen draw calls instead of a few
    /// thousand. Second, torchlight is baked into vertex colours at build time and drawn with an
    /// unlit shader, which sidesteps the per-object realtime light limit entirely and gives the
    /// stone the blotchy, hand-placed look the old arena shooters had.
    public static class LevelBuilder
    {
        const int ChunkCells = 8;

        // Lighting was tuned far too dark to actually play in: torch pools barely overlapped and
        // the gaps between them were near black. The reach and strength of the bake are both up,
        // and ambient is raised separately, which lifts the floor of the image without washing
        // out the pools of light that give the dungeon its shape.
        const float TorchRadius = 24f;
        const float TorchIntensity = 1.75f;
        const int MaxRealLights = 48;

        public class BuiltLevel
        {
            public GameObject Root;
            public List<TorchFlicker> Torches = new List<TorchFlicker>();
            public Vector3 PlayerSpawn;
            public Vector3 ShrinePosition;
        }

        struct TorchSample
        {
            public Vector3 Position;
            public Color Color;
        }

        public static BuiltLevel Build(DungeonMap map, int floorNumber)
        {
            var result = new BuiltLevel();
            var root = new GameObject("Level_Floor" + floorNumber);
            result.Root = root;

            Color tint = FloorTint(floorNumber);
            Color ambient = new Color(0.17f, 0.18f, 0.235f) * Mathf.Lerp(1f, 1.25f, floorNumber * 0.06f);

            var stone = StoneTexture.Get();

            // The floor takes the texture at a lower strength than the walls: at a glancing
            // viewing angle a strongly patterned floor turns into aliased noise.
            var floorMat = MatLib.VertexLit(Palette.StoneFloor * tint, ambient, 1.25f, stone, 0.75f);
            var wallMat = MatLib.VertexLit(Palette.StoneWall * tint, ambient, 1.25f, stone, 1f);
            var ceilMat = MatLib.VertexLit(Palette.Ceiling * tint, ambient * 0.75f, 1.15f, stone, 0.55f);

            var torches = BuildTorchSamples(map, floorNumber);

            int chunksX = Mathf.CeilToInt(map.Width / (float)ChunkCells);
            int chunksY = Mathf.CeilToInt(map.Height / (float)ChunkCells);

            for (int cy = 0; cy < chunksY; cy++)
                for (int cx = 0; cx < chunksX; cx++)
                    BuildChunk(map, root.transform, cx, cy, torches, floorMat, wallMat, ceilMat);

            SpawnTorchProps(map, root.transform, torches, result);
            SpawnPillars(map, root.transform, tint);

            result.PlayerSpawn = map.CellToWorld(map.StartCell, 1.2f);
            result.ShrinePosition = map.CellToWorld(map.ShrineCell, 0f);

            Layers.Apply(root, Layers.Level);
            return result;
        }

        /// Each descent rotates the stone's hue so the player can tell how deep they are from a
        /// single glance at a wall.
        ///
        /// This walks the colour wheel rather than picking from a fixed list, so it keeps giving
        /// distinct floors however far down the player gets. The golden-ratio step means
        /// successive floors land far apart on the wheel instead of drifting gradually, and
        /// saturation stays low - this is weathered stone catching coloured light, not paint.
        static Color FloorTint(int floor)
        {
            float hue = Mathf.Repeat(0.58f + (floor - 1) * 0.618f, 1f);
            float saturation = Mathf.Lerp(0.10f, 0.26f, Mathf.Clamp01((floor - 1) / 8f));

            Color c = Color.HSVToRGB(hue, saturation, 1f);

            // Pull back toward neutral so the tint reads as a cast over grey stone rather than
            // recolouring it outright.
            return Color.Lerp(Color.white, c, 0.65f);
        }

        static List<TorchSample> BuildTorchSamples(DungeonMap map, int floorNumber)
        {
            var list = new List<TorchSample>();
            Color warm = Palette.TorchLight;
            Color cold = Palette.ArcaneLight;

            for (int i = 0; i < map.TorchCells.Count; i++)
            {
                var cell = map.TorchCells[i];
                Vector3 pos = map.CellToWorld(cell, 3.2f);

                // Every fourth sconce burns arcane blue. It reads as castle wiring rather than
                // decoration and keeps the palette from going uniformly orange.
                bool arcane = (i + floorNumber) % 4 == 0;
                list.Add(new TorchSample { Position = pos, Color = arcane ? cold : warm });
            }
            return list;
        }

        static void BuildChunk(DungeonMap map, Transform parent, int cx, int cy,
                               List<TorchSample> allTorches, Material floorMat, Material wallMat, Material ceilMat)
        {
            int x0 = cx * ChunkCells;
            int y0 = cy * ChunkCells;
            int x1 = Mathf.Min(x0 + ChunkCells, map.Width);
            int y1 = Mathf.Min(y0 + ChunkCells, map.Height);

            var local = FilterTorches(allTorches, map, x0, y0, x1, y1);

            var floor = new MeshBuilder();
            var walls = new MeshBuilder();
            var ceiling = new MeshBuilder();

            float cs = DungeonMap.CellSize;
            float wh = DungeonMap.WallHeight;

            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    if (!map.IsOpen(x, y)) continue;

                    float wx0 = x * cs, wx1 = (x + 1) * cs;
                    float wz0 = y * cs, wz1 = (y + 1) * cs;

                    AddLitQuad(floor, local,
                        new Vector3(wx0, 0, wz0), new Vector3(wx0, 0, wz1),
                        new Vector3(wx1, 0, wz1), new Vector3(wx1, 0, wz0), Vector3.up);

                    AddLitQuad(ceiling, local,
                        new Vector3(wx0, wh, wz0), new Vector3(wx1, wh, wz0),
                        new Vector3(wx1, wh, wz1), new Vector3(wx0, wh, wz1), Vector3.down);

                    if (map.IsSolid(x + 1, y))
                        AddWall(walls, local, new Vector3(wx1, 0, wz0), new Vector3(wx1, 0, wz1), Vector3.left, wh);
                    if (map.IsSolid(x - 1, y))
                        AddWall(walls, local, new Vector3(wx0, 0, wz1), new Vector3(wx0, 0, wz0), Vector3.right, wh);
                    if (map.IsSolid(x, y + 1))
                        AddWall(walls, local, new Vector3(wx1, 0, wz1), new Vector3(wx0, 0, wz1), Vector3.back, wh);
                    if (map.IsSolid(x, y - 1))
                        AddWall(walls, local, new Vector3(wx0, 0, wz0), new Vector3(wx1, 0, wz0), Vector3.forward, wh);
                }
            }

            if (floor.IsEmpty && walls.IsEmpty) return;

            var chunk = new GameObject("Chunk_" + cx + "_" + cy);
            chunk.transform.SetParent(parent, false);
            chunk.isStatic = true;

            AttachPiece(chunk.transform, "Floor", floor, floorMat, true);
            AttachPiece(chunk.transform, "Walls", walls, wallMat, true);
            AttachPiece(chunk.transform, "Ceiling", ceiling, ceilMat, true);
        }

        static List<TorchSample> FilterTorches(List<TorchSample> all, DungeonMap map, int x0, int y0, int x1, int y1)
        {
            float cs = DungeonMap.CellSize;
            var bounds = new Bounds();
            bounds.SetMinMax(
                new Vector3(x0 * cs - TorchRadius, -TorchRadius, y0 * cs - TorchRadius),
                new Vector3(x1 * cs + TorchRadius, DungeonMap.WallHeight + TorchRadius, y1 * cs + TorchRadius));

            var list = new List<TorchSample>();
            foreach (var t in all)
                if (bounds.Contains(t.Position)) list.Add(t);
            return list;
        }

        /// Walls are split into three vertical bands. One quad per face would light the whole
        /// wall from its corners only and torches would smear; three bands is enough to see a
        /// pool of light fall off toward the ceiling.
        static void AddWall(MeshBuilder mb, List<TorchSample> torches, Vector3 a, Vector3 b, Vector3 normal, float height)
        {
            const int Bands = 3;
            for (int i = 0; i < Bands; i++)
            {
                float h0 = height * (i / (float)Bands);
                float h1 = height * ((i + 1) / (float)Bands);

                AddLitQuad(mb, torches,
                    a + Vector3.up * h0, a + Vector3.up * h1,
                    b + Vector3.up * h1, b + Vector3.up * h0, normal);
            }
        }

        static void AddLitQuad(MeshBuilder mb, List<TorchSample> torches,
                               Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            mb.AddQuad(a, b, c, d, normal,
                Sample(torches, a, normal), Sample(torches, b, normal),
                Sample(torches, c, normal), Sample(torches, d, normal));
        }

        static Color Sample(List<TorchSample> torches, Vector3 p, Vector3 n)
        {
            float r = 0f, g = 0f, bl = 0f;

            for (int i = 0; i < torches.Count; i++)
            {
                Vector3 delta = torches[i].Position - p;
                float sqr = delta.sqrMagnitude;
                if (sqr > TorchRadius * TorchRadius) continue;

                float dist = Mathf.Sqrt(sqr);
                float atten = 1f - (dist / TorchRadius);
                atten *= atten;

                float ndotl = dist > 0.001f ? Vector3.Dot(n, delta / dist) : 1f;
                // A generous wrap term keeps surfaces facing away from a torch from going pure
                // black, which would hide walls the player still needs to navigate around.
                ndotl = Mathf.Max(0.38f, ndotl * 0.72f + 0.28f);

                float k = atten * ndotl * TorchIntensity;
                var col = torches[i].Color;
                r += col.r * k; g += col.g * k; bl += col.b * k;
            }

            return new Color(Mathf.Min(r, 2f), Mathf.Min(g, 2f), Mathf.Min(bl, 2f), 1f);
        }

        static void AttachPiece(Transform parent, string name, MeshBuilder mb, Material mat, bool collide)
        {
            if (mb.IsEmpty) return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.isStatic = true;

            var mesh = mb.ToMesh(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            if (collide)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
            }
        }

        static void SpawnTorchProps(DungeonMap map, Transform parent, List<TorchSample> torches, BuiltLevel result)
        {
            var holder = new GameObject("Torches");
            holder.transform.SetParent(parent, false);

            int lightStride = Mathf.Max(1, Mathf.CeilToInt(torches.Count / (float)MaxRealLights));

            for (int i = 0; i < torches.Count; i++)
            {
                var t = torches[i];
                var go = new GameObject("Torch");
                go.transform.SetParent(holder.transform, false);
                go.transform.position = t.Position;

                var iron = MatLib.Lit(new Color(0.14f, 0.13f, 0.12f), default, 0.45f, 0.7f);

                // The mount is a separate child so the whole sconce can sway as one piece while
                // the flame leans independently inside it. Rotating the torch root instead would
                // swing the light and the ember origin around with it.
                var mount = new GameObject("Mount");
                mount.transform.SetParent(go.transform, false);

                Shapes.Box(mount.transform, Vector3.down * 0.5f, new Vector3(0.1f, 1f, 0.1f), iron,
                    Quaternion.Euler(14f, 0f, 0f));
                Shapes.Box(mount.transform, Vector3.down * 0.95f, new Vector3(0.3f, 0.09f, 0.3f), iron);
                Shapes.Prim(PrimitiveType.Cylinder, mount.transform, Vector3.down * 0.12f,
                    new Vector3(0.3f, 0.16f, 0.3f), iron);

                var flame = Shapes.Ball(mount.transform, Vector3.zero, 0.55f,
                    MatLib.Lit(t.Color * 0.2f, t.Color * 3.4f));

                // A dimmer inner core gives the flame some depth instead of one flat ball.
                Shapes.Ball(flame.transform, Vector3.up * 0.18f, 0.55f,
                    MatLib.Lit(t.Color * 0.1f, t.Color * 1.6f));

                Light light = null;
                if (i % lightStride == 0)
                {
                    light = go.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = t.Color;
                    light.range = 21f;
                    light.intensity = 4.2f;
                    light.shadows = LightShadows.None;
                }

                var flicker = go.AddComponent<TorchFlicker>();
                flicker.Setup(light, flame.transform, 4.2f, mount.transform);
                flicker.SetEmberColor(t.Color);
                result.Torches.Add(flicker);
            }
        }

        /// Pillars are the only cover in the castle. They go on a lattice inside larger rooms so
        /// every arena has something to break line of sight against a wizard.
        static void SpawnPillars(DungeonMap map, Transform parent, Color tint)
        {
            var holder = new GameObject("Pillars");
            holder.transform.SetParent(parent, false);

            var stone = MatLib.Lit(Palette.Pillar * tint, default, 0.1f);
            var rune = MatLib.Lit(Palette.ArcaneLight * 0.25f, Palette.ArcaneLight * 1.8f);

            foreach (var room in map.Rooms)
            {
                if (room.width < 8 || room.height < 8) continue;

                for (int y = room.yMin + 2; y < room.yMax - 2; y += 4)
                {
                    for (int x = room.xMin + 2; x < room.xMax - 2; x += 4)
                    {
                        if (!map.IsOpen(x, y)) continue;

                        var pillar = new GameObject("Pillar");
                        pillar.transform.SetParent(holder.transform, false);
                        pillar.transform.position = map.CellToWorld(x, y, 0f);

                        Shapes.Prim(PrimitiveType.Cube, pillar.transform,
                            Vector3.up * (DungeonMap.WallHeight * 0.5f),
                            new Vector3(1.6f, DungeonMap.WallHeight, 1.6f), stone, null, true);

                        Shapes.Box(pillar.transform, Vector3.up * 2.2f, new Vector3(1.75f, 0.18f, 1.75f), rune);
                        Shapes.Box(pillar.transform, Vector3.up * 4.4f, new Vector3(1.75f, 0.18f, 1.75f), rune);
                    }
                }
            }
        }
    }
}
