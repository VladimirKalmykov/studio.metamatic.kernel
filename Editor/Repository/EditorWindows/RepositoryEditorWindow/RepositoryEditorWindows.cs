using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEngine.UIElements.PopupWindow;
using TreeView = UnityEngine.UIElements.TreeView;

namespace Metamatic
{
    class CreateLayerEditorWindow : EditorWindow
    {
        public string value { get; private set; } = "";

        public string errorMsg
        {
            set => EditorUtility.DisplayDialog("Error", value, "Ok");
        }
        public Func<bool> ConfirmInput;
        private void OnEnable()
        {
            // Настройка размера и положения всплывающего окна
            minSize = new Vector2(250, 150);
            maxSize = new Vector2(250, 150);
            
            VisualElement root = rootVisualElement;
            TextField nameField = new TextField("Name:") { value = value };
            nameField.RegisterValueChangedCallback(evt => value = evt.newValue);
            root.Add(nameField);

            Button okButton = new Button(() =>
            {
                var response = ConfirmInput?.Invoke();
                if (response != null && response.Value)
                {
                    Close();
                }
            }) { text = "Ok" };
            root.Add(okButton);

            Button cancelButton = new Button(Close) { text = "Cancel" };
            root.Add(cancelButton);
        }
    }
    public class RepositoryEditorWindows: EditorWindow
    {
        private RepositoryBehaviour m_RepositoryInstance;
        private TreeView m_LayersTreeView;
        private RepositoryLayer m_SelectedLayer;
        private VisualElement m_Content;
        private ExclusiveExecution<RepositoryLayer> m_LayerExecution;
        [MenuItem("Window/Repository/Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<RepositoryEditorWindows>();
            window.titleContent = new GUIContent("Repository manager");
        }

        private void CreateGUI()
        {
            var markup = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Repository/EditorWindows/RepositoryEditorWindow/RepositoryEditorWindow.uxml");
            var styles = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/studio.metamatic.kernel/Editor/Repository/EditorWindows/RepositoryEditorWindow/RepositoryEditorWindow.uss");
            markup.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(styles);
            rootVisualElement.AddToClassList("RepositoryEditorWindow");
            
            /* Layers navigation tree */
            m_LayersTreeView = rootVisualElement.Q<TreeView>("LayersTreeView");
            // Получение данных
            var data = RepositoryLegacy.instance.layers; // List<IRepositoryDataLayer>
            // Создание и добавление TreeViewItem в TreeView
            var rootItems = treeRoots;
            m_LayersTreeView.makeItem = () => new Label();
            m_LayersTreeView.bindItem = (element, i) => {
                var item = treeRoots[i];
                ((Label)element).text = item.data.name;
            };
            m_LayersTreeView.SetRootItems(treeRoots);
            m_LayersTreeView.selectionChanged += selection =>
            {
                if (selection.Count() > 0)
                {
                    m_LayerExecution?.Handle(selection.First() as RepositoryLayer);
                }
                else
                {
                    m_LayerExecution?.Handle(null);
                }
            };
            
            /* Create layer button */
            var createLayerButton = rootVisualElement.Q<Button>("CreateLayerButton");
            createLayerButton.clicked += ShowCreateLayerPopup;
            
            /* Content */
            m_Content = rootVisualElement.Q<VisualElement>("Content");
            m_LayerExecution?.Handle(null);
        }

        private async void SetSelectedLayer(RepositoryLayer layer, CancellationToken token)
        {
            m_SelectedLayer = layer;
            m_Content.Clear();
            
            if (layer == null)
            {
                return;
            }
            else
            {
                /* Load layer view from LayerInspector.uxml */
                var markup = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Repository/EditorWindows/RepositoryEditorWindow/LayerInspector.uxml");
                markup.CloneTree(m_Content);
                
                /* Set layer name */
                var layerName = m_Content.Q<Label>("Title");
                layerName.text = layer.name;
                
                /* Get layer content */
                var layerContent = m_Content.Q<VisualElement>("LayerContent");
                
                /* Subscribe for stores */
                using (var layerStoreChangeHandler =
                       new ExclusiveHandler((cancellationToken) => OnLayerStoresChange(cancellationToken, layerContent, layer), token))
                using (new DisposableCallback(layer.SubscribeStoresChanged(layerStoreChangeHandler.Handle)))
                {
                    layerStoreChangeHandler.Handle();
                    await token.AsTask();
                }
            }
        }

        /// <summary>
        /// React to layer stores change and update layer content view
        /// </summary>
        /// <param name="token"></param>
        /// <param name="context"></param>
        /// <param name="layerContent"></param>
        private void OnLayerStoresChange(CancellationToken token, VisualElement layerContent, RepositoryLayer layer)
        {
            layerContent.Clear();
            layer.stores.ForEach(pair =>
            {
                var type = pair.Key;
                var namedStores = pair.Value;
                var foldout = new Foldout() { text = $"<{type.Name}> ({namedStores.Count})" };
                namedStores.ForEach(namedStore =>
                {
                    var name = namedStore.Key;
                    var label = new Label() { text = name };
                    foldout.Add(label);
                });
                layerContent.Add(foldout);
            });
        }
        
        private void OnEnable()
        {
            m_LayerExecution = new ExclusiveExecution<RepositoryLayer>(SetSelectedLayer, default);
            m_RepositoryInstance = RepositoryLegacy.instance;
            m_RepositoryInstance.OnLayerAdded += HandleLayerAdded;
            m_RepositoryInstance.OnLayerRemoved += HandleLayerRemoved;
            m_RepositoryInstance.OnLayerMerged += HandleLayerMerged;
        }

        private void OnDisable()
        {
            m_LayerExecution.Dispose();
            m_RepositoryInstance.OnLayerAdded -= HandleLayerAdded;
            m_RepositoryInstance.OnLayerRemoved -= HandleLayerRemoved;
            m_RepositoryInstance.OnLayerMerged -= HandleLayerMerged;
        }

        private void ShowCreateLayerPopup()
        {
            CreateLayerEditorWindow editorWindow = CreateInstance<CreateLayerEditorWindow>();
            editorWindow.ConfirmInput = () =>
            {
                if (string.IsNullOrEmpty(editorWindow.value))
                {
                    editorWindow.errorMsg = "Name cannot be empty";
                    return false;
                }
                if (m_RepositoryInstance.layers.Any(layer => layer.name == editorWindow.value))
                {
                    editorWindow.errorMsg = "Layer with the same name already exists";
                    return false;
                }
                /* Create layer */
                m_RepositoryInstance.CreateLayer(editorWindow.value);
                return true;
            };
            editorWindow.ShowUtility();
        }

        

        private void HandleLayerAdded(RepositoryLayer layer)
        {
            RefreshLayersTreeView();
        }

        private void HandleLayerRemoved(RepositoryLayer layer)
        {
            RefreshLayersTreeView();
        }

        private void HandleLayerMerged(RepositoryLayer fromLayer, RepositoryLayer toLayer)
        {
            RefreshLayersTreeView();
        }
        
        private void RefreshLayersTreeView()
        {
            var rootItems = treeRoots;
            m_LayersTreeView.bindItem = (element, i) => {
                var item = treeRoots[i];
                ((Label)element).text = item.data.name;
            };
            m_LayersTreeView.SetRootItems(treeRoots);
            m_LayersTreeView.RefreshItems();
        }

        protected static IList<TreeViewItemData<RepositoryLayer>> treeRoots
        {
            get
            {
                int id = 0;
                var layers = RepositoryLegacy.instance.layers;
                var roots = new List<TreeViewItemData<RepositoryLayer>>(layers.Count);
                foreach (var layer in RepositoryLegacy.instance.layers)
                {
                    
                    roots.Add(new TreeViewItemData<RepositoryLayer>(id++, layer, GetChildren(layer.children)));
                }
                return roots;
            }
        }
        
        private static List<TreeViewItemData<RepositoryLayer>> GetChildren(IReadOnlyList<RepositoryLayer> layers)
        {
            int id = 0;
            var children = new List<TreeViewItemData<RepositoryLayer>>(layers.Count);
            foreach (var child in layers)
            {
                children.Add(new TreeViewItemData<RepositoryLayer>(++id, child, child.children.Count > 0 ? GetChildren(child.children) : null));
            }
            return children;
        }
    }
}