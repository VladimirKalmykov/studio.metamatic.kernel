Глобальные синглтон
--

В игре часто требуется единый синглтон для обмена данными и событиями. На данный момент существует три работоспособных реализации глобальных синглтонов

### 1. Статический объект со статический методами и полями

Статический класс, который содержит статические поля и методы. Поля и методы могут быть публичными, что позволяет использовать их из любого места в коде.

```csharp
public static class GameLogic
{
   public static float JumpForce = 10f;
   public MicroEvent<float> OnJump = new MicroEvent<float>();
}
```

```csharp
GameLogic.OnJump.Subscribe((force) => Debug.Log($"Jumped with force {force}"));
```

Pros:

- Простота реализации
- Легкость использования
- Не требует дополнительных затрат на создание объекта
- Не требует дополнительных затрат на уничтожение объекта
- Не требует дополнительных затрат на проверку наличия объекта
- Не требуется инициализация объекта

Cons:

- Сохраняет состояние между сценами, что может вызывать нулевые ссылки
- Не может иметь сериализуемых полей, а соответственно не может иметь настроек в редакторе

### 2. Нестатический класс со статическими методами и полями, и предустановленным экземпляром

Экземпляр такого класса заранее создается в сцене в редакторе с DontDestroyOnLoad директивой. Статичные асинхронные методы вначале асинхронно находят или дожидаются экземпляр, что бы использовать его поля.

```csharp
class Runtime: MonoBehaviour {
public static Runtime Instance { get; private set; }
    private static MicroState<Runtime> _Instance = new();
    public static MicroStateReadOnly<Runtime> Instance => _Instance;
    
    public static UniTask<Runtime> GetInstance(CancellationToken token = default)
    {
        return _Instance.WaitUntilValueIsNotNull(token);
    }
    
    void Awake() {
        if (_Instance.value != null)
        {
            Debug.LogError("DuckyJumperRuntime instance already set");
        }

        _Instance.value = this;
    }
}    
}
```

```csharp
await Runtime.GetInstance();
```

Pros:

- Может иметь сериализуемые поля, которые можно настраивать из инспектора

Cons:

- Требует подготовки экземпляра в редакторе
- Требует асинхронного получения собственного экземпяра, поэтому не может содержать синхронных методов
- Не умеет сам создавать свой экземпляр, что может вызвать ошибки в случае его отсутствия
- Имена статических методов могут конфликтовать с именами экземплярных методов
- Задержка выполнения методов из-за асинхронного получения экземпляра

### 3. Нестатический класс с единственными статическим свойством  и не статическим экземпляром

Нестатический класс, который имеет ссылку на вспомогаетльный экземпляр, создаваемый автоматически по требованию, содержащий экземпляр данного класса. Подход показал хорошую практичность и удобство использования.

```csharp
public class Dispatcher
{
    private static DispatcherComponent _globalDispatcher;
    public static Dispatcher global
    {
        get => (_globalDispatcher ??= new GameObject("__GLOBALDISPATCHER__").AddComponent<DispatcherComponent>()).dispatcher;
    }
}
```

```csharp
Disaptcher.global.Dispatch(new SomeEvent());
```

Pros:
- Не требует подготовки экземпляра в редакторе
- Не требует асинхронного получения собственного экземпяра
- Легко обращаться к экземпляру через статическое свойство
- Доступен в любое время и в любом месте
- В теории может быть использован не только как синглтон, поскоьку не имеет ограничений на количество экземпляров

Cons:
- Не может иметь сериализуемых полей, а соответственно не может иметь настроек в редакторе
- Сложность создания

### 4. Нестатический MonoBehaviour без статических методов и полей, но с регистрацией в статическом словаре

Обычный MonoBahaviour, который при включении регистрируется в статическом словаре, а при выключении удаляется из него. При этом в словаре хранится только один экземпляр данного класса, что позволяет использовать его как синглтон.

```csharp
public class GameManager: AsyncBehaviour {
      protected override async UniTask RunAsync(CancellationToken token) {
          using var registration = Dispatcher.global.Register(this);
          await token;
      }
}
```

```csharp
var gameManager = await Dispatcher.global.WhenRegistered<GameManager>(); // Async getting registered object
// or
var gameManager = Dispatcher.global.OnRegistered<GameManager>(); // Sync getting registered object (if it's already registered)
// or
using var listener = Dispatcher.global.GetRegistered<GameManager>((gameManager) => Debug.Log("GameManager registered")); // Subscribing to registration event
```

Pros:
- Может иметь сериализуемые поля, которые можно настраивать из инспектора и имеет все приемущества MonoBehaviour
- Может быть получен асинхронно из любого места и в любое время (при правильном словаре подписаться на него можно до его регистрации, во время и после)
- Может содержать как статические, так и нестатические методы и поля
- Может уничтоожаться и создаваться в любое время (поскольку можно подписаться на событие его регистрации и удаления)
- Легко создается - достаточно при инициализации добавить его в словарь и при уничтожении удалить из него
- Можно содержать множество таких псевдо-синглтонов с разделением по ролям

Cons:
- Сложность получения (нужно подписаться на событие регистрации или использовать асинхронный метод - что не позволяет отслеживать его уничтожение)
- Не сохраняет состояние между сценами (если только не использовать DontDestroyOnLoad)
- Предполагает наличие продуманного вспомогательного статичного словаря для регистрации и удаления объектов

Итоги
--

| Подход | Persistent | Easy to create | Easy to use | Cross scene | Safe references | Multiple instances | Effective in practice |
| --- |------------|----------------|-------------|-------------|-----------------|--------------------|-----------------------|
| GameLogic | -          | +              | +           | +           | -               | -                  | 3                     |
| Runtime | +          | -              | -           | +           | +               | -                  | 2                     |
| Dispatcher | -          | -              | +           | +           | +               | +                  | 5                     |
| GameManager | +          | +              | -           | +          | +               | -                  | 4                     |

| Подход | Плюсы | Минусы |
| --- |-------|--------|
| GameLogic | 3     | 2      |
| Runtime | 2     | 4      |
| Dispatcher | 4     | 2      |
| GameManager | 4     | 2      |