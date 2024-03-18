using System;

namespace Metamatic.GameAccessManagment {
    [Serializable]
    public struct Pass
    {
        public PassType type;
        public string value;
        public float count;
    }
}