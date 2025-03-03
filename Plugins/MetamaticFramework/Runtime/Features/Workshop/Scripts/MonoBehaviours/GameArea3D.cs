using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    [AddComponentMenu("Metamatic/Workshop/Game Area 3D")]
    public class GameArea3D: GameArea
    {
        [SerializeField] private Bounds m_Bounds;
    }
}