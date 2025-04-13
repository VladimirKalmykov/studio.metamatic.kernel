using System;
using Cysharp.Threading.Tasks;

namespace Metamatic.Framework.Workshop
{
    public readonly struct EntityDescriptor
    {
        public readonly VirtualWorld world;
        public readonly VirtualPlace place;
        public readonly string id;
        public readonly bool hibernated;
        public readonly bool materialized;
        public readonly Entity.SaveData data;
        
        public EntityDescriptor(VirtualWorld world, VirtualPlace place, string id, bool hibernated, bool materialized, Entity.SaveData data)
        {
            this.world = world;
            this.place = place;
            this.id = id;
            this.hibernated = hibernated;
            this.materialized = materialized;
            this.data = data;
        }

        public void Release()
        {
            place.ReleaseEntity(id, DismissReason.Kick);
        }
    }
}