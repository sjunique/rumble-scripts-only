// Assets/Rumble/RpgQuest/Scripts/RunTime/Misc/PreviewTurntable.cs
using UnityEngine;

namespace RpgQuest
{
    [DisallowMultipleComponent]
    public class PreviewTurntable : MonoBehaviour
    {
        [Header("Spawn Root")]
        public Transform pivot;

        [Header("Spin")]
        public float spinSpeed = 25f;
        public bool  spin = true;

        GameObject _current;

        void Reset()
        {
            if (!pivot)
            {
                var p = new GameObject("Pivot");
                p.transform.SetParent(transform, false);
                pivot = p.transform;
            }
        }

        void Update()
        {
            if (spin && pivot) pivot.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
        }

        public void Show(GameObject prefab, Vector3 offset, float scale = 1f)
        {
            Clear();
            if (!prefab || !pivot) return;
            _current = Instantiate(prefab, pivot);
            _current.transform.localPosition = offset;
            _current.transform.localRotation = Quaternion.identity;
            _current.transform.localScale    = Vector3.one * Mathf.Max(0.0001f, scale);

            // Optional: disable colliders for preview, so they don't interfere with UI clicks
            foreach (var col in _current.GetComponentsInChildren<Collider>(true)) col.enabled = false;
        }

        public void Clear()
        {
            if (_current)
            {
                Destroy(_current);
                _current = null;
            }
        }
    }
}
