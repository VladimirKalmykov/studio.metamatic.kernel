using System;

namespace Metamatic.GameAccessManagment {
    /// <summary>
    /// Provide character with some code (no matter what it is)
    /// </summary>
    [Serializable]
    public class CodePass: Pass {
        public string code = "";
    }
}