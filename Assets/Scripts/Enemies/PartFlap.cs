using UnityEngine;

namespace Shmupper
{
    public class PartFlap : MonoBehaviour
    {
        float _dir, _phase;

        public void Setup(float direction)
        {
            _dir = direction;
            _phase = Random.value * 6f;
        }

        void Update()
        {
            float a = Mathf.Sin(Time.time * 11f + _phase) * 34f;
            transform.localRotation = Quaternion.Euler(0f, 0f, a * _dir);
        }
    }
}
