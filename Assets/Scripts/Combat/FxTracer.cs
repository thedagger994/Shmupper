using UnityEngine;

namespace Shmupper
{
    public class FxTracer : MonoBehaviour
    {
        float _life, _maxLife;
        Vector3 _startScale;

        public void Play(float life)
        {
            _maxLife = life;
            _life = life;
            _startScale = transform.localScale;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            float t = _life / _maxLife;
            transform.localScale = new Vector3(_startScale.x * t, _startScale.y * t, _startScale.z);
        }
    }

}
