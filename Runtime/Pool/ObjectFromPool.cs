namespace Metamatic
{
    public class ObjectFromPool : Releasable
    {
        public override void Release()
        {
            gameObject.SetActive(false);
        }
    }
}