using System;

namespace DarkMatter.GameAccessManagment {
    [Serializable]
    /// <summary>
    /// Provide character with some code (no matter what it is)
    /// </summary>
    public class CertificatePass: Pass {
        public Certificate certificate;
    }
}