namespace Metamatic {
    using UnityEngine;
#if UNITY_EDITOR
#endif

    [ExecuteAlways]
    public class DisplayMesh : MonoBehaviour
    {
        MeshFilter meshFilter;
        // Start is called before the first frame update
        void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
        }

    #if UNITY_EDITOR
        Color debug_Color = MiscUtils.HexCodeToColor("#3FD9C4");
        void OnDrawGizmos() {
            if (meshFilter != null) {
                Gizmos.color = debug_Color;
                Gizmos.DrawWireMesh(meshFilter.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
            }
        }
    #endif
    }

}