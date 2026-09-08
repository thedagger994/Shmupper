using UnityEngine;

namespace Shmupper
{
    /// Every visual in Shmupper is assembled from Unity primitives. This keeps the art direction
    /// honest to the brief - simple, distinctive silhouettes - and means no external assets.
    public static class Shapes
    {
        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos,
                                      Vector3 localScale, Material mat, Quaternion? rot = null,
                                      bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rot ?? Quaternion.identity;
            go.transform.localScale = localScale;

            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            return go;
        }

        public static GameObject Box(Transform parent, Vector3 pos, Vector3 scale, Material mat,
                                    Quaternion? rot = null, bool keepCollider = false)
            => Prim(PrimitiveType.Cube, parent, pos, scale, mat, rot, keepCollider);

        public static GameObject Ball(Transform parent, Vector3 pos, float diameter, Material mat)
            => Prim(PrimitiveType.Sphere, parent, pos, Vector3.one * diameter, mat);

        public static GameObject Pill(Transform parent, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null)
            => Prim(PrimitiveType.Capsule, parent, pos, scale, mat, rot);

        /// Unity has no cone primitive, so one is generated. Used for wizard robes, dragon
        /// snouts, helm crests and the shrine spire - the single most recognisable shape in the
        /// game's vocabulary.
        public static GameObject Cone(Transform parent, Vector3 pos, float radius, float height,
                                      Material mat, int sides = 8, Quaternion? rot = null)
        {
            var go = new GameObject("Cone");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot ?? Quaternion.identity;

            var mesh = ConeMesh(radius, height, sides);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        static Mesh ConeMesh(float radius, float height, int sides)
        {
            var mesh = new Mesh { name = "Cone" };
            var verts = new Vector3[sides * 3 + sides * 3];
            var tris = new int[sides * 3 + sides * 3];

            int v = 0, t = 0;
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                Vector3 tip = new Vector3(0f, height, 0f);

                verts[v] = p0; verts[v + 1] = tip; verts[v + 2] = p1;
                tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
                v += 3; t += 3;

                verts[v] = p1; verts[v + 1] = Vector3.zero; verts[v + 2] = p0;
                tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
                v += 3; t += 3;
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// Flat double sided quad - dragon wings, banners, shrine sigils.
        public static GameObject Blade(Transform parent, Vector3 pos, Vector2 size, Material mat, Quaternion? rot = null)
        {
            var go = new GameObject("Blade");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot ?? Quaternion.identity;

            var mesh = new Mesh { name = "Blade" };
            float hw = size.x * 0.5f, hh = size.y * 0.5f;
            mesh.vertices = new[]
            {
                new Vector3(-hw, -hh, 0), new Vector3(-hw, hh, 0), new Vector3(hw, hh, 0), new Vector3(hw, -hh, 0),
                new Vector3(-hw, -hh, 0), new Vector3(-hw, hh, 0), new Vector3(hw, hh, 0), new Vector3(hw, -hh, 0)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 6, 5, 4, 7, 6, 4 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }
    }
}
