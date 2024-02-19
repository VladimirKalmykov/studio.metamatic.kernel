namespace Metamatic
{
    public interface IMigratable<T>
    {
        public abstract T Migrate(string version, object data);
    }
}