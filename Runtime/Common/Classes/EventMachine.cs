namespace Metamatic
{
    public class EventMachine<T>
    {
        public delegate void EventDelegate<TEventArgs>(object sender, TEventArgs e);
        private event EventDelegate<T> Event;

        public void Emit(T args)
        {
            Event?.Invoke(this, args);
        }

        public System.Action Subscribe(EventDelegate<T> eventHandler)
        {
            Event += eventHandler;
            return () =>
            {
                Event -= eventHandler;
            };
        }

        public void Unsubscribe(EventDelegate<T> eventHandler)
        {
            Event -= eventHandler;
        }
    }
}