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

        public enum ReserveStage
        {
            Waiting,
            CheckedIn,
            CheckingOut,
            CheckedOut,
            Abort
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
        
        [UnityTest]
        public IEnumerator DisposeAsyncAlreadyDisposed() => UniTask.ToCoroutine(async () =>
        {
            var reserveStage = ReserveStage.Waiting;
            await using var motel = new MicroMotel(4);
            var a = motel.isDisposed;
            var reservation1 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                await operation.cancellationToken;
            });
            var reservation2 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                try
                {
                    reserveStage = ReserveStage.CheckedIn;
                    await operation.cancellationToken;
                }
                catch (OperationCanceledException e)
                {
                    // Skip
                }
                finally
                {
                    reserveStage = ReserveStage.CheckingOut;
                    if (!operation.isAborted)
                    {
                        await UniTask.Delay(1000);
                        reserveStage = ReserveStage.CheckedOut;
                    }
                }
            });
            await UniTask.Delay(100);
            Assert.AreEqual(ReserveStage.CheckedIn, reserveStage);
            await reservation1.DisposeAsync();
            Assert.AreEqual(ReserveStage.CheckingOut, reserveStage);
            await reservation2.DisposeAsync();
            await UniTask.Delay(100);
            Assert.AreEqual(ReserveStage.CheckedOut, reserveStage);
        });

        [UnityTest]
        public IEnumerator DisposeMotelShouldAbortAllGuests() => UniTask.ToCoroutine(async () =>
        {
            var exposedDisposable = new ExposedDisposable();
            var reserveStage1 = ReserveStage.Waiting;
            var reserveStage2 = ReserveStage.Waiting;
            await using var motel = new MicroMotel(4, dependencies: new object[] { exposedDisposable });
            var reservation1 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                try
                {
                    reserveStage1 = ReserveStage.CheckedIn;
                    await operation.cancellationToken;
                }
                catch (OperationCanceledException e)
                {
                    // Skip
                }
                finally
                {
                    reserveStage1 = ReserveStage.CheckingOut;
                    if (!operation.isAborted)
                    {
                        await UniTask.Delay(1000);
                        reserveStage1 = ReserveStage.CheckedOut;
                    }
                    else
                    {
                        reserveStage1 = ReserveStage.Abort;
                    }
                }
            });
            var reservation2 = motel.Reserve(new int[] { 2 }, async (cell, operation) =>
            {
                try
                {
                    reserveStage2 = ReserveStage.CheckedIn;
                    await operation.cancellationToken;
                }
                catch (OperationCanceledException e)
                {
                    // Skip
                }
                finally
                {
                    reserveStage2 = ReserveStage.CheckingOut;
                    if (!operation.isAborted)
                    {
                        await UniTask.Delay(1000);
                        reserveStage2 = ReserveStage.CheckedOut;
                    }
                    else
                    {
                        reserveStage2 = ReserveStage.Abort;
                    }
                }
            });
            await UniTask.Delay(100);
            Assert.AreEqual(ReserveStage.CheckedIn, reserveStage1);
            Assert.AreEqual(ReserveStage.CheckedIn, reserveStage2);
            exposedDisposable.Dispose();
            Assert.AreEqual(ReserveStage.Abort, reserveStage1);
            Assert.AreEqual(ReserveStage.Abort, reserveStage2);
        });
        
        [UnityTest]
        public IEnumerator AlternatePreferredCell() => UniTask.ToCoroutine(async () =>
        {
            var guest1Cell = -1;
            var guest2Cell = -1;
            var guest3Cell = -1;
            var guest4Cell = -1;
            await using var motel = new MicroMotel(8, cellCapacity: 1);
            var reservation1 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                guest1Cell = cell;
                await operation.cancellationToken;
            });
            var reservation2 = motel.Reserve(new int[] { 1, 5 }, async (cell, operation) =>
            {
                guest2Cell = cell;
                await operation.cancellationToken;
            });
            var reservation3 = motel.Reserve(new int[] { 5, 3 }, async (cell, operation) =>
            {
                guest3Cell = cell;
                await operation.cancellationToken;
            });
            var reservation4 = motel.Reserve(new int[] { 3, 7 }, async (cell, operation) =>
            {
                guest4Cell = cell;
                await operation.cancellationToken;
            });
            Assert.AreEqual(1, guest1Cell);
            Assert.AreEqual(5, guest2Cell);
            Assert.AreEqual(3, guest3Cell);
            Assert.AreEqual(7, guest4Cell);
        });
        
        [UnityTest]
        public IEnumerator MultipleGuestAtTheSameCell() => UniTask.ToCoroutine(async () =>
        {
            var exposedDisposable = new ExposedDisposable();
            var guest1Cell = -1;
            var guest2Cell = -1;
            var guest3Cell = -1;
            var guest4Cell = -1;
            await using var motel = new MicroMotel(4, cellCapacity: 2, dependencies: new object[] { exposedDisposable });
            var reservation1 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                guest1Cell = cell;
                await operation.cancellationToken;
            });
            var reservation2 = motel.Reserve(new int[] { 1 }, async (cell, operation) =>
            {
                guest2Cell = cell;
                await operation.cancellationToken;
            });
            var reservation3 = motel.Reserve(new int[] { 2 }, async (cell, operation) =>
            {
                guest3Cell = cell;
                await operation.cancellationToken;
            });
            var reservation4 = motel.Reserve(new int[] { 2 }, async (cell, operation) =>
            {
                guest4Cell = cell;
                await operation.cancellationToken;
            });
            Assert.AreEqual(1, guest1Cell);
            Assert.AreEqual(1, guest2Cell);
            Assert.AreEqual(2, guest3Cell);
            Assert.AreEqual(2, guest4Cell);
        });
    }
}