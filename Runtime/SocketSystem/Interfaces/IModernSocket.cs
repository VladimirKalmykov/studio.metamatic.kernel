namespace Metamatic
{
    public interface IModernSocket
    {
        
    }
    
    public interface IModernSocket<TMother, TFather>: IModernSocket
    {
        public DisposableAction ConnectPlug(IModernPlug<TMother, TFather> plug);
        protected void OnPlugConnected(IModernPlug<TMother, TFather> plug);
    }
}