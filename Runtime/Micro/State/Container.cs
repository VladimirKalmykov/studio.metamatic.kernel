namespace Metamatic
{
    public class Container<T>
    {
        public T value { get; set; }

        public Container()
        {
            
        }
        
        public Container(T value)
        {
            this.value = value;
        }
    }
}