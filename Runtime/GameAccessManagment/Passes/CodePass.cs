using System;

namespace Metamatic.GameAccessManagment {
    [Serializable]
    /// <summary>
    /// Provide character with some code (no matter what it is)
    /// </summary>
    public class CodePass: Pass {
        public string code = "";
    }
}