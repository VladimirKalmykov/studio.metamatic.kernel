ExclusiveHandler
--

Позволяет управлять последовательностями, которые служат для асинхронных взаимосключающих операций.

```csharp
var characterInstantiator = new ExclusiveHandler<CharacterData>((characterData, cancellationToken) => {
    var character = new Character(characterData);
    cancellationToken.Register(() => character.Dispose());
});
characterInstantiator.Handle(characterData1);
await Task.Delay(1000);
characterInstantiator.Handle(characterData2);
```

Обработчик переданный ExclusiveHandler будет принимать новые данные и cancellationToken, который будет играть роль диструктора для объекта, созданного в обработчике.
Таким образом передавая управление SingletonSequence, можно гарантировать, что предыдущий объект будет уничтожен, прежде чем будет создан новый.

ExclusiveArrayHandler
--

Позволяет управлять массивами, которые служат для асинхронных взаимосключающих операций.

```csharp
var effectsInstantiator = new ExclusiveArrayHandler<EffectData>((effectData, cancellationToken) => {
    var effect = new Effect(effectData);
    cancellationToken.Register(() => effect.Dispose());
});
effectsInstantiator.Handle(new [] {effectData1, effectData2});
await Task.Delay(1000);
effectsInstantiator.Handle(new [] {effectData2, effectData3, effectData4});
```

Принцип тот же, что и для ExclusiveHandler, но в данном случае Handle вызывает с массивом данных. Финальный обработчик будет вызван только для тех элементов массива, которые отсутствовали в предыдущем массиве. Для элементов которые присутствовали в предыдущем массиве, но отсутствуют в новом, будет вызван cancellationToken, что позволяет уничтожить объекты, которые больше не нужны.

```csharp