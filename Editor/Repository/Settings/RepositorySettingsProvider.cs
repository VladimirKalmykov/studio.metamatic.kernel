using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Threading; // Добавьте этот namespace
using Metamatic;
using UnityEngine.UIElements;

public class RepositorySettingsProvider : SettingsProvider
{
    
    private static RepositorySettings settings;
    private VisualTreeAsset m_PlatformVisualTree;
    private ScrollView m_PlatformsScrollView;
    private SequentialArrayHandler<PlatformSource> m_PlatformItemsInitializer;
    private CancellationTokenSource m_ActivationCancellationTokenSource;
    private Dictionary<RuntimePlatform, List<Type>> m_PlatformToSourcesMap;
    private SerializedObject m_SerializedSettings;
    private event Action OnDataChanged;
    public RepositorySettingsProvider(string path, SettingsScope scopes) : base(path, scopes) {}

    public override async void OnActivate(string searchContext, VisualElement rootElement)
    {
        Debug.Log("Activete RepositorySettingsProvider");
        m_ActivationCancellationTokenSource = new CancellationTokenSource();
        string directoryPath = Path.GetDirectoryName(RepositoryLegacy.SettingsPath);
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        settings = AssetDatabase.LoadAssetAtPath<RepositorySettings>(RepositoryLegacy.SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<RepositorySettings>();
            settings.platforms = new PlatformSource[] { };
            AssetDatabase.CreateAsset(settings, RepositoryLegacy.SettingsPath);
            AssetDatabase.SaveAssets();
        }
        
        /* Collect information about available sources for the platforms */
        m_PlatformToSourcesMap = FindCompatibleIOSourceClasses();
        
        /* Create platform items initializer */
        using var platformItemsInitializer = m_PlatformItemsInitializer = new SequentialArrayHandler<PlatformSource>(RenderPlatformSource);
        var root = CreateSettingsGUI();
        
        
        /* Select scroll view */
        Debug.Log("Reassign m_PlatformsScrollView");
        m_PlatformsScrollView = root.Q<ScrollView>("PlatformSources");
        Debug.Log($"m_PlatformsScrollView has been reassigned: {m_PlatformsScrollView}");
        
        /* Render platform sources */
        platformItemsInitializer.Handle(settings.platforms);
        
        rootElement.style.display = DisplayStyle.Flex;
        rootElement.style.flexDirection = FlexDirection.Column;
        rootElement.style.alignSelf = Align.Stretch;
        rootElement.style.justifyContent = Justify.SpaceBetween;
        root.style.alignSelf = Align.Stretch;
        root.style.flexGrow = 1;
        
        rootElement.Add(root);
        await m_ActivationCancellationTokenSource.Token;
    }
    
    public override void OnDeactivate()
    {
        Debug.Log("Diactivate RepositorySettingsProvider");
        m_ActivationCancellationTokenSource?.Cancel();
    }
    
    private VisualElement CreateSettingsGUI()
    {
        /* Load UXML */
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Repository/Settings/Settings.uxml");
        var root = visualTree.CloneTree();
        m_SerializedSettings = new SerializedObject(settings);
        
        /* Load Platform UXML */
        m_PlatformVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Repository/Settings/Platform.uxml");
        
        /* Select Platform Dropdown */
        var platformDropdown = root.Q<DropdownField>("PlatformDropdown");
        /* Add every enum value from RuntimePlatform to the dropdown choices */
        var choices = System.Enum.GetValues(typeof(RuntimePlatform)).Cast<RuntimePlatform>().Select(x => x.ToString()).ToList();
        /* Exclude from choices the values that are already in the settings.platfroms -> RuntimePlatform platform */
        var platformChoices = choices.Except(settings.platforms.Select(x => x.platform.ToString())).ToList();
        platformDropdown.choices = platformChoices;
        
        OnDataChanged += () =>
        {
            /* Exclude from choices the values that are already in the settings.platfroms -> RuntimePlatform platform */
            platformDropdown.choices = choices.Except(settings.platforms.Select(x => x.platform.ToString())).ToList();
        };
        
        /* Select add platform button */
        var addPlatformButton = root.Q<Button>("AddPlatformButton");
        /* Create handler for add platform button */
        addPlatformButton.clicked += () =>
        {
            /* Check that the platform doesn't exist in the settings.platforms */
            if (settings.platforms.Any(x => x.platform.ToString() == platformDropdown.value))
            {
                /* If the platform already exists, show alert */
                EditorUtility.DisplayDialog("Error", "Platform already exists", "OK");
                return;
            }
            var platform = (RuntimePlatform)System.Enum.Parse(typeof(RuntimePlatform), platformDropdown.value);
            settings.platforms = settings.platforms.Append(new PlatformSource { platform = platform }).ToArray();
            m_PlatformItemsInitializer.Handle(settings.platforms);
            SaveSettings();
        };
        
        return root;
        /*
         var property = serializedObject.GetIterator();
        property.Next(true);
        while (property.NextVisible(false))
        {
            var propertyField = new PropertyField(property);
            propertyField.Bind(serializedObject);
            root.Add(propertyField);
        }
        */
    }
    
    private async void RenderPlatformSource(PlatformSource platformSource, CancellationToken token)
    {
        var root = m_PlatformVisualTree.CloneTree();
        var platformField = root.Q<Label>("PlatformName");
        platformField.text = platformSource.platform.ToString();
        /* Select SourcesDropdown */
        var sourcesDropdown = root.Q<DropdownField>("SourcesDropdown");
        /* Add to dropdown choices the sources that are compatible with the platform */
        sourcesDropdown.choices = m_PlatformToSourcesMap.ContainsKey(platformSource.platform)
            ? m_PlatformToSourcesMap[platformSource.platform].Select(x => x.FullName).ToList()
            : new List<string>();
        sourcesDropdown.SetValueWithoutNotify(platformSource.sourceClassName);
        sourcesDropdown.RegisterValueChangedCallback(evt =>
        {
            platformSource.sourceClassName = evt.newValue;
            SaveSettings();
        });
        /* Select RemoveButton */
        var removeButton = root.Q<Button>("RemoveButton");
        removeButton.clicked += () =>
        {
            settings.platforms = settings.platforms.Where(x => x != platformSource).ToArray();
            m_PlatformItemsInitializer.Handle(settings.platforms);
            SaveSettings();
        };
        
        m_PlatformsScrollView.Add(root);
        await token;
        if (m_PlatformsScrollView.Contains(root))
        m_PlatformsScrollView.Remove(root);
    }

    [SettingsProvider]
    public static SettingsProvider CreateRepositorySettingsProvider()
    {
        var provider = new RepositorySettingsProvider("Project/Repository", SettingsScope.Project)
        {
            keywords = new[] { "Repository", "Storage", "Configuration" }
        };

        return provider;
    }
    
    private void SaveSettings()
    {
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        OnDataChanged?.Invoke();
    }
    
    public static Dictionary<RuntimePlatform, List<Type>> FindCompatibleIOSourceClasses()
    {
        var result = new Dictionary<RuntimePlatform, List<Type>>();
        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(Storage).IsAssignableFrom(type) && !type.IsAbstract);

        foreach (var type in allTypes)
        {
            var attributes = (PlatformCompatibleAttribute[])type.GetCustomAttributes(typeof(PlatformCompatibleAttribute), false);

            if (attributes.Length == 0)
            {
                foreach (var platform in Enum.GetValues(typeof(RuntimePlatform)).Cast<RuntimePlatform>())
                {
                    if (!result.ContainsKey(platform))
                    {
                        result[platform] = new List<Type>();
                    }
                    result[platform].Add(type);
                }
            }
            else
            {
                foreach (var attribute in attributes)
                {
                    if (!result.ContainsKey(attribute.platform))
                    {
                        result[attribute.platform] = new List<Type>();
                    }
                    result[attribute.platform].Add(type);
                }
            }
        }

        return result;
    }
}