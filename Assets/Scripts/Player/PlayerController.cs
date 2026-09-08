using UnityEngine;
using UnityEngine.InputSystem;

namespace Shmupper
{
    /// Movement in the arena-shooter tradition: high top speed, near-instant acceleration on the
    /// ground, and Quake's projected air acceleration so strafing while turning gains speed. The
    /// brief asked for fluid gameplay, and in a first person shmup that means the player should
    /// never feel like they are waiting for the character to respond.
    public class PlayerController : MonoBehaviour
    {
        public float BaseSpeed = 9.2f;
        public float GroundAccel = 75f;
        public float AirAccel = 42f;
        public float AirSpeedCap = 1.6f;
        public float Friction = 8.5f;
        public float Gravity = 26f;
        public float JumpVelocity = 8.6f;
        public float MouseSensitivity = 0.14f;

        CharacterController _cc;
        Transform _camera;
        RunState _run;

        Vector3 _velocity;
        float _pitch;
        float _yaw;
        float _recoilPitch;
        float _coyote;
        float _jumpBuffer;

        float _shakeAmount;
        float _shakeTimer;
        Vector3 _cameraBaseLocal;

        public bool InputEnabled = true;
        public bool LookEnabled = true;
        public bool IsGrounded { get; private set; }
        public float SpeedFraction { get; private set; }
        public Vector3 HorizontalVelocity => new Vector3(_velocity.x, 0f, _velocity.z);
        public float Yaw => _yaw;

        public void Init(Transform cameraTransform, RunState run)
        {
            _cc = GetComponent<CharacterController>();
            _camera = cameraTransform;
            _run = run;
            _cameraBaseLocal = _camera.localPosition;
            _yaw = transform.eulerAngles.y;
        }

        /// Swapping in a fresh run must not re-read the camera's rest position: by then the
        /// camera has been shifted by shake and roll, and capturing that would leave the view
        /// permanently off centre.
        public void SetRun(RunState run) => _run = run;

        public void Teleport(Vector3 position, float yaw)
        {
            if (_cc != null) _cc.enabled = false;
            transform.position = position;
            _yaw = yaw;
            _pitch = 0f;
            _velocity = Vector3.zero;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (_cc != null) _cc.enabled = true;
        }

        public void AddRecoil(float amount) => _recoilPitch += amount;

        public void Shake(float amount, float duration = 0.25f)
        {
            _shakeAmount = Mathf.Max(_shakeAmount, amount);
            _shakeTimer = Mathf.Max(_shakeTimer, duration);
        }

        public void AddImpulse(Vector3 impulse)
        {
            _velocity += impulse;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            HandleLook(dt);
            HandleMove(dt);
            HandleCameraOffsets(dt);
        }

        void HandleLook(float dt)
        {
            if (LookEnabled)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    _yaw += delta.x * MouseSensitivity;
                    _pitch -= delta.y * MouseSensitivity;
                }

                var pad = Gamepad.current;
                if (pad != null)
                {
                    Vector2 look = pad.rightStick.ReadValue() * (220f * dt);
                    _yaw += look.x;
                    _pitch -= look.y;
                }
            }

            _pitch = Mathf.Clamp(_pitch, -88f, 88f);
            _recoilPitch = Mathf.MoveTowards(_recoilPitch, 0f, dt * 18f);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        void HandleMove(float dt)
        {
            Vector2 wish = ReadMoveAxis();
            Vector3 wishDir = (transform.right * wish.x + transform.forward * wish.y);
            if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

            IsGrounded = _cc != null && _cc.isGrounded;

            if (IsGrounded) _coyote = 0.12f;
            else _coyote -= dt;

            if (InputEnabled && JumpPressed()) _jumpBuffer = 0.14f;
            else _jumpBuffer -= dt;

            float maxSpeed = BaseSpeed * (_run != null ? _run.MoveSpeedMul : 1f);

            if (IsGrounded && _velocity.y <= 0.1f)
            {
                ApplyFriction(dt);
                Accelerate(wishDir, maxSpeed, GroundAccel, dt);
                _velocity.y = -2f;

                if (_jumpBuffer > 0f && _coyote > 0f)
                {
                    _velocity.y = JumpVelocity * (_run != null ? _run.JumpMul : 1f);
                    _jumpBuffer = 0f;
                    _coyote = 0f;
                }
            }
            else
            {
                // Quake air control: acceleration is capped against the component of velocity
                // already going the way you want, which is what lets a turning strafe build speed.
                Accelerate(wishDir, AirSpeedCap, AirAccel, dt);
                _velocity.y -= Gravity * dt;
            }

            if (_cc != null) _cc.Move(_velocity * dt);

            SpeedFraction = Mathf.Clamp01(HorizontalVelocity.magnitude / Mathf.Max(1f, maxSpeed));
        }

        Vector2 ReadMoveAxis()
        {
            if (!InputEnabled) return Vector2.zero;

            Vector2 v = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
            }

            var pad = Gamepad.current;
            if (pad != null && v.sqrMagnitude < 0.01f) v = pad.leftStick.ReadValue();

            return v;
        }

        bool JumpPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;

            var pad = Gamepad.current;
            return pad != null && pad.buttonSouth.wasPressedThisFrame;
        }

        void ApplyFriction(float dt)
        {
            Vector3 flat = HorizontalVelocity;
            float speed = flat.magnitude;
            if (speed < 0.01f) { _velocity.x = 0f; _velocity.z = 0f; return; }

            float drop = Mathf.Max(speed, 4f) * Friction * dt;
            float scale = Mathf.Max(0f, speed - drop) / speed;

            _velocity.x *= scale;
            _velocity.z *= scale;
        }

        void Accelerate(Vector3 wishDir, float wishSpeed, float accel, float dt)
        {
            if (wishDir.sqrMagnitude < 0.001f) return;

            float current = Vector3.Dot(HorizontalVelocity, wishDir);
            float add = wishSpeed - current;
            if (add <= 0f) return;

            float accelSpeed = Mathf.Min(accel * wishSpeed * dt, add);
            _velocity.x += wishDir.x * accelSpeed;
            _velocity.z += wishDir.z * accelSpeed;
        }

        void HandleCameraOffsets(float dt)
        {
            if (_camera == null) return;

            Vector3 shakeOffset = Vector3.zero;
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= dt;
                float k = Mathf.Clamp01(_shakeTimer / 0.25f) * _shakeAmount;
                shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 34f, 0f) - 0.5f) * k,
                    (Mathf.PerlinNoise(0f, Time.time * 31f) - 0.5f) * k,
                    0f);
                if (_shakeTimer <= 0f) _shakeAmount = 0f;
            }

            // A slight roll into the strafe direction sells the speed without making the player
            // seasick; it is scaled by actual sideways velocity rather than input.
            float sideways = Vector3.Dot(HorizontalVelocity, transform.right);
            float roll = Mathf.Clamp(-sideways * 0.18f, -2.4f, 2.4f);

            _camera.localPosition = _cameraBaseLocal + shakeOffset;
            _camera.localRotation = Quaternion.Euler(_pitch + _recoilPitch, 0f, roll);
        }
    }
}
