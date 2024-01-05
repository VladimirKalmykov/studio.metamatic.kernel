using System;

namespace Metamatic
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    
    [Obsolete("Out of concept")]
    public abstract class ModuleDescriptor<TSlot> : ScriptableObject
        where TSlot : SlotLegacy
    {
    [SerializeField] private string moduleName;
    [SerializeField] private SpriteAssetReference previewImage;
    [SerializeField] private float price;
        
    [SerializeField] private AssetReferenceT<ModuleLegacy<TSlot>> modulePrefab;
    }
}