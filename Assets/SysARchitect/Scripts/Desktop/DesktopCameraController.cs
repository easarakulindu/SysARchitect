namespace SysARchitect.Desktop
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// Orbit/pan/zoom camera controller for the desktop workspace.
    /// Orbits around a focus point (the tabletop centre) using spherical coordinates.
    /// <para>
    /// Controls:
    /// <list type="bullet">
    /// <item>Right mouse button — orbit (yaw / pitch)</item>
    /// <item>Middle mouse button — pan</item>
    /// <item>Scroll wheel — zoom</item>
    /// </list>
    /// </para>
    /// </summary>
    public class DesktopCameraController : MonoBehaviour
    {
        [Header("Orbit")]
        [SerializeField] private float orbitSpeed = 120f;
        [SerializeField] private float minPitch = 10f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minDistance = 0.5f;
        [SerializeField] private float maxDistance = 5f;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 0.5f;

        [Header("Smoothing")]
        [SerializeField] private float smoothTime = 0.08f;

        private float yaw;
        private float pitch = 45f;
        private float distance = 2f;
        private Vector3 focusPoint = Vector3.zero;

        // Smoothing velocity trackers (used by SmoothDamp).
        private float yawVelocity;
        private float pitchVelocity;
        private float distanceVelocity;
        private Vector3 focusVelocity;

        // Smoothed values applied each frame.
        private float smoothYaw;
        private float smoothPitch = 45f;
        private float smoothDistance = 2f;
        private Vector3 smoothFocus = Vector3.zero;

        void Start()
        {
            smoothYaw = yaw;
            smoothPitch = pitch;
            smoothDistance = distance;
            smoothFocus = focusPoint;
            ApplyCameraTransform();
        }

        void LateUpdate()
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                HandleOrbit();
                HandlePan();
                HandleZoom();
            }

            // Smoothly interpolate towards target values.
            smoothYaw = Mathf.SmoothDamp(smoothYaw, yaw, ref yawVelocity, smoothTime);
            smoothPitch = Mathf.SmoothDamp(smoothPitch, pitch, ref pitchVelocity, smoothTime);
            smoothDistance = Mathf.SmoothDamp(smoothDistance, distance, ref distanceVelocity, smoothTime);
            smoothFocus = Vector3.SmoothDamp(smoothFocus, focusPoint, ref focusVelocity, smoothTime);

            ApplyCameraTransform();
        }

        /// <summary>
        /// Immediately set the focus point the camera orbits around.
        /// </summary>
        public void SetFocusPoint(Vector3 point)
        {
            focusPoint = point;
        }

        /// <summary>
        /// Reset orbit to default viewing angle.
        /// </summary>
        public void ResetView()
        {
            yaw = 0f;
            pitch = 45f;
            distance = 2f;
            focusPoint = Vector3.zero;
        }

        private void HandleOrbit()
        {
            // Right mouse button held.
            if (!Input.GetMouseButton(1)) return;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * orbitSpeed * Time.unscaledDeltaTime;
            pitch -= mouseY * orbitSpeed * Time.unscaledDeltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private void HandlePan()
        {
            // Middle mouse button held.
            if (!Input.GetMouseButton(2)) return;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Pan in camera-local right/up directions.
            Transform t = transform;
            Vector3 right = t.right;
            Vector3 up = t.up;

            focusPoint -= (right * mouseX + up * mouseY) * panSpeed;
        }

        private void HandleZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f)) return;

            distance -= scroll * zoomSpeed * Time.unscaledDeltaTime;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        private void ApplyCameraTransform()
        {
            // Convert spherical coordinates to Cartesian offset.
            float pitchRad = smoothPitch * Mathf.Deg2Rad;
            float yawRad = smoothYaw * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            ) * smoothDistance;

            transform.position = smoothFocus + offset;
            transform.LookAt(smoothFocus);
        }
    }
}
