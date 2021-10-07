using System;
using UnityEngine;

namespace Metamatic {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class InspectorDescriptionAttribute: Attribute {
        public string text {
            get; private set;
        } = "";
        public Color color {
            get; private set;
        }
        public InspectorDescriptionAttribute(string text) {
            this.text = text;
        }
        public InspectorDescriptionAttribute(string text, Color color) {
            this.text = text;
            this.color = color;
        }
    }
}