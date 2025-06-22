using System;

namespace Metamatic
{
    public abstract class Commit
    {
        public readonly string RevisionId = Guid.NewGuid().ToString();
        public static readonly Commit Empty = new EmptyCommit();
        public abstract void Apply();
        public abstract void Revert();
    }
    
    public class EmptyCommit: Commit
    {
        public override void Apply()
        {
        }

        public override void Revert()
        {
        }
    }

    public class RevertCommit : Commit
    {
        private readonly Action m_RevertAction;
        
        public RevertCommit(Action revertAction)
        {
            m_RevertAction = revertAction;
        }

        public override void Apply()
        {
            Console.Exception("Commit", "RevertCommit can not be applied");
        }

        public override void Revert()
        {
            m_RevertAction();
        }
    }
    
    public class CommitComposition : Commit
    {
        private readonly Commit[] m_Commits;
        
        public CommitComposition(params Commit[] commits)
        {
            m_Commits = commits;
        }
        
        public override void Apply()
        {
            foreach (var commit in m_Commits)
            {
                commit.Apply();
            }
        }
        
        public override void Revert()
        {
            for (var i = m_Commits.Length - 1; i >= 0; i--)
            {
                m_Commits[i].Revert();
            }
        }
    }
}