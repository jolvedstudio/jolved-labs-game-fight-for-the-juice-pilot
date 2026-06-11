using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game
{
    /// <summary>
    /// Occasional random flicker for a 2D light, evoking failing/abandoned-facility lighting.
    /// Most of the time the light is steady; every so often (at least MinInterval seconds apart)
    /// it performs a short burst of flickering, then returns to its base intensity.
    /// Designed to be cheap: only does work during the brief flicker windows.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    [DisallowMultipleComponent]
    public class LightFlicker2D : MonoBehaviour
    {
        [Header("Timing (seconds)")]
        [Tooltip("Minimum time between the start of one flicker burst and the next. Keep >= 10 for a sparse, eerie feel.")]
        public float MinInterval = 10f;
        [Tooltip("Maximum time between flicker bursts.")]
        public float MaxInterval = 25f;

        [Header("Flicker burst")]
        [Tooltip("Shortest duration of a single flicker burst.")]
        public float MinBurstDuration = 0.08f;
        [Tooltip("Longest duration of a single flicker burst.")]
        public float MaxBurstDuration = 0.45f;
        [Tooltip("How fast the intensity jitters during a burst (changes per second).")]
        public float JitterSpeed = 22f;

        [Header("Intensity")]
        [Tooltip("Lowest fraction of base intensity reached while flickering (0 = fully off).")]
        [Range(0f, 1f)] public float MinIntensityFactor = 0.1f;

        private Light2D _light;
        private float _baseIntensity;
        private float _nextFlickerTime;
        private float _burstEndTime;
        private float _nextJitterTime;
        private bool _bursting;

        private void Awake()
        {
            _light = GetComponent<Light2D>();
            _baseIntensity = _light.intensity;
            ScheduleNext();
        }

        private void OnEnable()
        {
            // Re-randomise so pooled / re-enabled lights don't sync up
            if (_light != null) _light.intensity = _baseIntensity;
            ScheduleNext();
        }

        private void ScheduleNext()
        {
            // Desync each light with a random phase so they never flicker together
            _nextFlickerTime = Time.time + Random.Range(MinInterval, MaxInterval);
            _bursting = false;
        }

        private void Update()
        {
            float now = Time.time;

            if (!_bursting)
            {
                if (now >= _nextFlickerTime)
                {
                    _bursting = true;
                    _burstEndTime = now + Random.Range(MinBurstDuration, MaxBurstDuration);
                    _nextJitterTime = 0f;
                }
                return;
            }

            // In a burst: jitter intensity rapidly
            if (now >= _burstEndTime)
            {
                _light.intensity = _baseIntensity;
                ScheduleNext();
                return;
            }

            if (now >= _nextJitterTime)
            {
                float factor = Random.Range(MinIntensityFactor, 1f);
                _light.intensity = _baseIntensity * factor;
                _nextJitterTime = now + (1f / Mathf.Max(1f, JitterSpeed));
            }
        }

        private void OnDisable()
        {
            if (_light != null) _light.intensity = _baseIntensity;
        }
    }
}
