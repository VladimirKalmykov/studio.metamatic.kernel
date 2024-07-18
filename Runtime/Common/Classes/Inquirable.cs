namespace Metamatic
{
    public class Inquirable
    {
        public int inquireCount { get; private set; }
        public DisposableAction Inquire()
        {
            inquireCount++;
            return new DisposableAction(() => inquireCount--);
        }
    }
    public class Inquirable<T>: Inquirable
    {
        public readonly T value;
        

        public Inquirable(T value)
        {
            this.value = value;
        }
    }
}