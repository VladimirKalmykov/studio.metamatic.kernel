using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework.Workshop
{
    public class PlacePortal: MonoBehaviour
    {
        public enum PlacementType
        {
            /// <summary>
            /// Move to default checkpoint
            /// </summary>
            Default,
            /// <summary>
            /// Move to specific checkpoint (must be set in the inspector)
            /// </summary>
            SpecificCheckpoint,
            /// <summary>
            /// Move to specific position (must be set in the inspector)
            /// </summary>
            FreePosition
        }
        [SerializeField] private AssetReferenceT<Place> place;
        [SerializeField] private PlacementType placementType;
        [ShowIf("@placementType == PlacementType.SpecificCheckpoint")]
        [SerializeField] private string checkpointId;
        [ShowIf("@placementType == PlacementType.FreePosition")]
        [SerializeField] private Vector3 freePosition;

        public void Travel(Entity entity)
        {
            InternalTravelTo(entity).Forget();
        }
        
        private async UniTaskVoid InternalTravelTo(Entity entity)
        {
            try
            {
                var gameService = await Service.Resolve<GameService>();
            
                switch (placementType)
                {
                    case PlacementType.Default:
                        // Move the entity to the default checkpoint
                        VirtualWorld.Current.MoveEntityToPlace(entity.id, place.AssetGUID);
                        break;
                    case PlacementType.SpecificCheckpoint:
                        // Move the entity to the specific checkpoint
                        VirtualWorld.Current.MoveEntityToPlace(entity.id, place.AssetGUID, checkpointId);
                        break;
                    case PlacementType.FreePosition:
                        // Move the entity to the free position
                        VirtualWorld.Current.MoveEntityToPlace(entity.id, place.AssetGUID, freePosition);
                        break;
                }
            } catch (Exception e)
            {
                Console.Error("PlacePortal", $"Error while moving entity {entity.id} to place {place.AssetGUID}: {e}");
            }
        }
    }
}