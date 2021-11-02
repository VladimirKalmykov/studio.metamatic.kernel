namespace Metamatic {
    using System.Linq;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.IMGUI.Controls;

    class FolderViewItem: TreeViewItem {

    }

    class ResourceTreeView : TreeView
    {
        internal struct ResourceInfo {
            public string guid;
            public string localGuid;
            public string fullpath;
            public string[] pathSegments;
            public System.Type nativeType;
        }
        internal class Prop {
            public int id;
            public readonly ResourceInfo info;
            public readonly System.Type nativeType;
            public readonly string guid;
            public readonly string fullpath;
            public readonly string[] path;
            public IDictionary<string, Prop> children = new Dictionary<string, Prop>();
            public Prop parent;
            public TreeViewItem treeViewItem;
            public Prop root {
                get {
                    Prop current = this;
                    while (current.parent != null) {
                        current = current.parent;
                    }
                    return current;
                }
            }
            public int depth {
                get {
                    return this.path.Length - 1;
                }
            }
            public string name {
                get {
                    return PathRemoveExtensionName(this.path[this.path.Length - 1]);
                }
            }
            public Prop(ResourceInfo info) {
                this.info = info;
                this.guid = info.localGuid;
                this.fullpath = info.fullpath;
                this.path = info.pathSegments;
                this.nativeType = info.nativeType;
                // this.nativeType = AssetDatabase.GetMainAssetTypeAtPath(String.Join(path);
            }
            public void SetParent(Prop parent) {
                this.parent = parent;
                var root = this.root;
                var id = root.RegisterProp(this);
                treeViewItem = new TreeViewItem{id = id, depth = depth, displayName = name};
                /// <summary>
                /// Specify icon for the row
                /// </summary>
                /// <returns></returns>
                treeViewItem.icon = this.nativeType == typeof (UnityEditor.DefaultAsset) ? s_TestIcons[0] : s_TestIcons[1];
                if (parent.treeViewItem != null) {
                    parent.treeViewItem.AddChild(treeViewItem);
                }
            }
            public void AddChildren(Prop prop) {
                children.Add(prop.guid, prop);
                prop.SetParent(this);
            }
            public void Clear() {
                children.Clear();
            }
            public bool ContainsChildren(string key) {
                return children.ContainsKey(key);
            }
            public Prop GetChildren(string key) {
                return children[key];
            }
            public virtual void AddTreeViewItem(TreeViewItem item) {
                throw new System.Exception("You can add treeViewItem only to the root Prop (See RootProp class)");
            }
            public virtual int GetNewId() {
                throw new System.Exception("You can get new id only from root Prop (See RootProp class)");
            }
            public virtual int RegisterProp(Prop prop) {
                throw new System.Exception("You can register prop only in root Prop (See RootProp class)");
            }
            internal string PathRemoveExtensionName(string name) {
                var s = name.Split('.');
                if (s.Length > 1) {
                    var segment = new System.ArraySegment<string>(s, 0, s.Length - 1);
                    return string.Join("/", segment.ToArray());
                }
                return name;
            }
        }
        public class RootProp: Prop {
            public int idCounter = 0;
            private IDictionary<int, Prop> m_AllProps = new Dictionary<int, Prop>();
            public RootProp(): base(new ResourceInfo {
                nativeType = typeof(RootProp)
            }) {
                treeViewItem = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            }
            public override int GetNewId() {
                return ++idCounter;
            }
            public Prop FindProp(int id) {
                return m_AllProps[id];
            }
            public override int RegisterProp(Prop prop) {
                var id = GetNewId();
                m_AllProps[id] = prop;
                return id;
            }
        }
        private Prop rootProp = new RootProp();
        static Texture2D[] s_TestIcons =
        {
            EditorGUIUtility.FindTexture ("Folder Icon"),
            EditorGUIUtility.FindTexture ("Prefab Icon")
        };
        public ResourceTreeView(TreeViewState treeViewState)
            : base(treeViewState)
        {
            Debug.Log("Resources tree reload");
            Reload();
        }

        private void BuildStructureFromFolder(string pattern, string cut) {
            var assets = AssetDatabase.FindAssets("", new string[] { pattern });
            foreach (var guid in assets) {
                var fullpath = AssetDatabase.GUIDToAssetPath(guid);
                var path = fullpath.Substring(cut.Length);
                var pathSegments = path.Split('/');
                Prop parentProps = rootProp;

                for (var i = 0; i < pathSegments.Length; i++ ) {
                    var propPathSegments = pathSegments.Take(i + 1).ToArray();
                    var localPath = string.Join("/", propPathSegments);
                    var nativeType = AssetDatabase.GetMainAssetTypeAtPath(fullpath);
                    // var localGuid = localPath == path && nativeType != typeof(UnityEditor.DefaultAsset) ? guid : localPath;
                    var localGuid = localPath;
                    if (!parentProps.ContainsChildren(localGuid)) {
                        var prop = new Prop(new ResourceInfo {
                            guid = guid,
                            localGuid = localGuid,
                            fullpath = fullpath,
                            pathSegments = propPathSegments,
                            nativeType = nativeType
                        });
                        parentProps.AddChildren(prop);
                        parentProps = prop;
                    } else {
                        parentProps = parentProps.GetChildren(localGuid);
                    }
                }
            }
        }
            
        protected override TreeViewItem BuildRoot ()
        {
            rootProp.Clear();
            /* Find assets */
            BuildStructureFromFolder("Assets/Prefabs", "Assets/Prefabs/");
            BuildStructureFromFolder("Packages/studio.metamatic.nature/Prefabs", "Packages/studio.metamatic.nature/Prefabs/");
            BuildStructureFromFolder("Packages/studio.metamatic.nature3d/Prefabs", "Packages/studio.metamatic.nature3d/Prefabs/");
            BuildStructureFromFolder("Packages/studio.metamatic.essentials3d/Prefabs", "Packages/studio.metamatic.essentials3d/Prefabs/");
            return rootProp.treeViewItem;
        }

        protected override void RowGUI (RowGUIArgs args)
        {
            // Rect folderIconRect = args.rowRect;
            // folderIconRect.x += GetContentIndent(args.item);
            // folderIconRect.width = 25;

            // GUI.DrawTexture(folderIconRect, s_TestIcons[0], ScaleMode.ScaleToFit);

            // var nextRect = args.rowRect;
            // nextRect.x += 25;
            // args.rowRect = nextRect;

            // Text
            base.RowGUI(args);
        }

        /// <summary>
        /// Method handles selection change and gives me ability to show the selected object in the inspector
        /// </summary>
        /// <param name="selectedIds"></param>
        protected override void SelectionChanged (IList<int> selectedIds)
        {
            if (selectedIds.Count > 0) {
                var objectId = selectedIds[0];
                var prop = (rootProp as RootProp).FindProp(objectId);
                if (prop != null) {
                    Object obj = AssetDatabase.LoadAssetAtPath(prop.info.fullpath, typeof(Object));
                    Selection.activeObject = obj;
                }
            }
        }

        /// <summary>
        /// In allows to drag files
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected override bool CanStartDrag (CanStartDragArgs args)
        {
            return true;
        }

        protected override void SetupDragAndDrop (SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag ();

            var props =  SortItemIDsInRowOrder (args.draggedItemIDs)
                .Select(id => (rootProp as RootProp).FindProp(id))
                .Where(prop => prop != null && prop.nativeType == typeof(UnityEngine.GameObject))
                .ToArray();
            
            if (props.Length > 0) {
                var objects = props
                    .Select(prop => AssetDatabase.LoadAssetAtPath(prop.info.fullpath, typeof(Object)));
                DragAndDrop.objectReferences = objects.ToArray();

                string title = DragAndDrop.objectReferences.Length > 1 ? "<Multiple>" : DragAndDrop.objectReferences[0].name;
                DragAndDrop.StartDrag (title);
            }
        }
    }
}