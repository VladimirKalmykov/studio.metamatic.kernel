using System;

namespace Metamatic {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class InspectorPictureAttribute: Attribute {
        public string assetPath {
            get; private set;
        } = "";
        public string extension {
            get; private set;
        }
        public InspectorPictureAttribute(string assetPath) {
            this.assetPath = assetPath;
        }
        public InspectorPictureAttribute(string assetPath, string extension) {
            this.assetPath = assetPath;
            this.extension = extension;
        }
    }
}