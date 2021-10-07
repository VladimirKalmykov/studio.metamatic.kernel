using System;

namespace Metamatic {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireEditorComponentAttribute: Attribute {
        public Type componentType {
            get; private set;
        }
        public int order {
            get; private set;
        } = 1;
        public RequireEditorComponentAttribute(Type componentType, int order = 1) {
            this.componentType = componentType;
            this.order = order;
        }
    }
}