using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic
{
    [Serializable]
    [Obsolete("Use AssetReferenceSprite instead", true)]
    public class SpriteAssetReference: AssetReferenceT<Sprite>
    {
        public SpriteAssetReference(string guid) : base(guid)
        {
        }
    }
}