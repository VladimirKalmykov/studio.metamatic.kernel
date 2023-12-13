Wire
==

Performance information
--

By the performance tests I can summarize that the Wire coupled with another Wire can pass data with 48 fields 10000 times per 1 ms. It means that you can run 10k `Wire<Struct48Fields>` update cycles simultaneously in your game and have 1000 fps. Also couple of `Wire<int>` gives 100k operations per 1 ms.

### Recommended usage

1. Use `Mutate` method to change the state of the wire instead of re-creating struct. It will save you from garbage collection overhead.

```csharp

struct MyInputData
{
    public int A;
    public int B;
    //... 100 fields more
}

var wire = new Wire<MyInputData>(new MyInputData());
wire.Mutate((ref MyInputData state) => state.A = 10);
```
