using System;
using UnityEngine;

namespace NetLib.UI.Hololens
{
    /// <summary>
    /// Automatically resizes an attached collider or attaches a new collider when none is present.
    /// Used for providing world space colliders for ui elements. 
    /// </summary>
    public class ColliderResizer : MonoBehaviour
    {
        private new BoxCollider collider;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            collider = GetComponent<BoxCollider>();
            if (collider == null)
                collider = gameObject.AddComponent<BoxCollider>();
        }

        private void Update()
        {
            var v = new Vector3[4];

            rectTransform.GetLocalCorners(v);

            float width = Math.Abs(v[2].x - v[0].x);
            float height = Math.Abs(v[1].y - v[0].y);

            collider.size = new Vector3(width, height, 0.001f);
        }
    }
}
