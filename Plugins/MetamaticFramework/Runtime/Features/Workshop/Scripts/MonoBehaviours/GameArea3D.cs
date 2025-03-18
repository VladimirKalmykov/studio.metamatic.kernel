using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    [AddComponentMenu("Metamatic/Workshop/Game Area 3D")]
    public class GameArea3D: GameArea
    {
        [SerializeField] private Bounds m_Bounds;
        public override bool IsIntersected(Vector3 position)
        {
            throw new System.NotImplementedException();
        }
    }
}