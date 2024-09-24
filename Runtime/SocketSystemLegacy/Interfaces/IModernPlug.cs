namespace Metamatic
{
    public interface IModernPlug<TMother, TFather>
    {
        public DisposableAction ConnectSocket(IModernSocket<TMother, TFather> socket);
    }
}