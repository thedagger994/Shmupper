using UnityEngine;

namespace Shmupper
{
    public struct DamageInfo
    {
        public float Amount;
        public Vector3 Point;
        public Vector3 Normal;
        public Vector3 Push;
        public bool FromPlayer;
        public bool IsExplosive;
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        Transform Transform { get; }
        void TakeDamage(DamageInfo info);
    }

    public static class DamageUtil
    {
        /// Colliders on enemies sit on child objects so the hit shape can differ from the visual
        /// root. Walking the parent chain keeps every weapon from needing to know that.
        public static IDamageable Find(Collider col)
        {
            if (col == null) return null;
            var d = col.GetComponent<IDamageable>();
            if (d != null) return d;
            return col.GetComponentInParent<IDamageable>();
        }
    }
}
