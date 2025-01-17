using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Metamatic.Tests
{
    
    [TestFixture]
    public class MotelTests
    {
        [Flags]
        public enum Rooms
        {
            None = 0,
            Room1 = 1 << 0,
            Room2 = 1 << 1,
            Room3 = 1 << 2,
            Room4 = 1 << 3,
        }

        [Flags]
        public enum SmRooms
        {
            None = 0,
            Room1 = 1 << 0,
            Room2 = 1 << 1,
            Room3 = 1 << 2,
        }
        
        [UnityTest]
        public IEnumerator TestDefaultUniTask() => UniTask.ToCoroutine(async () =>
        {
            var emptyTask = new UniTask();
            await emptyTask;
            Assert.Pass();
        });

        [UnityTest]
        public IEnumerator SimpleBookIn() => UniTask.ToCoroutine(async () =>
        {
            using var motel = Motel<Rooms>.Create();
            var bookedRoom = Rooms.None;
            await using var disposable = motel.Book(Rooms.Room1 | Rooms.Room2, Rooms.None, async (room, cancellation) =>
            {
                Console.Debug("Motel", "Test", "Booking", $"Start (Room: {room})");
                bookedRoom = room;
                await cancellation.Signal;
                Console.Debug("Motel", "Test", "Booking", "End");
            });
            await UniTask.Delay(100);
            Assert.AreEqual(Rooms.Room1, bookedRoom, "After 100ms, the booked room passed to handler should be Room1.");
            Assert.AreEqual(Rooms.Room1, motel.bookedRooms, "After 100ms, the booked room should be Room1.");
        });
        
        [UnityTest]
        public IEnumerator SimpleBookOut() => UniTask.ToCoroutine(async () =>
        {
            using var motel = Motel<Rooms>.Create();
            var disposable = motel.Book(Rooms.Room1 | Rooms.Room2, Rooms.None, async (room, cancellation) =>
            {
                
                await cancellation.Signal;
                
            });
            await UniTask.Delay(100);
            Assert.AreEqual(Rooms.Room1, motel.bookedRooms, "After 100ms, the booked room should be Room1.");
            disposable.DisposeAsync();
            await UniTask.Delay(100);
            Assert.AreEqual(Rooms.None, motel.bookedRooms, "After 100ms, the booked room should be None.");
        });
        
        [UnityTest]
        public IEnumerator AsyncBookOut() => UniTask.ToCoroutine(async () =>
        {
            using var motel = Motel<Rooms>.Create();
            var disposable = motel.Book(Rooms.Room1 | Rooms.Room2, Rooms.None, async (room, cancellation) =>
            {
                await cancellation.Signal;
                await UniTask.Delay(100);
            });
            await UniTask.Delay(100);
            Assert.AreEqual(Rooms.Room1, motel.bookedRooms, "After 100ms, the booked room should be Room1.");
            disposable.DisposeAsync();
            await UniTask.Delay(25);
            Assert.AreEqual(Rooms.Room1, motel.bookedRooms, "After 25ms, the booked room should be Room1.");
            await UniTask.Delay(100);
            Assert.AreEqual(Rooms.None, motel.bookedRooms, "After 100ms, the booked room should be None.");
        });
        
        [UnityTest]
        public IEnumerator BoolBooked() => UniTask.ToCoroutine(async () =>
        {
            using var motel = Motel<Rooms>.Create();
            var room1 = Rooms.None;
            var room2 = Rooms.None;
            await using var book1 = motel.Book(Rooms.Room1 | Rooms.Room2, Rooms.None, async (room, cancellation) =>
            {
                room1 = room;   
                await cancellation.Signal;
            });
            await using var book2 = motel.Book(Rooms.Room1 | Rooms.Room2, Rooms.None, async (room, cancellation) =>
            {
                room2 = room;
                await cancellation.Signal;
            });
            await UniTask.Delay(100);
            Assert.AreEqual(Rooms.Room1, room1, "After 100ms, the booked room passed to handler should be Room1.");
            Assert.AreEqual(Rooms.Room2, room2, "After 100ms, the booked room passed to handler should be Room2.");
        });
        
        [UnityTest]
        public IEnumerator BoolPreferredAfterAlternative() => UniTask.ToCoroutine(async () =>
        {
            using var motel = Motel<Rooms>.Create();
            var room1 = Rooms.None;
            var room2 = Rooms.None;
            var book1 = motel.Book(Rooms.Room1, Rooms.None, async (room, cancellation) =>
            {
                room1 = room;   
                await cancellation.Signal;
                room1 = Rooms.None;
            });
            await using var book2 = motel.Book(Rooms.Room1, Rooms.Room2, async (room, cancellation) =>
            {
                room2 = room;
                await cancellation.Signal;
                room2 = Rooms.None;
            });
            Assert.AreEqual(Rooms.Room1, room1, "The room1 should be Room1.");
            Assert.AreEqual(Rooms.Room2, room2, "The room2 should be Room2.");
            // Free the room1
            await book1.DisposeAsync();
            Assert.AreEqual(Rooms.Room1, motel.bookedRooms, "The booked room should be Room1.");
            Assert.AreEqual(Rooms.None, room1, "The booked room 1 passed to handler should be None.");
            Assert.AreEqual(Rooms.Room1, room2, "The booked room 2 passed to handler should be Room2.");
        });
        
        [UnityTest]
        public IEnumerator AsyncCompetitiveRebookIn() => UniTask.ToCoroutine(async () =>
        {
            using var motel = Motel<SmRooms>.Create();
            var room1 = SmRooms.None;
            var room2 = SmRooms.None;
            var room3 = SmRooms.None;
            var book1 = motel.Book(SmRooms.Room1, SmRooms.None, async (room, cancellation) =>
            {
                room1 = room;   
                await cancellation.Signal;
                room1 = SmRooms.None;
            });
            await using var book2 = motel.Book(SmRooms.Room1, SmRooms.Room2, async (room, cancellation) =>
            {
                room2 = room;
                await cancellation.Signal;
                room2 = SmRooms.None;
            });
            await using var book3 = motel.Book(SmRooms.Room1, SmRooms.Room2 | SmRooms.Room3, async (room, cancellation) =>
            {
                room3 = room;
                await cancellation.Signal;
                room3 = SmRooms.None;
            });
            Assert.AreEqual(SmRooms.Room1, room1, "The room1 should be Room1.");
            Assert.AreEqual(SmRooms.Room2, room2, "The room2 should be Room2.");
            Assert.AreEqual(SmRooms.Room3, room3, "The room3 should be Room3.");
            // Free the room1
            await book1.DisposeAsync();
            Assert.AreEqual(SmRooms.None, room1, "The room1 should be None.");
            Assert.AreEqual(SmRooms.Room1, room2, "The room2 should be Room1.");
            Assert.AreEqual(SmRooms.Room2, room3, "The room3 should be Room2.");
        });
        
        [UnityTest]
        public IEnumerator AsyncDisposeMotel() => UniTask.ToCoroutine(async () =>
        {
            var motel = Motel<Rooms>.Create();
            var room1 = Rooms.None;
            var room2 = Rooms.None;
            var room1DisposedAsync = false;
            var room2DisposedAsync = false;
            {
                await using var book1 = motel.Book(Rooms.Room1, Rooms.None, async (room, cancellation) =>
                {
                    room1 = room;   
                    await cancellation.Signal;
                    room1 = Rooms.None;
                    await UniTask.Delay(100);
                    room1DisposedAsync = true;
                });
                await using var book2 = motel.Book(Rooms.Room1, Rooms.Room2, async (room, cancellation) =>
                {
                    room2 = room;
                    await cancellation.Signal;
                    room2 = Rooms.None;
                    await UniTask.Delay(100);
                    room2DisposedAsync = true;
                });
                Assert.AreEqual(Rooms.Room1, room1, "The room1 should be Room1.");
                Assert.AreEqual(Rooms.Room2, room2, "The room2 should be Room2.");
            }
            // Dispose async the motel
            motel.Dispose();
            Assert.AreEqual(Rooms.None, motel.bookedRooms, "The booked room should be None.");
            Assert.AreEqual(Rooms.None, room1, "The booked room 1 passed to handler should be None.");
            Assert.AreEqual(Rooms.None, room2, "The booked room 2 passed to handler should be None.");
            Assert.IsTrue(room1DisposedAsync, "The room1 should be disposed async.");
            Assert.IsTrue(room2DisposedAsync, "The room2 should be disposed async.");
        });

        [UnityTest]
        public IEnumerator BookAndImmediatelyDispose() => UniTask.ToCoroutine(async () =>
        {
            using var motel = Motel<Rooms>.Create();
            var steps = new List<int>();
            var book1 = motel.Book(Rooms.Room1, Rooms.None, async (room, cancellation) =>
            {
                try
                {
                    steps.Add(1);
                    await PlayerLoop.Delay(0.5f, PlayerLoopTiming.Update, cancellation.Signal);
                    steps.Add(2); // Will not be added, because the book1 is disposed immediately
                    await cancellation.Signal;
                }
                catch (OperationCanceledException)
                {
                    // Ignore
                }
                finally
                {
                    steps.Add(3);
                    var abortSignal = cancellation.AbortSignal;
                    UniTask.Delay(1000);
                    await PlayerLoop.Delay(0.5f, PlayerLoopTiming.Update, abortSignal);
                    steps.Add(4);
                }
            });
            await book1.DisposeAsync();
            Assert.AreEqual(new List<int> { 1, 3, 4 }, steps);
        });
        
        [UnityTest]
        public IEnumerator GetFromPool() => UniTask.ToCoroutine(async () =>
        {
            var motel1 = Motel<Rooms>.Create();

            {
                await using var book1 = motel1.Book(Rooms.Room1, Rooms.None, async (room, cancellation) =>
                {
                    await cancellation.Signal;
                });
            
                Assert.AreEqual(Rooms.Room1, motel1.bookedRooms, "The booked room of motel1 should be Room1.");
            
                // Dispose the motel
                motel1.Dispose();
            
                Assert.AreEqual(Rooms.None, motel1.bookedRooms, "The booked room of motel1 should be None");
            }
            
            using var motel2 = Motel<Rooms>.Create();
            
            Assert.AreEqual(Rooms.None, motel2.bookedRooms, "The booked room of motel2 should be None");

            {
                await using var book2 = motel2.Book(Rooms.Room2, Rooms.None, async (room, cancellation) =>
                {
                    await cancellation.Signal;
                });
                
                Assert.AreEqual(Rooms.Room2, motel2.bookedRooms, "The booked room of motel2 should be Room2.");
                Assert.AreEqual(Rooms.None, motel1.bookedRooms, "The booked room of motel2 should be None.");
            }
        });

        [UnityTest]
        public IEnumerator ChangePreferredRoomsDynamically() => UniTask.ToCoroutine(async () =>
        {
            var motel1 = Motel<Rooms>.Create();
            var bookedRoom = Rooms.None;
            await using var book1 = motel1.Book(Rooms.Room1, Rooms.None, async (room, cancellation) =>
            {
                bookedRoom = room;
                await cancellation.Signal;
                bookedRoom = Rooms.None;
            });
            Assert.AreEqual(Rooms.Room1, bookedRoom, "The booked room should be Room1.");
            
            book1.SetPreferredRooms(Rooms.Room2);
            
            Assert.AreEqual(Rooms.Room2, bookedRoom, "The booked room should be Room2.");
        });
        
        [UnityTest]
        public IEnumerator ChangeCompetitivePreferredRoomsDynamically() => UniTask.ToCoroutine(async () =>
        {
            var motel1 = Motel<Rooms>.Create();
            var bookedRoom1 = Rooms.None;
            var bookedRoom2 = Rooms.None;
            await using var book1 = motel1.Book(Rooms.Room1, Rooms.None, async (room, cancellation) =>
            {
                bookedRoom1 = room;
                await cancellation.Signal;
                bookedRoom1 = Rooms.None;
            });
            await using var book2 = motel1.Book(Rooms.Room1, Rooms.Room2, async (room, cancellation) =>
            {
                bookedRoom2 = room;
                await cancellation.Signal;
                bookedRoom2 = Rooms.None;
            });
            Assert.AreEqual(Rooms.Room1, bookedRoom1, "The booked room 1 should be Room1.");
            Assert.AreEqual(Rooms.Room2, bookedRoom2, "The booked room 2 should be Room2.");
            
            book1.SetPreferredRooms(Rooms.Room2);
            
            Assert.AreEqual(Rooms.Room1, bookedRoom2, "The booked room 2 should be Room1.");
            Assert.AreEqual(Rooms.Room2, bookedRoom1, "The booked room 1 should be Room2.");
        });
    }
}