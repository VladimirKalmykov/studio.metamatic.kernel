namespace Metamatic
{
    public class ModernSocket<TMother, TFather>: IModernSocket<TMother, TFather>
    {
        public DisposableAction ConnectPlug(IModernPlug<TMother, TFather> plug)
        {
            throw new System.NotImplementedException();
        }

        void IModernSocket<TMother, TFather>.OnPlugConnected(IModernPlug<TMother, TFather> plug)
        {
            throw new System.NotImplementedException();
        }
    }
}