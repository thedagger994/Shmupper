using UnityEngine;

namespace Shmupper
{
    public class PartSpin : MonoBehaviour
    {
        Vector3 _rate;

        public void Setup(Vector3 degreesPerSecond) => _rate = degreesPerSecond;

        void Update() => transform.localRotation *= Quaternion.Euler(_rate * Time.deltaTime);
    }

}
