using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework;
using Metamatic.Framework.Workshop;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Auto-follow and look at the player (provided by PlayerSystem) with CinemachineCamera
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class CinemachineCameraTrackingPlayerHelper : AsyncBehaviour
{
    [SerializeField] private bool follow = true;
    [SerializeField] private bool lookAt = true;
    
    private CinemachineCamera m_CinemachineCamera;
    protected override async UniTask Run(Ready ready, Signal signal)
    {
        m_CinemachineCamera = GetComponent<CinemachineCamera>();
        using var observePlayer = PlayerSystem.WatchPlayer(OnPrimaryPlayerChange, 0);
        await ready();
    }
    
    private void OnPrimaryPlayerChange(Entity player)
    {
        if (follow)
        {
            m_CinemachineCamera.Follow = player.transform;
        }
        if (lookAt)
        {
            m_CinemachineCamera.LookAt = player.transform;
        }
    }
}
