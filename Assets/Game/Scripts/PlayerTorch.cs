using UnityEngine;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game
{
    /// <summary>
    /// Toggles a 2D torch light on/off (default key: T).
    /// Attach to the player. Expects a child Light2D (spot/point) acting as the torch.
    /// The torch flips horizontally with the character's facing direction.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerTorch : MonoBehaviour
    {
        [Tooltip("The 2D light used as the torch. If left empty, the first child Light2D is used.")]
        public Light2D TorchLight;

        [Tooltip("Key used to toggle the torch (new Input System).")]
        public Key ToggleKey = Key.T;

        [Tooltip("Legacy Input Manager key fallback.")]
        public KeyCode ToggleKeyCode = KeyCode.T;

        [Tooltip("Whether the torch starts switched on.")]
        public bool StartOn = true;

        [Tooltip("Optional: a Transform whose localScale.x sign indicates facing direction (e.g. the model). Used to aim the torch left/right when the player is standing still.")]
        public Transform FacingReference;

        [Tooltip("How quickly the torch swings to follow the aim direction (degrees per second). Higher = snappier.")]
        public float TurnSpeed = 540f;

        [Tooltip("Minimum movement speed (units/sec) before the torch aims along the movement direction. Below this it falls back to horizontal facing.")]
        public float MoveThreshold = 0.5f;

        [Tooltip("How much the torch tilts toward vertical movement (0 = horizontal only, 1 = full follow of jump/fall).")]
        [Range(0f, 1f)]
        public float VerticalInfluence = 0.8f;

        private bool _on;
        private Vector3 _lastPos;
        private float _currentZ;          // current smoothed cone angle (world Z)
        private float _facingSign = 1f;   // +1 right, -1 left

        // The URP 2D spot cone is centered on local +Y (up). To aim it along a world
        // direction we offset by -90 degrees (so +X -> 0, +Y -> 90, etc).
        private const float ConeForwardOffset = -90f;

        private void Awake()
        {
            if (TorchLight == null)
                TorchLight = GetComponentInChildren<Light2D>(true);
        }

        private void OnEnable()
        {
            _on = StartOn;
            ApplyState();
            _lastPos = transform.position;
            _currentZ = 0f;
        }

        private void Update()
        {
            if (WasTogglePressed())
            {
                _on = !_on;
                ApplyState();
            }

            AimTorch();
        }

        private void AimTorch()
        {
            if (TorchLight == null) return;

            // Estimate movement this frame (engine-agnostic).
            Vector3 delta = transform.position - _lastPos;
            _lastPos = transform.position;
            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            Vector2 vel = new Vector2(delta.x, delta.y * VerticalInfluence) / dt;

            // Track horizontal facing for the idle fallback.
            if (FacingReference != null)
                _facingSign = FacingReference.lossyScale.x < 0f ? -1f : 1f;
            else if (Mathf.Abs(delta.x) > 1e-4f)
                _facingSign = Mathf.Sign(delta.x);

            // Decide aim direction: moving -> velocity, idle -> horizontal facing.
            Vector2 aimDir = vel.magnitude >= MoveThreshold
                ? vel.normalized
                : new Vector2(_facingSign, 0f);

            float targetZ = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg + ConeForwardOffset;

            // Smoothly rotate the torch toward the target angle.
            _currentZ = Mathf.MoveTowardsAngle(_currentZ, targetZ, TurnSpeed * Time.deltaTime);

            var e = TorchLight.transform.localEulerAngles;
            e.z = _currentZ;
            TorchLight.transform.localEulerAngles = e;
        }

        private bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb[ToggleKey].wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(ToggleKeyCode))
                return true;
#endif
            return false;
        }

        private void ApplyState()
        {
            if (TorchLight != null)
                TorchLight.enabled = _on;
        }

        /// <summary>Public API so other systems (UI buttons, abilities) can toggle the torch.</summary>
        public void Toggle()
        {
            _on = !_on;
            ApplyState();
        }

        public void SetTorch(bool on)
        {
            _on = on;
            ApplyState();
        }
    }
}
