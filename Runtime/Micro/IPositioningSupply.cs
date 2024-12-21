using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public interface IPositioningSupply: IUniTaskAsyncDisposable
    {
        public (Vector3 position, Vector2 anchor) GetPositionAndAnchor();
    }
}