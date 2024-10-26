using NUnit.Framework;

namespace Tests.EditMode.EditDerivedStruct
{
    [TestFixture]
    public class EditDerivedStructTests
    {
        public struct Position
        {
            public float x;
            public float y;
        }
        
        public abstract class Owner<T>
        where T: struct
        {
            protected T State;
        }
        
        public class Player: Owner<Position>
        {
            public float x
            {
                get => State.x;
                set => State.x = value;
            }
            
            public float y
            {
                get => State.y;
                set => State.y = value;
            }
        }
        [Test]
        public void EditDerivedStructTest()
        {
            var player = new Player();
            player.x = 10;
            player.y = 20;
            Assert.AreEqual(10, player.x);
            Assert.AreEqual(20, player.y);
        }
    }
}