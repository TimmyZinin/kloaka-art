using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// MonoBehaviour wrapper around <see cref="GameBootstrap.Boot"/>.
    /// Attached to an empty GameObject in the scene so that Awake fires
    /// reliably in batch-built WebGL players — more robust than
    /// <c>[RuntimeInitializeOnLoadMethod]</c> when the project is upgraded
    /// to a new Unity version on first build.
    /// </summary>
    public class BootstrapRunner : MonoBehaviour
    {
        private void Awake()
        {
            // Guard against accidental double-invocation if the static
            // bootstrap already ran earlier in the frame.
            if (FindAnyObjectByType<GameManager>() != null) return;
            GameBootstrap.BootFromRunner();
        }
    }
}
