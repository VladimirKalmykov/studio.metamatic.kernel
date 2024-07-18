namespace Metamatic
{
    using UnityEngine;
    #if UNITY_EDITOR
    using UnityEditor;
    #endif
    
    [RequireComponent(typeof(SpaceGrid))]
    [PluginFor(typeof(SpaceGrid))]
    [ExecuteAlways]
    public class SpaceGridFitScreen : MonoBehaviour
    {
        public float distance = 10f;
        public bool invertAxis;
        private SpaceGrid m_SpaceGrid;
        private Transform m_SpaceGridTransform;
        private Camera m_CachedCamera;
        private float m_CachedDistance;
        private float m_CachedScreenRation;
        
        #region Debug
        #if UNITY_EDITOR
        
        private Vector3 debug_BottomLeft;
        private Vector3 debug_TopLeft;
        private Vector3 debug_TopRight;
        private Vector3 debug_BottomRight;
        private Vector3 debug_CenterPoint;
        private Vector3 debug_DirectionToCamera;
        
        #endif
        #endregion

        private void OnEnable()
        {
            m_SpaceGrid = GetComponent<SpaceGrid>();
            m_SpaceGridTransform = m_SpaceGrid.transform;
            m_CachedCamera = Camera.main;
            if (m_CachedCamera != null) return;
            Debug.LogError("Camera is undefined");
            enabled = false;
        }
        
        #if UNITY_EDITOR
        private void Update()
        #else
        private void FixedUpdate()
        #endif
        {
            var screenRatio = (float)Screen.width / Screen.height;
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (distance == m_CachedDistance && m_CachedScreenRation == screenRatio) return;
            // ReSharper restore CompareOfFloatsByEqualityOperator
            m_CachedDistance = distance;
            m_CachedScreenRation = screenRatio;
            // var pos = m_SpaceGridTransform.position;
            // var rotation = m_SpaceGridTransform.rotation;
            var cameraPosition = m_CachedCamera.transform.position;
            var directionOfCameraFront = transform.TransformDirection(Vector3.forward.normalized).normalized;
            var centerPoint = cameraPosition + (directionOfCameraFront * distance);
            var directionToCamera = cameraPosition - centerPoint;
            var plane = new Plane(directionToCamera.normalized, centerPoint);
            
            var bottomLeft = MiscUtils.ViewportToPerspectivePoint(new Vector2(0, 0), m_CachedCamera, plane);
            var topLeft = MiscUtils.ViewportToPerspectivePoint(new Vector2(0, 1), m_CachedCamera, plane);
            var topRight = MiscUtils.ViewportToPerspectivePoint(new Vector2(1, 1), m_CachedCamera, plane);
            var bottomRight = MiscUtils.ViewportToPerspectivePoint(new Vector2(1, 0), m_CachedCamera, plane);

            if (invertAxis)
            {
                m_SpaceGrid.width = Vector3.Distance(bottomLeft, topLeft);
                m_SpaceGrid.height = Vector3.Distance(bottomLeft, bottomRight);
            }
            else
            {
                 m_SpaceGrid.width = Vector3.Distance(bottomLeft, bottomRight);
                m_SpaceGrid.height = Vector3.Distance(bottomLeft, topLeft);
            }
           
            
            #if UNITY_EDITOR
            debug_CenterPoint = centerPoint;
            debug_DirectionToCamera = directionToCamera;
            debug_BottomLeft = bottomLeft;
            debug_TopLeft = topLeft;
            debug_TopRight = topRight;
            debug_BottomRight = bottomRight;
            #endif

            /*
             * To make grid fit screen it should be faced to Camera
             */
            // var facingTopDirection = Vector3.Cross(Vector3.forward, Vector3.right);
            // var crossRightDirection = rotation * facingTopDirection.normalized;
            // var rotationAxis = Vector3.Cross(plane.normal, crossRightDirection);
            // var angle = Vector3.SignedAngle(plane.normal, crossRightDirection, rotationAxis);
            //     
            // m_SpaceGridTransform.rotation = rotation * Quaternion.AngleAxis(-angle, Quaternion.Inverse(rotation) * rotationAxis);
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (m_CachedCamera == null) return;
            Handles.color = Color.red;
            Vector3[] points =
            {
                debug_BottomLeft,
                debug_TopLeft,
                debug_TopRight,
                debug_BottomRight,
                debug_BottomLeft
            };
            Handles.DrawPolyLine(points);
            Handles.DrawWireCube(debug_CenterPoint, Vector3.one * 3f);
            Handles.color = new Color(1f, 0.48f, 0.17f);
            Handles.DrawLine(debug_CenterPoint, debug_DirectionToCamera);
            Handles.DrawWireCube(m_CachedCamera.transform.position, Vector3.one * 2f);
        }
        #endif
    }

}