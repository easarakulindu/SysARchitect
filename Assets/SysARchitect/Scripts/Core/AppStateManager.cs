using System;
using UnityEngine;

namespace SysARchitect.Core
{
    /// <summary>
    /// Central application state manager. Tracks the current mode and
    /// broadcasts mode-change events to all listeners.
    /// </summary>
    public class AppStateManager : MonoBehaviour
    {
        public static AppStateManager Instance { get; private set; }

        [SerializeField] private AppMode initialMode = AppMode.Build;

        private AppMode currentMode;

        /// <summary>Current application mode.</summary>
        public AppMode CurrentMode => currentMode;

        /// <summary>Fired when the application mode changes. Arg: new mode.</summary>
        public event Action<AppMode> OnModeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            currentMode = initialMode;
        }

        /// <summary>
        /// Switch to a new application mode.
        /// </summary>
        public void SetMode(AppMode mode)
        {
            if (currentMode == mode) return;
            currentMode = mode;
            OnModeChanged?.Invoke(currentMode);
        }
    }
}
