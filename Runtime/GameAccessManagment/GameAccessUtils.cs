using System;

namespace Metamatic.GameAccessManagment {
    using System.Linq;
    public class GameAccessUtils {
        public static AccessDetails GetAccess(UnityEngine.Object target, IAccessable accessable, Passes passes) {
            if (accessable == null) {
                return AccessDetails.None;
            }
            var accessConfig = accessable.GetAccessConfig();
            /* Check sertificates */
            
            var use = (!accessConfig.use.enabled || accessConfig.use.Value == null || accessConfig.use.Value.Verify(target, passes));
            var interact = use || (!accessConfig.interact.enabled || accessConfig.interact.Value == null || accessConfig.interact.Value.Verify(target, passes));
            var view = interact || (!accessConfig.view.enabled || accessConfig.view.Value == null || accessConfig.view.Value.Verify(target, passes));

            var accessDetails = new AccessDetails {
                use = use,
                interact = interact,
                view = view,
                config = accessConfig
            };

            return accessDetails;
        }

        static public bool ValidateCertificate(Certificate certificate, Pass[] passes)
        {
            return false;
        }
    }
}