using System;

namespace Metamatic
{
    /// <summary>
    /// Objects that can be aborted. Instead of disposing, aborting assumes that the object has special logic handling the abort.
    /// In most cases aborting results in the object being disposed synchronously. But you can implement custom logic for aborting. 
    /// </summary>
    /// <remarks>
    /// You can ask why not to use IDisposable for this purpose. The answer is that IDisposable is used for disposing resources as for normal scenario as for exceptional scenario.
    /// But aborting is a special case when you need to stop the operation immediately because of some external reason. E.g. for an object to work,
    /// the presence of another object is required, which is managed by the Unity lifecycle. For some reason, Unity destroys this object. In this case, this object should be aborted,
    /// because it can't work without the required object. You can access the OnAborted event or isAborted property to determine if the object is exactly aborted, and handle it accordingly.
    ///
    /// Also, some mechanisms (like ObserveExposedDisposable) can call check if the dependency is abortable, and call Abort() instead of Dispose() that will result that the entire dependency tree
    /// is aborted instead of disposed. You can implement custom logic for aborting, e.g. to release resources immediately or to wait for some operation to finish, or to call some special logic, or
    /// call console warning, etc.
    /// </remarks>
    public interface IAbortable
    {
        bool isAborted { get; }
        event Action OnAborted;

        public void Abort();
    }
}