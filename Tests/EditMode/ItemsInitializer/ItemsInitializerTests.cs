using System.Collections.Generic;

namespace Metamatic
{
    using NUnit.Framework;
using System;
using System.Threading;

[TestFixture]
public class ItemsInitializerTests
{
    private class MockDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private class SomeClass
    {
        public static IDisposable Initialize(SomeClass someClass) => new MockDisposable();
    }

    [Test]
    public void ShouldInitializeNewItem()
    {
        var item = new SomeClass();
        var itemsInitializer = new ItemsInitializer<SomeClass>(SomeClass.Initialize, new CancellationToken());
        
        // Assert that OnHandleItem was called with the correct item
        IDisposable disposable = null;
        itemsInitializer.OnHandleItem += (i, d) => { if (i == item) disposable = d; };
        
        itemsInitializer.Handler(new[] { item });
        
        Assert.IsNotNull(disposable);
    }
    
    [Test]
    public void ShouldInitializeNewAndDisposeRemovedAndDoNotTouchExisten()
    {
        var item = new SomeClass();
        var itemsInitializer = new ItemsInitializer<SomeClass>(SomeClass.Initialize, new CancellationToken());
        
        // Assert that OnHandleItem was called with the correct item
        List<IDisposable> disposables = new List<IDisposable>();
        itemsInitializer.OnHandleItem += (i, d) =>
        {
            disposables.Add(d);
        };
        itemsInitializer.OnDisposeItem += (i, d) =>
        {
            disposables.Remove(d);
        };
        
        itemsInitializer.Handler(new[] { item });
        Assert.AreEqual(1, disposables.Count);
        itemsInitializer.Handler(new[] { item, new SomeClass() });
        Assert.AreEqual(2, disposables.Count);
        itemsInitializer.Handler(new[] { new SomeClass() });
        Assert.AreEqual(1, disposables.Count);
    }

    [Test]
    public void ShouldDisposeRemovedItem()
    {
        var item = new SomeClass();
        var itemsInitializer = new ItemsInitializer<SomeClass>(SomeClass.Initialize, new CancellationToken());
        
        var isDisposedCalled = false;
        itemsInitializer.OnDisposeItem += (i, d) => { if (i == item) isDisposedCalled = true; };
        
        itemsInitializer.Handler(new[] { item });
        itemsInitializer.Handler(new SomeClass[0]);
        
        Assert.IsTrue(isDisposedCalled);
    }

    [Test]
    public void ShouldDisposeAllOnSelfDispose()
    {
        var cts = new CancellationTokenSource();
        var itemsInitializer = new ItemsInitializer<SomeClass>(SomeClass.Initialize, cts.Token);

        var item1 = new SomeClass();
        var item2 = new SomeClass();
        itemsInitializer.Handler(new[] { item1, item2 });
        
        var disposedCount = 0;
        itemsInitializer.OnDisposeItem += (_, __) => { disposedCount++; };
        
        itemsInitializer.Dispose();
       
        Assert.AreEqual(2, disposedCount);
    }
    
    [Test]
    public void ShouldDisposeAllOnCancellation()
    {
        var cts = new CancellationTokenSource();
        var itemsInitializer = new ItemsInitializer<SomeClass>(SomeClass.Initialize, cts.Token);

        var item1 = new SomeClass();
        var item2 = new SomeClass();
        itemsInitializer.Handler(new[] { item1, item2 });
        // Assert that all items were disposed
        var disposedCount = 0;
        itemsInitializer.OnDisposeItem += (_, __) => { disposedCount++; };
        cts.Cancel();
        
        Assert.AreEqual(2, disposedCount);
    }

    [Test]
    public void ShouldNotLeakResourcesAfterDispose()
    {
        var item = new SomeClass();
        var itemsInitializer = new ItemsInitializer<SomeClass>(SomeClass.Initialize, new CancellationToken());
        itemsInitializer.Handler(new[] { item });
        itemsInitializer.Dispose();

        // Assert no more items can be handled after dispose
        Assert.Throws<ObjectDisposedException>(() => itemsInitializer.Handler(new[] { new SomeClass() }));
    }
}

}