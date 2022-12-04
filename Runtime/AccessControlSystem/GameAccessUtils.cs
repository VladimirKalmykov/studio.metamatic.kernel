namespace DarkMatter.GameAccessManagment {
    public class GameAccessUtils {
        public static AccessDetails GetAccess(IAccessable accessable, Pass[] passes) {
            if (accessable == null) {
                return AccessDetails.None;
            }
            var accessConfig = accessable.GetAccessConfig();
            /* Check sertificates */
            bool own = accessConfig.certify ? ValidateCertificate(accessConfig.certificate, passes) : false;
            var use = !accessConfig.certify || own;
            var interact = own || (accessConfig.interact == null || accessConfig.interact.Verify(passes));
            var view = own || (accessConfig.view == null || accessConfig.view.Verify(passes));

            var accessDetails = new AccessDetails {
                own = own,
                use = use,
                interact = interact,
                config = accessConfig,
                isCertified = accessConfig.certify && accessConfig.certificate != null
            };

            return accessDetails;
        }

        static public bool ValidateCertificate(Certificate certificate, Pass[] passes) {
            if (certificate != null) {
                foreach(CertificatePass pass in passes) {
                    if (pass == null) {
                        continue;
                    }
                    
                    if (pass.certificate == certificate) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}