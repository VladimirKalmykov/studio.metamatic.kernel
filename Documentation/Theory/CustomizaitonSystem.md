Customization system
==

The customization system is a system that allows the user to customize character. For example, we have a duck character. This character module allows to customize the duck skin (texture), duck beak (mesh), duck eyes (mesh), duck hat (mesh).

Lets create struct that will hold all the customization options for the duck character.

```csharp
public struct DuckCustomizationData
{
    public Texture2D skin;
    public Prefab beak;
    public Prefab eyes;
    public Prefab hat;
}
```

We create socket scheme that will hold the customization data.

```csharp
class DuckCustomizationSocketScheme: SocketScheme<TypeBundle<DuckCustomizationData>>
{
    
}
```

And socket itself.

```csharp
class DuckCustomizationSocket: Socket<DuckCustomizationSocketScheme>
{
    
}
```

This socket will be added as component to the duck character object.

Another component handles the customization data and applies it to the duck character.

```csharp
class DuckCustomizator : MonoBehaviour
{
    public Pin<DuckCustomizationData> customizationDataPin;
    
    private VirtualPinReader<DuckCustomizationData> _customizationDataReader;
    
    void OnEnable() {
        _customizationDataReader = customizationDataPin.CreateVirtualPinReader(this, true)
        _customizationDataReader.SubscribeDiff(OnCustomizationDataChanged);
    }
    
    void OnDisable() {
        _customizationDataReader.UnsubscribeDiff(OnCustomizationDataChanged);
        _customizationDataReader = null;
    }
    
    private void OnCustomizationDataChanged(DuckCustomizationData data, DuckCustomizationData prevData)
        // apply customization data to the duck character
        // for example
        if (data.skin != prevData.skin) {
            // change duck skin
        }
        if (data.beak != prevData.beak) {
            // change duck beak
        }
        if (data.eyes != prevData.eyes) {
            // change duck eyes
        }
        if (data.hat != prevData.hat) {
            // change duck hat
        }
    }
}
```

Then we create scriptable effect that will be used to customize the duck character.

```csharp
class MonoTypeSocketModifierEffect<TData> : ScriptableEffect<DuckEffector>
{
    private VirtualPlug<DuckCustomizationSocketScheme> _plug;
       public override UniTask RunEffect(EffectExecutionContext<TEffector, TProps> executionContext,
            CancellationToken cancellationToken) {
               _plug = new VirtualPlug<SocketScheme<TypeBundle<TData>, TypeBundle<TData>>>(this);
               var socket = executionContext.target.GetComponent<Socket<SocketScheme<TypeBundle<TData>, TypeBundle<TData>>>>();
               socket.ConnectPlug(_plug);
               _plug.inputPins[0].Subscribe(HandleDataChanged);
               while (await executionContext.cycle) {
                    // ...    
               }
               _plug.Dispose();
           }
    
    private void HandleDataChanged(TData data) {
           _plug.outputPins[0].Write(OnDataChange(data));
    }
    
    protected virtual TData OnDataChange(TData data) {
        return data;
    }
}
```

And add to final scriptable effect RequiredComponent attribute to be able to check the compatibility of the effect with the game object

```csharp
[RequireComponent(typeof(DuckCustomizationSocket))]
class DuckCustomizationEffect : MonoTypeSocketModifierEffect<DuckCustomizationData>
{
    protected override DuckCustomizationData OnDataChange(DuckCustomizationData data) {
        // apply customization data to the duck character
        return data;
    }
}
```

Then we create scriptable effect that will be used to customize the duck character.

```csharp