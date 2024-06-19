namespace Metamatic
{
    public class Logger
    {
        public static void Log(string message)
        {
            #if METAMATIC_TERMINAL
            Terminal.Log(message);
            #endif
        }
    }
}