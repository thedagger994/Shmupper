using UnityEngine;

namespace Shmupper
{
    public class FxPulse : MonoBehaviour
    {
        float _from, _to, _life, _maxLife;

        public void Play(float from, float to, float life)
        {
            _from = from;
            _to = to;
            _maxLife = life;
            _life = life;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            float t = 1f - (_life / _maxLife);
            float size = Mathf.Lerp(_from, _to, t);
            transform.localScale = Vector3.one * size * (1f - t * 0.55f);
        }
    }
}
