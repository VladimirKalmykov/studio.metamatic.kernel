using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

namespace Metamatic.DevTools
{
    public class SceneGizmos : MonoBehaviour
    {
        private static SceneGizmos m_Instance;
        private static bool m_IsInstantiated;

        public static SceneGizmos instance
        {
            get
            {
                if (!m_IsInstantiated)
                {
                    Instantiate();
                }

                return m_Instance;
            }
        }
        private static void Instantiate() {
            if (!Application.isPlaying)
            {
                Console.Error("SceneGizmos", "Can only be instantiated in play mode");
                return;
            }
            var go = new GameObject("__GIZMOS__");
            m_Instance = go.AddComponent<SceneGizmos>();
            m_IsInstantiated = true;
            
            /* DontDestroyOnLoad is needed for this component */
            DontDestroyOnLoad(go);
        }

        public class Gizmo
        {
            public Color color = Color.white;
            public delegate void Drawer(Gizmo gizmo);
            public Drawer drawer;
            public float duration = 1f;
            public float startTime;
            public float ratio;
            public float elapsed;

            public Gizmo(Drawer drawer, Color color = default, float duration = 1f)
            {
                this.color = color;
                this.drawer = drawer;
                this.duration = duration;
                this.startTime = Time.time;
            }

            public void Draw()
            {
                #if UNITY_EDITOR
                Handles.color = color;
                elapsed = Time.time - startTime;
                ratio = Mathf.Clamp(elapsed / duration, 0f, 1f);
                this.drawer(this);
                #endif
            }
        }

        private List<Gizmo> gizmos = new List<Gizmo>();

        public static void AddLines(Vector3[] lines, Color color = default, float duration = 1f)
        {
            #if UNITY_EDITOR
            instance.gizmos.Add(new Gizmo(gizmo =>
            {
                Handles.DrawLines(lines);
            }, color, duration));
            #endif
        }
        
        public static DisposableAction AddGizmoRenderer(Gizmo.Drawer drawer, Color color = default, float duration = 1f)
        {
            #if UNITY_EDITOR
            var gizmoRenderer = new Gizmo(drawer, color, duration);
            instance.gizmos.Add(gizmoRenderer);

            return new DisposableAction(() =>
            {
                instance.gizmos.Remove(gizmoRenderer);
            });
            #else
            return new DisposableAction(() => {});
            #endif
        }

        public static DisposableAction AddRay(Vector3 position, Vector3 direction, float distance, Color color = default, float duration = 1f)
        {
            #if UNITY_EDITOR
            var endPosition = position + (direction.normalized * distance);
            var gizmoRenderer = new Gizmo(gizmo =>
            {
                Handles.DrawDottedLine(position, endPosition, 5f);
            }, color, duration);
            instance.gizmos.Add(gizmoRenderer);

            return new DisposableAction(() =>
            {
                instance.gizmos.Remove(gizmoRenderer);
            });
            #else
            return new DisposableAction(() => {});
            #endif
        }

        public static void AddWireDisk(Vector3 position, float radius, Vector3 normal = default, Color color = default, float duration = 1f)
        {
            #if UNITY_EDITOR
            if (normal.magnitude == 0)
            {
                normal = Vector3.forward;
            }
            instance.gizmos.Add(new Gizmo(gizmo =>
            {
                Handles.DrawWireDisc(position, normal, radius);
            }, color, duration));
            #endif
        }
        
        public static void AddDropDisk(Vector3 position, float maxRadius, Vector3 normal = default, Color color = default, float duration = 1f)
        {
            if (normal.magnitude == 0)
            {
                normal = Vector3.forward;
            }
            instance.gizmos.Add(new Gizmo(gizmo =>
            {
                #if UNITY_EDITOR
                var dropRatio = Mathf.Clamp01(gizmo.elapsed / 0.65f);
                Handles.DrawSolidDisc(position, normal, Mathf.Lerp(0f, maxRadius, gizmo.ratio < 0.5f ? Easing.OutCirc(gizmo.ratio * 2f) : Easing.InCirc(1f - ((gizmo.ratio - 0.5f) * 2f))));
                Handles.color = new Color(gizmo.color.r, gizmo.color.g, gizmo.color.b, 1f - dropRatio);
                Handles.DrawWireDisc(position, normal, Mathf.Lerp(0f, maxRadius * 5f, Easing.InSine(dropRatio)));
                Handles.DrawWireDisc(position, normal, Mathf.Lerp(0f, maxRadius * 7f, Easing.InSine(dropRatio)));
                Handles.DrawWireDisc(position, normal, Mathf.Lerp(0f, maxRadius * 9f, Easing.InSine(dropRatio)));
                #endif
            }, color, duration));
        }
        
        #if UNITY_EDITOR
        private void FixedUpdate()
        {
            foreach (var gizmo in gizmos.ToArray())
            {
                if (gizmo.startTime + gizmo.duration < Time.time)
                {
                    gizmos.Remove(gizmo);
                }
            }
        }
        #endif

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            foreach (var gizmo in gizmos.ToArray())
            {
                gizmo.Draw();
            }
        }
        #endif
    }
}