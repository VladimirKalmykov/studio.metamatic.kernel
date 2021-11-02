namespace Metamatic {
    using System.Linq;
    using UnityEngine;
    using Sirenix.OdinInspector;
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    public class Wraparound: MonoBehaviour {
        [System.Serializable]
        public enum BoundsMode {
            Teleport,
            Barrier
        }
        [Tooltip("Register for player")]
        public GameObjectRegistry playerRegister;
        [Tooltip("Register for all objects except player")]
        public GameObjectRegistry npcRegister;
        public GameObjectRegistry cameraRegister;
        [LabelText("Walls"), Tooltip("Enable walls around")]
        public bool debug = false;

        #region Private
        bool m_Visible = false;
        public bool playerisVisible {
            get {
                return m_Visible;
            }
            set {
                bool changed = m_Visible != value;
                m_Visible = value;
                if (changed) {
                    VisibilityChange(value);
                }
            }
        }

        GameObject player;
        new Camera camera;
        Collider playerCollider;
        Renderer[] playerRenderers;
        Bounds playerBounds;
        #endregion

        #if UNITY_EDITOR
        [ShowInInspector, ReadOnly]
        public static Vector3 editor_BoundsCenter;
        #endif

        void Start() {
            playerRegister.SubscribeFirst(HandlePlayerChange);
            cameraRegister.SubscribeFirst(HandleCameraChange);
        }

        void HandlePlayerChange(UnityEngine.Object player) {
            this.player = (GameObject)player;
            if (player != null) {
                this.playerRenderers = this.player.GetComponentsInChildren<Renderer>().Where(renderer => {
                    System.Type type = renderer.GetType();
                    bool isParticle = type == typeof(ParticleSystemRenderer);
                    return !isParticle;
                }).ToArray();
            }
        }

        void HandleCameraChange(UnityEngine.Object camera) {
            this.camera = camera == null
                ? null
                : ((GameObject)camera).GetComponent<Camera>();
        }

        void FixedUpdate() {
            if (player != null) {
                playerBounds = playerRenderers[0].bounds;
                for (var i = 1; i < playerRenderers.Length; i++)
                {
                    playerBounds.Encapsulate(playerRenderers[i].bounds);
                }
                var ext = playerBounds.extents;

                float maxY = -999999, maxX = -999999, minY = 99999, minX = 99999;

                for (var x = -1; x < 2; x+=2)
                {
                    for (var y = -1; y < 2; y+=2)
                    {
                        for (var z = -1; z < 2; z+=2)
                        {
                            var vect = playerBounds.center+new Vector3(ext.x*x, ext.y*y, ext.z*z);
                            var worldPoint = camera.WorldToScreenPoint(vect);

                            maxY = Mathf.Max(maxY, worldPoint.y);
                            maxX = Mathf.Max(maxX, worldPoint.x);
                            minY = Mathf.Min(minY, worldPoint.y);
                            minX = Mathf.Min(minX, worldPoint.x);
                        }
                    }
                }

                playerisVisible = !(minY > Screen.height || minX > Screen.width || maxY < 0 || maxX < 0);

                #if UNITY_EDITOR
                editor_BoundsCenter = playerBounds.center;
                #endif
            }
        }

        void VisibilityChange(bool visible) {
            if (!visible) {
                var worldPosition = camera.WorldToScreenPoint(player.transform.position);
                var boundsWorldCenter = camera.WorldToScreenPoint(playerBounds.center);
                var diff = worldPosition - boundsWorldCenter;

                /* Spawn in another side */
                if (worldPosition.y > Screen.height || worldPosition.y < 0) {
                    worldPosition.y = Screen.height - worldPosition.y;
                }
                if (worldPosition.x > Screen.width || worldPosition.x < 0) {
                    worldPosition.x = Screen.width - worldPosition.x;
                }

                player.transform.position = camera.ScreenToWorldPoint(worldPosition);
            }
        
        }

    #if UNITY_EDITOR
        void OnDrawGizmos() {
            if (debug) {
                Gizmos.color = Color.cyan;
                Handles.color = Color.cyan;
                Handles.DotHandleCap(0, editor_BoundsCenter, Quaternion.identity, 1f, EventType.Repaint);
            }
        }
    #endif
    }
}