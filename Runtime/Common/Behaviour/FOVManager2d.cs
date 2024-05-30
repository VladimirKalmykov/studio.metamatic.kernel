using Cysharp.Threading.Tasks;

namespace Metamatic
{
    using System.Collections.Generic;
using System.Threading;
using Metamatic;
using Metamatic.Actions;
using Metamatic.MicroBehaviours;
using UnityEngine;

public class FOVManager2d : AsyncBehaviour
{
    [SerializeField] private float m_OuterBuffer = 1.0f;
    [SerializeField] private float m_InnerBuffer = 1.0f;
    
    private CameraFOVTracker2d m_CameraFovTracker2d;
    public CameraFOVTracker2d cameraFovTracker2d => m_CameraFovTracker2d;
    
    protected override async void RunAsync(CancellationToken cancellationToken)
    {
        Debug.Log("CameraFOVTracker2d RunAsync Started");
        /* Subscribe to camera controller */
        await using var cameraFovTracker2d = m_CameraFovTracker2d = new CameraFOVTracker2d(null, m_InnerBuffer, m_OuterBuffer, transform.position.z);
        using var validationListener = SubscribeValidate(() =>
        {
            m_CameraFovTracker2d.innerBuffer = m_InnerBuffer;
            m_CameraFovTracker2d.outerBuffer = m_OuterBuffer;
        });
        #if UNITY_EDITOR
        using var drawGizmosListener = SubscribeDrawGizmos(m_CameraFovTracker2d.DrawGizmos);
        #endif
        using var cameraControllerSubscription = Dispatcher.global.SubscribeRegistry<CameraAnchor>(
            cameraController =>
            {
                if (cameraController == null || cameraController.camera == null)
                {
                    cameraFovTracker2d.Pause();
                    cameraFovTracker2d.camera = null;
                    return;
                }
                cameraFovTracker2d.Resume();
                cameraFovTracker2d.camera = cameraController.camera;
            });
        /* Check for already registered */
        if (Dispatcher.global.HasRegistry<FOVManager2d>())
        {
            throw new System.Exception("FOVManager already registered");
        }
        /* Register this in Dispatcher */
        using var registration = Dispatcher.global.Register(this);
        await cancellationToken;
    }
    public DisposableAction TrackArea(VirtualArea2d area, CameraFOVTracker2d.FovBoundsType boundsType, CameraFOVTracker2d.TrackAreaHandler onActivated)
    {
        return cameraFovTracker2d.TrackArea(area, boundsType, onActivated);
    }
    
    /// <summary>
    /// Track specific area once
    /// </summary>
    /// <param name="area"></param>
    /// <param name="boundsType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public UniTask<CameraFOVTracker2d.AreaIntersection> TrackAreaOneShot(VirtualArea2d area, CameraFOVTracker2d.FovBoundsType boundsType, CancellationToken cancellationToken)
    {
        var tcs = new UniTaskCompletionSource<CameraFOVTracker2d.AreaIntersection>();
        DisposableAction release = null;
        CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled();
            release?.Dispose();
        });
        var shotWasTaken = false;
        release = cameraFovTracker2d.TrackArea(area, boundsType, (intersected, intersection) =>
        {
            if (shotWasTaken)
            {
                return;
            }
            shotWasTaken = true;
            
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled();
                release?.Dispose();
                return;
            }
            registration.Dispose();
            tcs.TrySetResult(intersection);
            release?.Dispose();
        });
        return tcs.Task;
    }
    
    /// <summary>
    /// Track specific areas in the FOV edges
    /// </summary>
    /// <param name="areas"></param>
    /// <param name="onActivated"></param>
    /// <returns></returns>
    public DisposableAction TrackAreas(IEnumerable<VirtualArea2d> areas, CameraFOVTracker2d.FovBoundsType boundsType, CameraFOVTracker2d.TrackAreaHandler onActivated)
    {
        return cameraFovTracker2d.TrackAreas(areas, boundsType, onActivated);
    }
    
    /// <summary>
    /// Track specific object in the FOV
    /// </summary>
    /// <param name="t"></param>
    /// <param name="onOverlapped"></param>
    /// <returns></returns>
    public DisposableAction TrackObject(Transform t, CameraFOVTracker2d.FovBoundsType boundsType, CameraFOVTracker2d.TrackObjectHandler onChange)
    {
        return cameraFovTracker2d.TrackObject(t, boundsType, onChange);
    }
    
    /// <summary>
    /// Track specific objects in the FOV
    /// </summary>
    /// <param name="ts"></param>
    /// <param name="onOverlapped"></param>
    /// <returns></returns>
    public DisposableAction TrackObjects(IEnumerable<Transform> ts, CameraFOVTracker2d.FovBoundsType boundsType, CameraFOVTracker2d.TrackObjectHandler onChange)
    {
        return cameraFovTracker2d.TrackObjects(ts, boundsType, onChange);
    }
}

}