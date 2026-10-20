using UnityEngine;

namespace Shmupper
{
    public class FxShard : MonoBehaviour
    {
        Vector3 _velocity;
        float _life;
        float _maxLife;
        Vector3 _startScale;

        public void Launch(Vector3 velocity, float life)
        {
            _velocity = velocity;
            _maxLife = life;
            _life = life;
            _startScale = transform.localScale;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            _velocity += Physics.gravity * (Time.deltaTime * 0.5f);
            transform.position += _velocity * Time.deltaTime;
            transform.localScale = _startScale * Mathf.Clamp01(_life / _maxLife);
        }
    }

}
