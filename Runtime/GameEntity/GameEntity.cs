namespace Metamatic {
    using System;
    using UnityEngine;

    /// <markdown path="Packages/studio.metamatic.kernel/Documentation/GameEntity.md" />
    [DisallowMultipleComponent]
    abstract public class GameEntity: MonoBehaviour {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class SummaryAttribute: Attribute {
            public string title {
                get; private set;
            } = "";
            public string description {
                get; private set;
            } = "";
            public SummaryAttribute(string title, string description = "") {
                this.title = title;
                this.description = description;
            }
        }
        public class PictureAttribute: Attribute {
            public string path = "";
            public bool relative = false;
            public PictureAttribute(string path, bool relative = false) {
                this.path = path;
                this.relative = relative;
            }
        }
        public class MarkdownGuideAssetPath: Attribute {
            public string path = "";
            public bool relative = false;

            public MarkdownGuideAssetPath(string path, bool relative = false) {
                this.path = path;
                this.relative = relative;
            }
        }

        [HideInInspector]
        public string entityName = "";
        [HideInInspector]
        public string description = "";
        [HideInInspector]
        public string guidePath = "";
        [HideInInspector]
        public string url = "";
    }
}