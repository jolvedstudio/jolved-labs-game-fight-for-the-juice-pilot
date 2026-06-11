using UnityEngine;

namespace AbandonedFacility
{
    /// <summary>
    /// Makes a SpriteRenderer flicker like a failing emergency light in an abandoned facility.
    /// Randomly dips brightness with occasional longer blackouts.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FlickerLight : MonoBehaviour
    {
        [Tooltip("Base color/intensity of the light when fully on.")]
        public Color BaseColor = new Color(0.4f, 0.9f, 1f, 0.8f);

        [Tooltip("Minimum brightness multiplier during a flicker dip.")]
        [Range(0f, 1f)] public float MinIntensity = 0.15f;

        [Tooltip("How quickly the light reacts to flicker changes.")]
        public float FlickerSpeed = 14f;

        [Tooltip("Chance per second of a longer blackout.")]
        public float BlackoutChance = 0.4f;

        private SpriteRenderer _sr;
        private float _target = 1f;
        private float _current = 1f;
        private float _blackoutTimer;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_blackoutTimer > 0f)
            {
                _blackoutTimer -= Time.deltaTime;
                _target = MinIntensity * 0.3f;
            }
            else
            {
                // Random flicker around full brightness, occasionally dipping.
                float r = Random.value;
                if (r < 0.06f) _target = Random.Range(MinIntensity, 0.6f);
                else if (r < 0.10f) _target = Random.Range(0.6f, 1f);
                else _target = 1f;

                if (Random.value < BlackoutChance * Time.deltaTime)
                    _blackoutTimer = Random.Range(0.08f, 0.35f);
            }

            _current = Mathf.Lerp(_current, _target, Time.deltaTime * FlickerSpeed);
            var c = BaseColor;
            c.a = BaseColor.a * _current;
            // also dim rgb slightly for a more electrical feel
            c.r = BaseColor.r * Mathf.Lerp(0.5f, 1f, _current);
            c.g = BaseColor.g * Mathf.Lerp(0.5f, 1f, _current);
            c.b = BaseColor.b * Mathf.Lerp(0.5f, 1f, _current);
            _sr.color = c;
        }
    }
}
