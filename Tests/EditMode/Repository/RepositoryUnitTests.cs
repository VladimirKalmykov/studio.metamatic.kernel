using System;
using System.Collections;
using UnityEngine.TestTools;
using Metamatic;
using Metamatic.Effects;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class RepositoryUnitTests
    {
        [Serializable]
        public struct TestData
        {
            public int a;
            public string b;
            public string[] c;
            public AssetReferenceScriptableEffect d;
        }
        [UnityTest]
        public IEnumerator SaveAndLoadData()
        {
            BinaryFileSystemStorage.Save<TestData>("unittesting/simplesaveload", new TestData()
            {
                a = 2,
                b = "hello world",
                c = new string[] { "one", "two", "three", "four", "five" },
            });
            var loadedData = BinaryFileSystemStorage.Load<TestData>("unittesting/simplesaveload");
            Assert.AreEqual(2, loadedData.a);
            Assert.AreEqual("hello world", loadedData.b);
            Assert.True(loadedData.c != null && loadedData.c.Length == 5 && loadedData.c[0] == "one", "Array should be equal");
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator SaveAndLoadFileWithNestedFolders()
        {
            var address = "unittesting/sub1/sub2/data1";
            BinaryFileSystemStorage.Save<TestData>(address, new TestData()
            {
                a = 2,
                b = "hello world",
                c = new string[] { "one", "two", "three", "four", "five" },
            });
            var loadedData = BinaryFileSystemStorage.Load<TestData>(address);
            Assert.AreEqual(2, loadedData.a);
            Assert.AreEqual("hello world", loadedData.b);
            Assert.True(loadedData.c != null && loadedData.c.Length == 5 && loadedData.c[0] == "one", "Array should be equal");
            yield return null;
        }
    }
}