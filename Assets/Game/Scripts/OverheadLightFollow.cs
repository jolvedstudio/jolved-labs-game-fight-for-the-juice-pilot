using UnityEngine;

namespace Game
{
    /// <summary>
    /// Keeps an overhead light positioned above a target (e.g. an NPC) without inheriting
    /// the target's scale/flip, so a downward spot cone stays aimed correctly.
    /// Lightweight: only updates position in LateUpdate.
    /// </summary>
    [DisallowMultipleComponent]
    public class OverheadLightFollow : MonoBehaviour
    {
        [Tooltip("The transform to hover above (e.g. the Dude).")]
        public Transform Target;

        [Tooltip("World-space offset from the target (height above the head).")]
        public Vector3 Offset = new Vector3(0f, 3.8f, 0f);

        [Tooltip("If true, only follows horizontally and keeps the initial Y height.")]
        public bool LockVerticalToStart = false;

        private float _startY;

        private void Start()
        {
            _startY = transform.position.y;
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            Vector3 p = Target.position + Offset;
            if (LockVerticalToStart) p.y = _startY;
            transform.position = p;
        }
    }
}
