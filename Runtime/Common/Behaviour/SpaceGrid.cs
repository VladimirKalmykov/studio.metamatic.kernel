namespace Metamatic {
    using UnityEngine;
    using Sirenix.OdinInspector;

    /// <summary>
    /// Draws a grid in the space and adds each children to the each cell
    /// </summary>
    [ExecuteAlways]
    public class SpaceGrid : PluggableMonoBehaviour
    {
        [BoxGroup("Main")]
        public int colsCount = 2;
        [BoxGroup("Main")]
        public int rowsCount = 2;
        [BoxGroup("Main")]
        public float width = 4f;
        [BoxGroup("Main")]
        public float height = 4f;
        [BoxGroup("Dev")]
        public Color gizmos_Color = Color.green;
        // Start is called before the first frame update
        void Start()
        {
            
        }

        #if UNITY_EDITOR
        void Update() {
            if (!Application.isPlaying) {
                UpdateChildrenPosition();
            }
        }
        #endif

        // Update is called once per frame
        void FixedUpdate()
        {
            UpdateChildrenPosition();
        }

        void UpdateChildrenPosition() {
            var colSize = width / colsCount;
            var rowSize = height / rowsCount;

            byte x = 0;
            byte y = 0; //
            foreach(Transform t in transform) {
                t.localPosition = new Vector3(
                    (colSize / 2f) + (x * colSize) - (width / 2f),
                    (height / 2f) - ((rowSize / 2f) + (y * rowSize)),
                    0f
                );
                x++;
                if (x > colsCount - 1) {
                    x = 0;
                    y++;
                }
                if (y > rowsCount - 1) {
                    y = 0;
                }
            }
        }

        #if UNITY_EDITOR
        public void OnDrawGizmosSelected() {
            Gizmos.color = gizmos_Color;
            var colSize = width / colsCount;
            var rowSize = height / rowsCount;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;

            /* Top border */
            Gizmos.DrawLine(
                new Vector3(
                    -(width / 2f),
                    (height / 2f),
                    0f
                ),
                new Vector3(
                    (width / 2f),
                    (height / 2f),
                    0f
                )
            );

            /* Right border */
            Gizmos.DrawLine(
                new Vector3(
                    (width / 2f),
                    (height / 2f),
                    0f
                ),
                new Vector3(
                    (width / 2f),
                    -(height / 2f),
                    0f
                )
            );

            /* Bottom border */
            Gizmos.DrawLine(
                new Vector3(
                    (width / 2f),
                    -(height / 2f),
                    0f
                ),
                new Vector3(
                    -(width / 2f),
                    -(height / 2f),
                    0f
                )
            );

            /* Left border */
            Gizmos.DrawLine(
                new Vector3(
                    -(width / 2f),
                    -(height / 2f),
                    0f
                ),
                new Vector3(
                    -(width / 2f),
                    (height / 2f),
                    0f
                )
            );
            
            /* Cols */
            for (var i = 1; i <= colsCount; i++) {
                Gizmos.DrawLine(
                    new Vector3(
                        ((width / colsCount) * i) - (width / 2f),
                        -(height / 2f),
                        0f
                    ),
                    new Vector3(
                        ((width / colsCount) * i) - (width / 2f),
                        (height / 2f),
                        0f
                    )
                );
            }

            /* Rows */
            for (var i = 1; i <= rowsCount; i++) {
                Gizmos.DrawLine(
                    new Vector3(
                        -(width / 2f),
                        ((height / rowsCount) * i ) - (height / 2f),
                        0f
                    ),
                    new Vector3(
                        (width / 2f),
                        ((height / rowsCount) * i ) - (height / 2f),
                        0f
                    )
                );
            }
        }
        #endif
    }

}