namespace DarkMatter.GameAccessManagment {
    public struct AccessDetails {
        public bool own;
        public bool view;
        public bool interact;
        public bool use;
        public bool isCertified;
        public GameAccessConfig config;
        public static AccessDetails None {
            get { return new AccessDetails(); }
        }
    }
}