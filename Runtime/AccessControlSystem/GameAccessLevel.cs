namespace DarkMatter.GameAccessManagment {
    public enum GameAccessLevel {
        /* Target is not permitted for the character */
        DISALLOWED = 0,
        /* Target is locked. Character can known about it, but it is locked */
        LOCKED = 1,
        /* Character can view, buy, iterate with target */
        INTERACTABLE = 2,
        /* Character can have target (use for free) */
        USABLE = 3,
        /* Character can customize object */
        CUSTOMIZABLE= 4
    }
}