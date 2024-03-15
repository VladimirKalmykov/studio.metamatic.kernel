using System;

namespace Metamatic.GameAccessManagment {
    /// <summary>
    /// Provide character with some code (no matter what it is)
    /// </summary>
    [Serializable]
    public class OwnAssetPass: Pass {
        public readonly string vendorCode;

        public OwnAssetPass(string vendorCode)
        {
            this.vendorCode = vendorCode;
        }
    }
}