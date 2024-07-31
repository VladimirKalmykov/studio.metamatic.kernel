using Metamatic;
using NUnit.Framework;

namespace Tests.EditMode.ModernWireSystem
{
    [TestFixture]
    public class ApiModernWireTests
    {
        class TestApi
        {
            public virtual int Sum(int a, int b)
            {
                return 0;
            }
        }
        [Test]
        public void ConnectionTest()
        {
            var client = new ApiModernWireClient<TestApi>();
            var implement = new ApiModernWireImplement<TestApi>();
            
            
        }
    }
}