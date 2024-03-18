namespace Metamatic.GameAccessManagment {
    public struct AccessDetails {
        public bool use;
        public bool interact;
        public bool view;
        
        public GameAccessConfig config;
        public static AccessDetails None {
            get { return new AccessDetails(); }
        }
    }
}