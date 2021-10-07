namespace Metamatic.GameAccessManagment {
    using UnityEngine;

    [CreateAssetMenu(fileName = "CertificatesCondition", menuName = "Dark Matter/Conditions/CertificatesCondition", order = 0)]
    /// <summary>
    /// Character must have pass MinLevelPass and its level must be greater or equal to minLevel
    /// </summary>
    public class CertificatesCondition: ScriptableCondition, ICondition {
        public Certificate[] certificates = new Certificate[0];
        public override bool Verify(Pass[] passes) {
            if (passes == null) {
                return false;
            }
            foreach(CertificatePass pass in passes) {
                if (pass == null) {
                    continue;
                }
                foreach(var requiredCertificate in certificates) {
                    if (pass.certificate == requiredCertificate) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}