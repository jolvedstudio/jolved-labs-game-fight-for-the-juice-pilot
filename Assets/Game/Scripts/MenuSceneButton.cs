using UnityEngine;
using MoreMountains.Tools;

namespace LavaRun
{
    /// <summary>
    /// Minimal menu helper: loads a target scene (through the Corgi/MMTools loading screen)
    /// when its public Load() method is invoked, typically from a UI Button OnClick.
    /// Also supports auto-loading after a delay (handy for Win screens).
    /// </summary>
    public class MenuSceneButton : MonoBehaviour
    {
        [Tooltip("Exact name of the scene to load (must be in Build Settings).")]
        public string SceneToLoad = "Lava";

        [Tooltip("Name of the loading screen scene to use.")]
        public string LoadingSceneName = "LoadingScreen";

        [Tooltip("If greater than 0, the scene auto-loads after this many seconds.")]
        public float AutoLoadDelay = 0f;

        protected virtual void Start()
        {
            if (AutoLoadDelay > 0f)
            {
                Invoke(nameof(Load), AutoLoadDelay);
            }
        }

        /// <summary>
        /// Loads the configured scene. Wire this to a UI Button's OnClick.
        /// </summary>
        public virtual void Load()
        {
            if (string.IsNullOrEmpty(SceneToLoad))
            {
                Debug.LogWarning("[MenuSceneButton] SceneToLoad is empty.");
                return;
            }
            MMSceneLoadingManager.LoadScene(SceneToLoad, LoadingSceneName);
        }

        /// <summary>
        /// Loads a specific scene by name (overrides the inspector value).
        /// </summary>
        public virtual void Load(string sceneName)
        {
            MMSceneLoadingManager.LoadScene(sceneName, LoadingSceneName);
        }
    }
}
