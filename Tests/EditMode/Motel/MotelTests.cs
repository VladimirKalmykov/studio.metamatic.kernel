using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Metamatic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.EditMode
{
    [TestFixture]
    public class MotelTests
    {
        [Flags]
        private enum MotelRoom
        {
            None = 0,
            Room1 = 1 << 1,
            Room2 = 1 << 2,
            Room3 = 1 << 3,
            Room4 = 1 << 4,
            Room5 = 1 << 5,
            Room6 = 1 << 6,
            Room7 = 1 << 7,
        }

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
            await using var motel = new Motel<MotelRoom>();
            await using var reservation1 = motel.Reserve(MotelRoom.Room1, async (room, reservation) =>
            {
                await UniTask.Delay(100);
            });
            Assert.AreEqual(MotelRoom.Room1, reservation1.room, "Reservation 1 should be in Room 1");
            Assert.IsTrue(!motel.IsFree(MotelRoom.Room1));
            Assert.IsTrue(motel.IsFree(MotelRoom.Room2));
        });
        
        [UnityTest]
        public IEnumerator SimpleReservationWithStrictPreference() => UniTask.ToCoroutine(async () =>
        {
            await using var motel = new Motel<MotelRoom>();
            await using var reservation1 = motel.Reserve(MotelRoom.Room1, async (room, reservation) =>
            {
                await UniTask.Delay(100);
            }, strict: true);
            Assert.AreEqual(MotelRoom.Room1, reservation1.room, "Reservation 1 should be in Room 1");
            Assert.IsTrue(!motel.IsFree(MotelRoom.Room1));
            Assert.IsTrue(motel.IsFree(MotelRoom.Room2));
            await using var reservation2 = motel.Reserve(MotelRoom.Room1, async (room, reservation) =>
            {
                await UniTask.Delay(100);
            }, strict: true);
            Assert.IsTrue(!motel.IsFree(MotelRoom.Room1));
            Assert.AreEqual(MotelRoom.None, reservation2.room, "Reservation 2 should be out of rooms");
        });
        
        [UnityTest]
        public IEnumerator NonStrictReservation() => UniTask.ToCoroutine(async () =>
        {
            await using var motel = new Motel<MotelRoom>();
            await using var reservation1 = motel.Reserve(MotelRoom.Room1, async (room, reservation) =>
            {
                await UniTask.Delay(100);
            });
            Assert.AreEqual(MotelRoom.Room1, reservation1.room, "Reservation 1 should be in Room 1");
            Assert.IsTrue(!motel.IsFree(MotelRoom.Room1));
            Assert.IsTrue(motel.IsFree(MotelRoom.Room2));
            await using var reservation2 = motel.Reserve(MotelRoom.Room1, async (room, reservation) =>
            {
                await UniTask.Delay(100);
            });
            Assert.AreEqual(MotelRoom.Room2, reservation2.room, "Reservation 2 should be in Room 2");
            Assert.IsTrue(!motel.IsFree(MotelRoom.Room2));
            Assert.IsTrue(motel.IsFree(MotelRoom.Room3));
        });
        
        [UnityTest]
        public IEnumerator AbortReservation() => UniTask.ToCoroutine(async () =>
        {
            var exposedDisposable = new ExposedDisposable();
            bool reservationCancelled = false;
            bool reservationAborted = false;
            await using var motel = new Motel<MotelRoom>();
            await using var reservation1 = motel.Reserve(MotelRoom.Room1, async (room, reservation) =>
            {
                try
                {
                    await UniTask.WaitWhile(() => true, cancellationToken: reservation.cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    reservationCancelled = true;
                }
                finally
                {
                    if (reservation.checkOutCancellationToken.IsCancellationRequested)
                    {
                        reservationAborted = true;
                    }
                }
            }, dependencies: new object[] {exposedDisposable});
            Assert.AreEqual(MotelRoom.Room1, reservation1.room, "Reservation 1 should be in Room 1");
            exposedDisposable.Dispose();
            await UniTask.Delay(1000);
            Assert.IsTrue(motel.IsFree(MotelRoom.Room1), "Room 1 should be free");
            Assert.IsTrue(reservationCancelled, "Reservation should be cancelled");
            Assert.IsTrue(reservationAborted, "Reservation should be aborted");
        });
        
        [UnityTest]
        public IEnumerator AbortDuringAsyncDisposing() => UniTask.ToCoroutine(async () =>
        {
            var exposedDisposable = new ExposedDisposable();
            bool reservationCancelled = false;
            bool reservationAborted = false;
            bool reservationDisposingCancelled = false;
            await using var motel = new Motel<MotelRoom>();
            var reservation1 = motel.Reserve(MotelRoom.Room1, async (room, reservation) =>
            {
                try
                {
                    await UniTask.WaitWhile(() => true, cancellationToken: reservation.cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    reservationCancelled = true;
                }
                finally
                {
                    reservationAborted = reservation.checkOutCancellationToken.IsCancellationRequested;
                    try
                    {
                        await UniTask.WaitWhile(() => true, cancellationToken: reservation.checkOutCancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        reservationDisposingCancelled = true;
                    }
                    
                }
            }, dependencies: new object[] {exposedDisposable});
            Assert.AreEqual(MotelRoom.Room1, reservation1.room, "Reservation 1 should be in Room 1");
            await UniTask.Delay(100);
            reservation1.DisposeAsync().Forget();
            await UniTask.Delay(100);
            exposedDisposable.Dispose();
            await UniTask.Delay(100);
            Assert.IsTrue(motel.IsFree(MotelRoom.Room1), "Room 1 should be free");
            Assert.IsTrue(reservationCancelled, "Reservation should be cancelled");
            Assert.IsFalse(reservationAborted, "Reservation should be aborted");
            Assert.IsTrue(reservationDisposingCancelled, "Reservation disposing should be cancelled");
        });
    }
}