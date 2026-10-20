using UnityEngine;

namespace Shmupper
{
    public class FxRing : MonoBehaviour
    {
        float _radius, _life, _maxLife;
        bool _growing;

        public void Play(float radius, float life, bool growing)
        {
            _radius = radius;
            _maxLife = life;
            _life = life;
            _growing = growing;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            float t = 1f - (_life / _maxLife);
            float scale = _growing ? Mathf.Lerp(0.15f, 1f, t) : Mathf.Lerp(1f, 0.15f, t);
            transform.localScale = new Vector3(_radius * 2f * scale, 0.02f, _radius * 2f * scale);
        }
    }

}
