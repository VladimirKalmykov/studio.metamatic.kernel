namespace Metamatic {
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;

    public class ResourceTreeEditorWindow : EditorWindow
    {
        [SerializeField] TreeViewState m_TreeViewState;
        ResourceTreeView m_ResourceTreeView;
        SearchField m_SearchField;
        

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/Resource tree")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            ResourceTreeEditorWindow window = (ResourceTreeEditorWindow)EditorWindow.GetWindow(typeof(ResourceTreeEditorWindow));
            window.titleContent = new GUIContent("Resources tree");

            window.Show();
        }

        void OnEnable ()
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState ();
            m_ResourceTreeView = new ResourceTreeView(m_TreeViewState);

            m_SearchField = new SearchField ();
            m_SearchField.downOrUpArrowKeyPressed += m_ResourceTreeView.SetFocusAndEnsureSelectedItem;
        }

        void OnGUI()
        {
            DoToolbar();
            DoTreeView();
        }

        void DoToolbar()
        {
            GUILayout.BeginHorizontal (EditorStyles.toolbar);
            GUILayout.Space (100);
            GUILayout.FlexibleSpace();
            m_ResourceTreeView.searchString = m_SearchField.OnToolbarGUI (m_ResourceTreeView.searchString);
            GUILayout.EndHorizontal();
        }

        void DoTreeView()
        {
            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
        m_ResourceTreeView.OnGUI(rect);
        }
    }
}