using System;

namespace Metamatic
{
    public class ObjectFromPool : Releasable
    {
        private Action m_ReleaseAction;
        /// <summary>
        /// If you want to do something when object is released, set this action.
        /// </summary>
        /// <param name="releaseAction"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SetReleaseAction(Action releaseAction)
        {
            if (m_ReleaseAction != null)
            {
                throw new InvalidOperationException("Release action already set");
            }
            m_ReleaseAction = releaseAction;
        }
        public override void Release()
        {
            if (m_ReleaseAction != null)
            {
                m_ReleaseAction();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}