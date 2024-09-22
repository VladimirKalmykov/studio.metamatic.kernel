Spell
--

Spell is a approach to encapsulate of influence logic. There're two types of spells: **ISyncSpell** and **IAsyncSpell**. ISyncSpell is a spell that act in a moment, and IAsyncSpell is a spell that act continuously.

For example, once give damage to a character by impact is a ISyncSpell, continuously give damage to a character, e.g. by walking in lava is a IAsyncSpell.

To create a spell, you need to implement **ISyncSpell** or **IAsyncSpell** interface with a struct.

```csharp
public struct GrowthPieSpell : ISyncSpell
{
    public void OnCast(Object spellCaster, Object target)
    {
        Spell.Resolve<Size>(target).Value += 1;
    }
}
```

To cast a spell, you need to call **Spell.Cast** method.

```csharp
Spell.Cast(new GrowthPieSpell(), growthPie, player);
```

To create a IAsyncSpell, you need to implement **IAsyncSpell** interface with a struct.

```csharp
public struct LavaDamageSpell : IAsyncSpell
{
    public UniTask OnCast(Object spellCaster, Object target, CancellationSignal cancellationSignal)
    {
        return UniTask.Void(async () =>
        {
            while (!cancellationSignal.IsCancellationRequested)
            {
                Spell.Resolve<Health>(target).Value -= 1;
                await UniTask.Delay(1000);
            }
        });
    }
}
```

There are two ways to cast a IAsyncSpell. One is to call **Spell.Cast** the same way as you cast sync spell.

```csharp
var spellCast = Spell.Cast(new LavaDamageSpell(), lava, player);
```

You got a **SpellCast** object from **Spell.Cast** method. You can cancel the spell by calling **Dipose** method.

```csharp
spellCast.Dispose();
```

And you can call Modify method to modify the spell.

```csharp
spellCast.Modify((ref LavaDamageSpell spell) =>
{
    spell.Damage = 2;
});
```

Another way to cast a IAsyncSpell is to create **SpellSource** object and call **Cast** method.

```csharp
var spellSource = new SpellSource(new LavaDamageSpell(), lava);
```

With **SpellSource** object, you can cast a spell by calling **Cast** method multiple times.

```csharp
var spellCastOnPlayer = spellSource.Cast(player);
var spellCastOnEnemy = spellSource.Cast(enemy);
```

And you can cancel all spells by calling **Dispose** method on **SpellSource** object.

```csharp
spellSource.Dispose();
```

Or individually by calling **Dispose** method on **SpellCast** object.

```csharp
spellCastOnPlayer.Dispose();
spellCastOnEnemy.Dispose();
```

And you can call Modify method to modify the spell.

```csharp
spellSource.Modify((ref LavaDamageSpell spell) =>
{
    spell.Damage = 2;
});
```

And all spells casted by **SpellSource** object will be modified.

# Features

## Wait for spell to finish

You can wait for a async spell to finish by awaiting **CastTask** property on **SpellCast** object.

```csharp
await spellCast.CastTask;
```