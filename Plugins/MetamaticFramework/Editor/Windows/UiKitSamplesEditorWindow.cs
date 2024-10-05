using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using UnityEditor;
using System;
using System.Collections.Generic;
using Metamatic.UIToolKit;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Editor
{
    public class UiKitSamplesEditorWindow: EditorWindow
    {
        public class SampleCategory
        {
            public string name { get; set; }
            public List<SampleCategory> children { get; set; }
            public Func<VisualElement> createContent { get; set; }
            
            public SampleCategory(string name, Func<VisualElement> createContent)
            {
                this.name = name;
                this.createContent = createContent;
            }
        }
        private VisualElement m_ContentContainer;
        private TreeView m_CategoriesTreeView;
        private readonly Dictionary<int, SampleCategory> m_MapIdToCategory = new Dictionary<int, SampleCategory>();
        
        [UnityEditor.MenuItem ("Metamatic/UIKit/Samples")]
        public static void ShowWindow()
        {
            GetWindow(typeof(UiKitSamplesEditorWindow), false, "Metamatic UIKit Samples");
        }
        public void CreateGUI()
        {
            // Connect uss stylesheet
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/studio.metamatic.kernel/Plugins/MetamaticFramework/Editor/Uss/MetamaticFrameworkEditor.uss"));
            rootVisualElement.AddToClassList("MetamaticFrameworkEditor");
            
            // Create two pane splitter
            var splitter = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            m_CategoriesTreeView = new TreeView();
            m_CategoriesTreeView.makeItem = () => new Label("Empty");
            m_CategoriesTreeView.bindItem = (element, id) =>
            {
                ((Label)element).text = m_MapIdToCategory[id].name;
            };
            
            m_CategoriesTreeView.selectionChanged += OnCategoriesSelectionChanged;
            
            var treeViewContainer = new VisualElement();
            treeViewContainer.Add(m_CategoriesTreeView);
            splitter.contentContainer.Add(treeViewContainer);

            m_ContentContainer = new VisualElement();
            splitter.contentContainer.Add(m_ContentContainer);
            
            // Create sample categories
            var categories = CreateSampleCategories();
            PopulateTreeView(categories);
            
            // Select first item
            m_CategoriesTreeView.SetSelection(0);
            
            rootVisualElement.Add(splitter);
        }

        private void PopulateTreeView(List<SampleCategory> categories)
        {
            // Преобразуем наши категории в формат для TreeView
            var treeItems = new List<TreeViewItemData<SampleCategory>>();
            int id = 0;
            foreach (var category in categories)
            {
                treeItems.Add(CreateTreeViewItem(category, ref id));
            }
            
            m_CategoriesTreeView.SetRootItems(treeItems);
        }
        
        private TreeViewItemData<SampleCategory> CreateTreeViewItem(SampleCategory category, ref int id)
        {
            var children = new List<TreeViewItemData<SampleCategory>>();
            var thisId = id;
            if (category.children != null)
            {
                foreach (var subCategory in category.children)
                {
                    children.Add(CreateTreeViewItem(subCategory, ref id));
                }
            }
            
            m_MapIdToCategory.Add(thisId, category);
            return new TreeViewItemData<SampleCategory>(id++, category, children);
        }

        #region Handlers
        private void OnCategoriesSelectionChanged(IEnumerable<object> selectedItems)
        {
            foreach (var item in selectedItems)
            {
                if (item is SampleCategory category)
                {
                    m_ContentContainer.Clear();
                    m_ContentContainer.Add(category.createContent());
                }
            }
        }
        #endregion
        
        #region Create samples
        public VisualElement CreateIconPacksList()
        {
            var root = new VisualElement();
            var allIcons = UIToolKitCore.GetAllIcons();
            
            foreach (var sizePack in allIcons)
            {
                var iconPack = new IconPackVisualElement(sizePack.Key, sizePack.Value);
                root.Add(iconPack);
            }

            return root;
        }

        public VisualElement CreateLayouts()
        {
            var root = new VisualElement();
            
            // Add KeyValueList
            var keyValueList = new KeyValueList();
            
            keyValueList.AddItem("Name", new Label("Vladimir"));
            keyValueList.AddItem("Age", new Label("40"));
            keyValueList.AddItem("Country", new Label("Russia"));
            
            root.Add(keyValueList);
            
            return root;
        }
        #endregion
        
        public List<SampleCategory> CreateSampleCategories()
        {
            var categories = new List<SampleCategory>();
            
            categories.Add(new SampleCategory("Icon Packs", CreateIconPacksList));
            categories.Add(new SampleCategory("Layouts", CreateLayouts));
            
            return categories;
        }
    }
}