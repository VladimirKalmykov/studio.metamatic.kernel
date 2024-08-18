using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Metamatic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.EditMode.MictoMotel
{
    [TestFixture]
    public class MicroMotelTests
    {
        private class ExposedDisposable: IDisposable, IExposedDisposed
        {
            public void Dispose()
            {
                isDisposed = true;
                OnDisposed?.Invoke();
            }

            public bool isDisposed { get; private set; }
            public event Action OnDisposed;
        }   
        
        [UnityTest]
        public IEnumerator SimpleReservation() => UniTask.ToCoroutine(async () =>
        {
            var checkedIn = false;
            var checkedOut = false;
            await using var motel = new MicroMotel(4);
            var a = motel.isDisposed;
            var reservation1 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                checkedIn = true;
                await operation.cancellationToken;
                checkedOut = true;
            });
            await UniTask.Delay(100);
            Assert.IsTrue(checkedIn);
            Assert.IsFalse(checkedOut);
            await reservation1.DisposeAsync();
            Assert.IsTrue(checkedOut);
        });
        
        [UnityTest]
        public IEnumerator TwoCompetingReservations() => UniTask.ToCoroutine(async () =>
        {
            var guest1CheckedIn = false;
            var guest1CheckedOut = false;
            var guest2CheckedIn = false;
            var guest2CheckedOut = false;
            var guest2Cell = 0;
            await using var motel = new MicroMotel(4);
            var a = motel.isDisposed;
            var reservation1 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                guest1CheckedIn = true;
                await operation.cancellationToken;
                guest1CheckedOut = true;
            });
            var reservation2 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                guest2Cell = cell;
                guest2CheckedIn = true;
                await operation.cancellationToken;
                guest2CheckedOut = true;
            });
            await UniTask.Delay(100);
            Assert.IsTrue(guest1CheckedIn);
            Assert.IsFalse(guest1CheckedOut);
            Assert.IsTrue(guest2CheckedIn);
            Assert.IsFalse(guest2CheckedOut);
            Assert.AreEqual(0, guest2Cell);
        });
        
        [UnityTest]
        public IEnumerator WaitCheckOutBeforeRecheckIn() => UniTask.ToCoroutine(async () =>
        {
            var guest1CheckedIn = false;
            var guest1CheckedOut = false;
            var guest2CheckedIn = false;
            var guest2CheckedOut = false;
            var guest1CheckedOutBeforeCheckIn = false;
            var guest2Cell = 0;
            await using var motel = new MicroMotel(4);
            var a = motel.isDisposed;
            var reservation1 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                guest1CheckedIn = true;
                await operation.cancellationToken;
                guest1CheckedOut = true;
            });
            var reservation2 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                if (guest1CheckedOut)
                {
                    guest1CheckedOutBeforeCheckIn = true;
                }
                guest2Cell = cell;
                guest2CheckedIn = true;
                await operation.cancellationToken;
                guest2CheckedOut = true;
            }, strict: true);
            await UniTask.Delay(100);
            Assert.IsTrue(guest1CheckedIn);
            Assert.IsFalse(guest1CheckedOut);
            Assert.IsFalse(guest2CheckedIn);
            Assert.IsFalse(guest2CheckedOut);
            
            await reservation1.DisposeAsync();

            Assert.AreEqual(1, guest2Cell);
            Assert.IsTrue(guest1CheckedOutBeforeCheckIn);
        });
        
        [UnityTest]
        public IEnumerator SimpleReservationWithDependencies() => UniTask.ToCoroutine(async () =>
        {
            var checkedIn = false;
            var checkedOut = false;
            var disposable = new ExposedDisposable();
            await using var motel = new MicroMotel(4);
            var a = motel.isDisposed;
            var reservation1 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                checkedIn = true;
                await operation.cancellationToken;
                checkedOut = true;
            }, dependencies: new object[] { disposable });
            await UniTask.Delay(100);
            Assert.IsTrue(checkedIn);
            Assert.IsFalse(checkedOut, "Must be not checkout before disposable is disposed");
            disposable.Dispose();
            await UniTask.Delay(100);
            Assert.IsTrue(checkedOut, "Must be checkout after disposable is disposed");
        });
    }
}