using UnityEngine;

namespace Metamatic.Actions
{
    public class ActionSource
    {
        public readonly Behaviour source;
        public readonly string description = "";
        public ActionSource()
        {
            
        }

        public ActionSource(Behaviour source)
        {
            this.source = source;
        }
        
        public ActionSource(string description)
        {
            this.description = description;
        }
        
        public ActionSource(Behaviour source, string description)
        {
            this.source = source;
            this.description = description;
        }

        public virtual string GetDebugMessage()
        {
            var thisType = GetType();
            var actionTypeName = thisType == typeof(ActionSource) ? "" : GetType().Name;
            var sourceTypeName = source == null ? "" : $"{source.gameObject.name}({source.GetType().Name})";
            var actionTypeText = actionTypeName == "" ? "" : $"{actionTypeName} ";
            var sourceTypeText = sourceTypeName.Length > 0 ? $"{sourceTypeName} " : "";
            return $"{(actionTypeText)}{(sourceTypeText.Length > 0 || actionTypeText.Length > 0 ? "<color=cyan> <- </color>" : "")}{(sourceTypeText)}{(description.Length > 0 ? $"{description}" : "")}";
        }
    }
}