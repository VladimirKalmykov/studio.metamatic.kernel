

Store - объект, предоставляющий управление над состоянием (факт. state machine), имеющий два главным метода

- Subscribe(StateChangeHandler handler)
- SetState(StateModifier modifier)

Каждый store привязан к конкретной структуре.
К примеру, store обсуживающий кошелек игрока, может иметь такой вид
```cs
public struct WalletState {
    public int coins;
}
```

К примеру у нас в UI есть блок, показывающий количество монет в кошельке. Получение этих данных может быть следующим:

```cs
void Start() {
    /* Подписываемся на кошелек объекта Player */
    OnDispose += Database.GetStore<WalletState>("Player")
        .Subscribe((ref WalletState nextState, ref WalletState prevState) => {
            GetComponentInChildren<CoinsView>("quantity").coins = nextState.coins;
        });
}
```

К примеру, игрок по мере прохождения уровня игры приобретает новые монеты. Но все новые монеты будут потеряны, если игрок умрет, не дойдя до конца уровня.

На момент начала уровня мы можем создать особую инстанцию состояния, которая бы считала записи в кошелек временными.

```cs
void OnLevelStart() {
    /* Переключиться на ветку ActiveLevel, которая будет автоматически проексироваться на ветку master */
    Database.CheckoutBranch('ActiveLevel');
}
```

Тогда условно создается новая ветка состояния игры _ActiveLevel_. Метод _GetStore_ в таком случае должен предоставить Store, который относится к этой ветке, а не к master. Но в случае, если store в ветке ActiveLevel не будет найден, то при подписки данные для него будут забираться из ветки master, до тех пор пока в ветку _ActiveLevel_ не будет произведена запись для конкретного объекта, конкретного стора.

Все случаи SetState будут производить запись уже в новую ветку, а не master.

```cs
virtualStore.SetState((ref WalletState state) => {
    state.coins += 10;
    /*
    Database
        .activeBranch # Источник сожержит активную ветку
        .value # Получить активную ветку
        .TouchEntitiy(store.entityName) # Убедиться, что сущность существует
        .SetState<TState>(modifier) # Применить состояние
    */
});
```

В случае с подпиской ситуация выглядит похоже:

```cs
virtualStore.Subscribe((ref WalletState nextState, ref WalletState prevState) => {
    /*
    Database.activeBranch.Subscribe(branch => {
        branch.SubscribeEntity(this.entityName, (entity) => {
            entity.Subscribe<TState>(store => {
                if (store == null) {
                    // Fallback
                }
            });
        });
    })
    */    
})
```

В случае удаления ветки, все записанные в нее данные будут удалены. Для всех подписчиков эта операция будет незаметна, их просто пенеправлят на те же данные, но ветке от которой была создана ActiveLevel.

```cs
Database.ReleaseBranch('ActiveLevel');
```

Что бы данные из ветки ActiveLevel попали в ее старшую ветку, перед удалением можно выполнить merge в ветку-родитель.

```cs
Database.MergeBranch<WalletState>('ActiveLevel', (ref WalletState leftState, ref WalletState rightState) => {
    rightState.coins = leftState.coins;
});
```

Для того, что бы GetStore мог незаметно переключаться между ветками-наследниками, сам GetStore возвращает не фактический Store, а его вертуальный прокси.

```cs
DatabaseStore<TState> GetStore<TState>(string entityName) {
    return new DatabaseVirtualStore<TState>(
        /* activeBranch - это _DatabaseBranchSource (derived from ObservableValue<string>), который позволяет подписаться на имя активной ветки */
        Database.activeBranch,
        /* Имя сущности */
        entityName
    );
}
```

Virtual Store следит за изменением _DatabaseBranchSource_ и незаменто для пользователя подставляет новые значения из новой ветки.

Если же у нас активная ветка не master, но мы хотим принудительн прочитать или записать данные из ветки master, тогда мы должны уточнить с какой веткой работаем при получении Virtual store.

```cs
Database.Branch("master").GetStore<WalletState>("Player")
        .Subscribe((ref WalletState nextState, ref WalletState prevState) => {
            
        });
```

Контакт выполнения
--

Так же как, например, игровой объект, может зайти в некоторую зону, в области действия которых все применяемое состояние является временным до выхода из этой зоны. В таком случае необходимо предусмотреть возможность указывать текущую ветку исходя из положения игрового объекта или нахождения внутри триггеров.

```cs
/* Указывая вместо имени ветки игровой объект, внутрення логика сама определить принадлежность объекта к ветке*/
Database.Branch(this).GetStore<WalletState>("Player")
// Где this: MonoBehaviour
```
Указывая вместо имени ветки игровой объект, внутрення логика сама определить принадлежность объекта к ветке. Для этого у объекта (или его родителя) должен быть вспомогательный компонент `DatabaseContext` или `DatabaseEntity` (который наследует DatabaseContext).

_Branch_ возвращает объект DatabaseSelector, который помимо содержания информации о источнике ветки, содержит API для подписки, записи и чтения из Store.

В случае наличия DatabaseEntity мы можем начать работу с него, при этом указывать имя сущности нам уже не нужно, так как она уже будет содержаться в компоненте DatabaseEntity.

```cs
Database.Entity(this).Subscribe<WalletState>(...);
Database.Entity(this).SetState(...)
```

При таком подходе будет автоматически определена актуальная ветка базы и имя сущности.

