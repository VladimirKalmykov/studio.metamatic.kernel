Hints
--

Использование PlayerLoop из любой части приложения. Класс PlayerLoop должен реализовывать интерфейс IPlayerLoopItem, метод MoveNext должен вернуть false, что бы прекратить петлю.

```c#
using Cysharp.Threading.Tasks;

public class PlayerLoop : IPlayerLoopItem
{
    private int counter = 0;
    public bool MoveNext()
    {
        Debug.Log($"Player loop: tick {counter}");
        counter++;
    if (counter >= 5)
    {
        return false;
    }
    
    return true;
    }
}

PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, new TestPlayerLoop());
```
