using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;

public class MaterialAlphaPresenter : Presenter
{
    [SerializeField] public float fadeOutDuration = 0.5f;
    [SerializeField] public float fadeInDuration = 0.5f;
    private SharedMaterialPropertyBlock m_SharedMaterialPropertyBlock;
    
    private static readonly int Alpha = Shader.PropertyToID("_Alpha");
    
    protected override void Awake()
    {
        base.Awake();
        m_SharedMaterialPropertyBlock = this.GetOrAddComponent<SharedMaterialPropertyBlock>();
    }
    protected override async UniTaskVoid OnPresent(Signal cancellationSignal)
    {
        await PlayerLoop.EachFrame(progress =>
        {
            m_SharedMaterialPropertyBlock.SetFloat(Alpha, progress);
            m_SharedMaterialPropertyBlock.Apply();
        }, fadeInDuration, signal: cancellationSignal);
    }

    protected override UniTask OnDismiss(Signal cancellationSignal)
    {
        return PlayerLoop.EachFrame(progress =>
        {
            m_SharedMaterialPropertyBlock.SetFloat(Alpha, 1f - progress);
            m_SharedMaterialPropertyBlock.Apply();
        }, fadeOutDuration, signal: cancellationSignal);
    }

    protected override void OnReset()
    {
        m_SharedMaterialPropertyBlock.SetFloat(Alpha, 0);
        m_SharedMaterialPropertyBlock.Apply();
    }
}
