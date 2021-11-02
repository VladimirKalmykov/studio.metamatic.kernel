## Move object towards target using rigibody force

```cs
dir = target.transform.position - transform.position;
dir = dir.normalized;
rigidbody.AddForce(dir * force);
```

## Awesome list of build-in Unity icons

[https://gist.github.com/MattRix/c1f7840ae2419d8eb2ec0695448d4321](https://gist.github.com/MattRix/c1f7840ae2419d8eb2ec0695448d4321)

You can use it with method

```cs
var icon = EditorGUIUtility.FindTexture ("Folder Icon")
```