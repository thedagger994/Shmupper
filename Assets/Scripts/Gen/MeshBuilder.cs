using System.Collections.Generic;
using UnityEngine;

namespace Shmupper
{
    /// Small accumulator for procedurally welded geometry. Quads are added with the normal the
    /// caller wants and the winding is corrected automatically, which removes an entire class of
    /// inside-out-wall bugs from the level builder.
    public class MeshBuilder
    {
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Vector3> _normals = new List<Vector3>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<Vector2> _uvs = new List<Vector2>();
        readonly List<int> _tris = new List<int>();

        /// World units per texture repeat. Because UVs are projected from world position rather
        /// than assigned per quad, neighbouring quads share coordinates at their shared edge and
        /// the stone tiles across a whole wall with no visible seam.
        public float TextureScale = 4f;

        public int VertexCount => _verts.Count;
        public bool IsEmpty => _tris.Count == 0;

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal,
                            Color ca, Color cb, Color cc, Color cd)
        {
            Vector3 computed = Vector3.Cross(b - a, c - a);
            bool flip = Vector3.Dot(computed, normal) < 0f;

            int baseIndex = _verts.Count;

            if (flip)
            {
                _verts.Add(a); _verts.Add(d); _verts.Add(c); _verts.Add(b);
                _colors.Add(ca); _colors.Add(cd); _colors.Add(cc); _colors.Add(cb);
                _uvs.Add(PlanarUv(a, normal)); _uvs.Add(PlanarUv(d, normal));
                _uvs.Add(PlanarUv(c, normal)); _uvs.Add(PlanarUv(b, normal));
            }
            else
            {
                _verts.Add(a); _verts.Add(b); _verts.Add(c); _verts.Add(d);
                _colors.Add(ca); _colors.Add(cb); _colors.Add(cc); _colors.Add(cd);
                _uvs.Add(PlanarUv(a, normal)); _uvs.Add(PlanarUv(b, normal));
                _uvs.Add(PlanarUv(c, normal)); _uvs.Add(PlanarUv(d, normal));
            }

            for (int i = 0; i < 4; i++) _normals.Add(normal);

            _tris.Add(baseIndex); _tris.Add(baseIndex + 1); _tris.Add(baseIndex + 2);
            _tris.Add(baseIndex); _tris.Add(baseIndex + 2); _tris.Add(baseIndex + 3);
        }

        /// Projects world position onto whichever axis plane the face points away from, so a
        /// vertical wall is textured by height and run, and a floor by its footprint.
        Vector2 PlanarUv(Vector3 p, Vector3 n)
        {
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);

            if (ay >= ax && ay >= az) return new Vector2(p.x, p.z) / TextureScale;
            if (ax >= az) return new Vector2(p.z, p.y) / TextureScale;
            return new Vector2(p.x, p.y) / TextureScale;
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(_verts);
            mesh.SetNormals(_normals);
            mesh.SetColors(_colors);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
