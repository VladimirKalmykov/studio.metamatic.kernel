using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Apply position source to the transform position
    /// </summary>
    [PluginFor(typeof(PositionSource))]
    public class ApplyPositionSource: MonoBehaviour
    {
        [SerializeField] private PositionSource positionSource;
        private void Update()
        {
            transform.position = positionSource.position;
        }
    }
}