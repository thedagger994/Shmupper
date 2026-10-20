using UnityEngine;

namespace Shmupper
{
    public class PartBob : MonoBehaviour
    {
        Vector3 _origin;
        float _amplitude, _speed, _phase;

        public void Setup(float amplitude, float speed)
        {
            _amplitude = amplitude;
            _speed = speed;
            _phase = Random.value * 10f;
            _origin = transform.localPosition;
        }

        void Update()
        {
            transform.localPosition = _origin + Vector3.up * (Mathf.Sin(Time.time * _speed + _phase) * _amplitude);
        }
    }

}
