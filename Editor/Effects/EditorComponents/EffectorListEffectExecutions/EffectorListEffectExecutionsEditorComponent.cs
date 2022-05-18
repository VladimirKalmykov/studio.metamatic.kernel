using Metamatic;
using Metamatic.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Effects
{
    public class EffectorListEffectExecutionsEditorComponent : EditorComponent
    {
        private Effector m_Effector;
        private ChannelsVisualElement m_ChannelsVisualElement;
        
        public EffectorListEffectExecutionsEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
            var root = this;
            m_Effector = (Effector) this.target;
            root.styleSheets.Add(this.editor.LoadRelativeAsset<StyleSheet>("./styles.uss"));

            BuildGUI(root);
        }

        private void BuildGUI(VisualElement root)
        {
            BuildChannelsGUI(root);
        }
        
        private void BuildChannelsGUI(VisualElement root)
        {
            m_ChannelsVisualElement = new ChannelsVisualElement((EffectorEdtitor)this.editor);
            root.Add(m_ChannelsVisualElement);
        }

        public override void OnEnable()
        {
            m_ChannelsVisualElement?.OnEnable();
        }
        
        public override void OnDisable()
        {
            m_ChannelsVisualElement?.OnDisable();
        }
    }
}