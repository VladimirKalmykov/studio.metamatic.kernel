using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic
{
    [Serializable]
    public class SpriteAssetReference: AssetReferenceT<Sprite>
    {
        public SpriteAssetReference(string guid) : base(guid)
        {
        }
    }
}