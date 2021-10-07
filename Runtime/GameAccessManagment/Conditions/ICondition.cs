namespace Metamatic.GameAccessManagment {
    public interface ICondition {
        public bool Verify(Pass[] passes);
    }
}