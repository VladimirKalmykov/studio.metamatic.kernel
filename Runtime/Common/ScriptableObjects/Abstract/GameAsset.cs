using System.Collections.Generic;
using Metamatic.GameAccessManagment;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Serialization;

namespace Metamatic
{
    public enum AssetRarityGrade
    {
        Auto,
        Junk,
        Regular,
        Legendary,
        Epic,
        Arcane
    }
    public interface IGameAsset
    {
        public LocalizedString GetTitle();
        public LocalizedString GetDescription();
        public AssetRarityGrade grade { get; }
        public AssetReferenceSprite previewImage { get; }
    }
    public abstract class GameAsset: ScriptableObject, IAccessable, IGameAsset, ISerializationCallbackReceiver
    {
        void ISerializationCallbackReceiver.OnAfterDeserialize() {

        }
        void ISerializationCallbackReceiver.OnBeforeSerialize() {
#if UNITY_EDITOR
            if (id == null || id == ""){
                id = System.Guid.NewGuid().ToString();
            }
        }
#endif
        [ReadOnly]
        public string id = "";
        
        [SerializeField]private LocalizedString title;
        [SerializeField]private LocalizedString description;
        [SerializeField][LabelText("Preview")] private AssetReferenceSprite sPreviewImage;
        [SerializeField][LabelText("Rarity")] private AssetRarityGrade sGrade;
        [BoxGroup("Access Config"), PropertyOrder(99)]
        public GameAccessConfig accessConfig;
        public GameAccessConfig GetAccessConfig()
        {
            return accessConfig;
        }

        public LocalizedString GetTitle()
        {
            return title;
        }

        public LocalizedString GetDescription()
        {
            return description;
        }

        public AssetRarityGrade grade => sGrade;

       
        public AssetReferenceSprite previewImage => sPreviewImage;
    }
    
    public abstract class SalableGameAsset: GameAsset, IProduct
    {
        [BoxGroup("Product")]
        public float price = 0f;
        float IProduct.GetPrice() {
            return price;
        }
        string IProduct.GetProductName() {
            return name;
        }
    }
}