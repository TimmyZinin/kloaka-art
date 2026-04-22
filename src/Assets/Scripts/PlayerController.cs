using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Moves the ship on the XZ plane and delegates firing to <see cref="WeaponSystem"/>.
    /// Supports:
    ///   - Keyboard (WASD/arrows) + Space/mouse-click to fire — desktop
    ///   - Touch drag-to-move + auto-fire while touching — mobile browsers
    ///   - Mouse drag (left button held) + auto-fire — also works on desktop
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 12f;
        public float xRange = 11f;
        public float zMin = -8f;
        public float zMax = 4f;
        public float tiltAmount = 25f;

        public WeaponSystem Weapon { get; private set; }

        private GameManager _manager;
        private Quaternion _baseRotation;

        // Drag-to-move (touch / mouse): remember where the ship was when the
        // press began and how far the pointer has travelled since, so motion
        // feels direct instead of snapping.
        private bool _dragging;
        private Vector2 _dragPointerStart;
        private Vector3 _dragShipStart;

        // Heuristic pixel-per-world-unit factor — tuned for 1080p landscape.
        // Gets rescaled by actual screen width so input feels similar on
        // different devices.
        private const float BasePxPerUnit = 45f;

        public void Bind(GameManager manager)
        {
            _manager = manager;
        }

        private void Start()
        {
            _baseRotation = transform.rotation;
            if (_manager == null) _manager = Object.FindAnyObjectByType<GameManager>();

            // Attach a weapon system if one isn't already present
            Weapon = gameObject.GetComponent<WeaponSystem>();
            if (Weapon == null) Weapon = gameObject.AddComponent<WeaponSystem>();
            Weapon.Init(_manager, transform);
        }

        private void Update()
        {
            if (_manager != null && (_manager.IsGameOver || _manager.IsVictory))
            {
                // On the end screen, only the keyboard shortcut restarts —
                // touches are handled by the HUD so we don't accidentally
                // restart when the player taps the CTA or reads the pitch.
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space))
                {
                    AudioManager.Play("ui_click");
                    _manager.Restart();
                }
                return;
            }

            bool firing = HandleMovementAndFireInput();

            // Visual tilt based on current horizontal movement
            float recentHoriz = Mathf.Clamp(
                (transform.position.x - _dragShipStart.x) * 0.5f, -1f, 1f);
            if (!_dragging) recentHoriz = Input.GetAxisRaw("Horizontal");

            var targetTilt = Quaternion.Euler(0f, 0f, -recentHoriz * tiltAmount) * _baseRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetTilt, 10f * Time.deltaTime);

            if (firing)
            {
                Weapon?.TryFire();
            }
        }

        /// <summary>
        /// Returns true if the fire button / touch is currently held.
        /// </summary>
        private bool HandleMovementAndFireInput()
        {
            // Prefer touch if present (mobile browsers).
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                UpdateDrag(t.position, t.phase == TouchPhase.Began,
                    t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled);
                return true;  // autofire on any active touch
            }

            // Mouse drag (also autofires — convenient for desktop playtesting).
            if (Input.GetMouseButton(0))
            {
                Vector2 mp = Input.mousePosition;
                UpdateDrag(mp, Input.GetMouseButtonDown(0), false);
                return true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _dragging = false;
            }

            // Keyboard fallback — works alongside touch / mouse.
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            var dir = new Vector3(h, 0f, v);
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.position += dir * moveSpeed * Time.deltaTime;
                ClampPosition();
            }

            return Input.GetKey(KeyCode.Space);
        }

        private void UpdateDrag(Vector2 pointer, bool began, bool ended)
        {
            if (began || !_dragging)
            {
                _dragging = true;
                _dragPointerStart = pointer;
                _dragShipStart = transform.position;
            }

            Vector2 delta = pointer - _dragPointerStart;
            float scale = Screen.width / 1080f;
            if (scale < 0.5f) scale = 0.5f;
            float pxPerUnit = BasePxPerUnit * scale;

            var target = _dragShipStart + new Vector3(
                delta.x / pxPerUnit,
                0f,
                delta.y / pxPerUnit);

            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * 1.8f * Time.deltaTime);
            ClampPosition();

            if (ended) _dragging = false;
        }

        private void ClampPosition()
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, -xRange, xRange);
            p.z = Mathf.Clamp(p.z, zMin, zMax);
            p.y = 0f;
            transform.position = p;
        }

        private void OnTriggerEnter(Collider other)
        {
            var enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.OnPlayerRammed();
                if (_manager != null) _manager.OnPlayerHit(transform.position);
            }
        }
    }
}
