using System.Collections.Generic;

namespace Metamatic
{
    public class CommitsGraph
    {
        private readonly HashSet<string> m_CommitRevisions = new HashSet<string>();
        private readonly Dictionary<string, Commit> m_CommitsById = new Dictionary<string, Commit>();
        
        public string Add(Commit commit, bool skipApply = false)
        {
            if (m_CommitRevisions.Contains(commit.RevisionId))
            {
                return "0";
            }

            if (!skipApply)
            {
                commit.Apply();
            }
            m_CommitRevisions.Add(commit.RevisionId);
            m_CommitsById[commit.RevisionId] = commit;

            return commit.RevisionId;
        }
        
        public bool Revert(string revisionId)
        {
            if (!m_CommitRevisions.Contains(revisionId))
            {
                return false;
            }
            var commit = m_CommitsById[revisionId];
            commit.Revert();
            m_CommitRevisions.Remove(revisionId);
            m_CommitsById.Remove(revisionId);
            
            return true;
        }

        public bool Revert(IEnumerable<string> revisionIds)
        {
            bool success = true;
            foreach (var revisionId in revisionIds)
            {
                success &= Revert(revisionId);
            }
            return success;
        }
    }
}