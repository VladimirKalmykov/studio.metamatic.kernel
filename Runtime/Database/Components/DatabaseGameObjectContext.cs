using UnityEngine;

namespace Metamatic
{
    public class DatabaseGameObjectContext : MonoBehaviour
    {
        public DatabaseBranchSource branchSource = new DatabaseBranchSource(Database.masterBranchName);
    }
}