using System;

namespace DarkMatter.GameAccessManagment {
    /// <summary>
    /// Every time user increases his level it should be saved here
    /// </summary>
    [Serializable]
    public class LevelPass: Pass {
        public int level;
    }
}