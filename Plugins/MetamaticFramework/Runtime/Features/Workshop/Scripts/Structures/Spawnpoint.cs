using System;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    [Serializable]
    public sealed class Spawnpoint
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
    }
}