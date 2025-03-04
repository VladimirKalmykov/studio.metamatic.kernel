using Cysharp.Threading.Tasks;

namespace Metamatic
{
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using MicroBehaviours;

/// <summary>
/// Service allows you to track objects in and out of the camera FOV
/// </summary>
[RequireService(typeof(CameraService))]
public class FOV2DService : Service
{
    [SerializeField] private float m_OuterBuffer = 1.0f;
    [SerializeField] private float m_InnerBuffer = 1.0f;
    
    private CameraFOVTracker2d m_CameraFovTracker2d;
    public CameraFOVTracker2d cameraFovTracker2d => m_CameraFovTracker2d;
    private List<DisposableAction> m_Trackers = new List<DisposableAction>(100);

    protected override async UniTask RunService(Ready ready, Signal signal)
    {
        /* Subscribe to camera controller */
        var cameraService = await Resolve<CameraService>();
        /* Wait for camera */
        await cameraService.WaitForCamera(signal);
        await using var disposableCameraFovTracker = m_CameraFovTracker2d = new CameraFOVTracker2d(cameraService.camera, m_InnerBuffer, m_OuterBuffer, transform.position.z);
        using var cameraListener = cameraService.SubscribeToMainCamera(cameraController =>
        {
            if (cameraController == null || cameraController.camera == null)
            {
                m_CameraFovTracker2d.Pause();
                m_CameraFovTracker2d.camera = null;
                return;
            }
            m_CameraFovTracker2d.Resume();
            m_CameraFovTracker2d.camera = cameraController.camera;
        });
        using var validationListener = SubscribeValidate(() =>
        {
            m_CameraFovTracker2d.innerBuffer = m_InnerBuffer;
            m_CameraFovTracker2d.outerBuffer = m_OuterBuffer;
        });
#if UNITY_EDITOR
        using var drawGizmosListener = SubscribeDrawGizmos(m_CameraFovTracker2d.DrawGizmos);
#endif
        using var cameraControllerSubscription = Dispatcher.global.SubscribeRegistry<CameraAnchor>(
            (cameraController, signal) =>
            {
                // TODO: This handler need to be refactored according to the new dispatcher register logic (that uses cancellation signal instead of passing default value)
                if (cameraController == null || cameraController.camera == null)
                {
                    disposableCameraFovTracker.Pause();
                    disposableCameraFovTracker.camera = null;
                }
                else
                {
                    disposableCameraFovTracker.Resume();
                    disposableCameraFovTracker.camera = cameraController.camera;
                }

                return new UniTaskVoid();
            });
        /* Check for already registered */
        if (Dispatcher.global.HasRegistry<FOV2DService>())
        {
            throw new System.Exception("FOVManager already registered");
        }
        /* Register this in Dispatcher */
        using var registration = Dispatcher.global.Register(this);
        await ready();
        /* Untrack all */
        m_Trackers.ForEach(t => t.Dispose());
    }

    public DisposableAction TrackArea(IArea2d area, CameraFOVTracker2d.FovBoundsType boundsType, CameraFOVTracker2d.TrackAreaHandler onActivated)
    {
        var untrack = cameraFovTracker2d.TrackArea(area, boundsType, onActivated);
        m_Trackers.Add(untrack);
        return DisposableAction.Create(() =>
        {
            m_Trackers.Remove(untrack);
            untrack.Dispose();
        });
    }
    
    /// <summary>
    /// Track specific area once
    /// </summary>
    /// <param name="area"></param>
    /// <param name="boundsType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public UniTask<CameraFOVTracker2d.AreaIntersection> TrackAreaOneShot(IArea2d area, CameraFOVTracker2d.FovBoundsType boundsType, CancellationToken cancellationToken)
    {
        var tcs = new UniTaskCompletionSource<CameraFOVTracker2d.AreaIntersection>();
        DisposableAction release = default;
        CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled();
            release.Dispose();
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
                release.Dispose();
                return;
            }
            registration.Dispose();
            tcs.TrySetResult(intersection);
            release.Dispose();
        });
        return tcs.Task;
    }
    
    /// <summary>
    /// Track specific areas in the FOV edges
    /// </summary>
    /// <param name="areas"></param>
    /// <param name="onActivated"></param>
    /// <returns></returns>
    public DisposableAction TrackAreas(IEnumerable<IArea2d> areas, CameraFOVTracker2d.FovBoundsType boundsType, CameraFOVTracker2d.TrackAreaHandler onActivated)
    {
        var untrack = cameraFovTracker2d.TrackAreas(areas, boundsType, onActivated);
        m_Trackers.Add(untrack);
        return DisposableAction.Create(() =>
        {
            m_Trackers.Remove(untrack);
            untrack.Dispose();
        });
    }
    
    /// <summary>
    /// Track specific object in the FOV
    /// </summary>
    /// <param name="t"></param>
    /// <param name="onOverlapped"></param>
    /// <returns></returns>
    public DisposableAction TrackObject(Transform t, CameraFOVTracker2d.FovBoundsType boundsType, CameraFOVTracker2d.TrackObjectHandler onChange)
    {
        var untrack = cameraFovTracker2d.TrackObject(t, boundsType, onChange);
        m_Trackers.Add(untrack);
        return DisposableAction.Create(() =>
        {
            m_Trackers.Remove(untrack);
            untrack.Dispose();
        });
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