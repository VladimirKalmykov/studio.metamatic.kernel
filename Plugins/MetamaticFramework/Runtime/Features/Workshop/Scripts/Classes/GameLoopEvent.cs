namespace Plugins.MetamaticFramework.Runtime.Features.Workshop.Scripts.Classes
{
    public abstract class GameLoopEvent
    {
        public static readonly IdleGameLoopEvent IdleGameLoopEvent = new IdleGameLoopEvent();
        public static readonly MenuGameLoopEvent MenuGameLoopEvent = new MenuGameLoopEvent();
        public static readonly WinGameLoopEvent WinGameLoopEvent = new WinGameLoopEvent();
    }
    
    public class IdleGameLoopEvent : GameLoopEvent
    {
        
    }
    
    public class MenuGameLoopEvent : GameLoopEvent
    {
        
    }
    
    public class LaunchGameLoopEvent : GameLoopEvent
    {
        public readonly string PlaceAddress;
        public readonly bool Seamless;
        
        public LaunchGameLoopEvent(string placeAddress, bool seamless)
        {
            this.PlaceAddress = placeAddress;
            this.Seamless = seamless;
        }
    }

    public class LoadGameLoopEvent : GameLoopEvent
    {
        public readonly string saveAddress;
        public readonly bool seamless;
        
        public LoadGameLoopEvent(string saveAddress, bool seamless)
        {
            this.saveAddress = saveAddress;
            this.seamless = seamless;
        }
    }
    
    public class WinGameLoopEvent : GameLoopEvent
    {
        
    }
}