using Cysharp.Threading.Tasks;
using Metamatic.Systems;
using UnityEditor;
using UnityEngine;

namespace Metamatic.SocketSystem
{
    /// <summary>
    /// Host is a mono behaviour that can hold sockets.
    /// </summary>
    public class Host: MonoBehaviour
    {
        private SignedIndexedHashPool<SignalSource> m_UnmountSignals = new SignedIndexedHashPool<SignalSource>(10);
        /// <summary>
        /// Mount a module from a blueprint into the first available slot that fits the module's form factor.
        /// </summary>
        /// <param name="blueprint"></param>
        /// <param name="ignoreMissingSlot"></param>
        /// <param name="cancellationSignal"></param>
        /// <exception cref="Exception"></exception>
        public AsyncSummon<Module> Mount(ModuleBlueprint blueprint)
        {
            var signalSource = SignalSource.Create();
            var token = m_UnmountSignals.Add(signalSource);
            
            return new AsyncSummon<Module>(token, InternalMount(blueprint, signalSource.Signal), Unmount);
        }
        
        private async UniTask<Module> InternalMount(ModuleBlueprint blueprint, Signal cancellationSignal = default)
        {
            var formFactor = blueprint.formFactor;
            // Find a slot that fits the form factor
            var slots = Slot.GetSlots(this.gameObject, formFactor);
            var firstFreeSlot = System.Array.Find(slots, s => s.IsFree());
            if (firstFreeSlot == null)
            {
                Console.Error("Host", 
                    $"No slot found for form factor {formFactor} in host {name}, but ignoring as requested.");
                return null;
            }

            using var moduleSummon = GameObjectFactory.SummonFromPrefab<Module>(blueprint.prefab);
            var module = await moduleSummon.Wait(cancellationSignal);
            module.transform.SetParent(firstFreeSlot.transform, false);
            module.gameObject.SetActive(true);

            return module;
        }
        
        private void Unmount(Token96Bit token)
        {
            if (m_UnmountSignals.Remove(token, out var signalSource))
            {
                signalSource.Dispose();
            }
            else
            {
                Console.Error("Host", $"No unmount signal found for token {token}, cannot unmount.");
            }
        }
    }
}