using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Metamatic.DevTools {
    /// <summary>
    /// Provides static 
    /// </summary>
    public class UILog: MonoBehaviour {
        private enum ValueDefinitionType {
            String,
            Func
        }
        private class ValueDefinition {
            public string value;
            public Func<object> func;
            public ValueDefinitionType type;
        }
        private static GameObject GetGo() {
            GameObject UILogGo = GameObject.Find("DarkMatter__UILog__Singleton");
            if (UILogGo is null) {
                if (Application.isPlaying) {
                    UILogGo = UILog.InstantiateGo();
                } else {
                    return null;
                }
            }

            return UILogGo;
        }
        private static GameObject InstantiateGo() {
            GameObject go = new GameObject("DarkMatter__UILog__Singleton");

            if (go != null) {
                go.AddComponent<UILog>();   
            }
            return go;
        }
        public static void Add(string key, object message) {
            if (!Application.isPlaying) {
                return;
            }
            GameObject go = UILog.GetGo();

            if (go != null) {
                UILog db = go.GetComponent<UILog>();
                db.InternalAdd(key, message);
            }
        }

        public static void Add(string key, Func<object> getter) {
            if (!Application.isPlaying) {
                return;
            }
            GameObject go = UILog.GetGo();

            if (go != null) {
                UILog db = go.GetComponent<UILog>();
                db.InternalAdd(key, getter);
            }
        }

        public static void Remove(string key) {
            if (!Application.isPlaying) {
                return;
            }
            GameObject go = UILog.GetGo();

            UILog db = go.GetComponent<UILog>();
            db.InternalRemove(key);
        }

        private IDictionary<string, ValueDefinition> _keyvalues = new Dictionary<string, ValueDefinition>();
        private string _text = "";

        public void InternalAdd(string key, object message) {
            if (!_keyvalues.ContainsKey(key)) {
                _keyvalues.Add(key, new ValueDefinition {
                    value = "",
                    type = ValueDefinitionType.String
                });
            }
            _keyvalues[key].type = ValueDefinitionType.String;
            _keyvalues[key].value = message.ToString();
        }

        public void InternalAdd(string key, Func<object> getter) {
            if (!_keyvalues.ContainsKey(key)) {
                _keyvalues.Add(key, new ValueDefinition {
                    func = getter,
                    type = ValueDefinitionType.Func
                });
            }
            _keyvalues[key].type = ValueDefinitionType.Func;
            _keyvalues[key].func = getter;
        }

        public void InternalRemove(string key) {
            _keyvalues.Remove(key);
        }

        private void CompileMessages() {
            List<string> lines = new List<string>();
            foreach( KeyValuePair<string, ValueDefinition> item in _keyvalues.ToList<KeyValuePair<string, ValueDefinition>>()) {
                if (item.Value.type == ValueDefinitionType.String) {
                    lines.Add($"{item.Key}: {item.Value.value}");
                } else {
                    lines.Add($"{item.Key}: {item.Value.func().ToString()}");
                }
            }
            _text = String.Join("\n ", lines.ToArray());
        }

        void OnGUI() {
            CompileMessages();
            GUIStyle style = new GUIStyle();
            style.fontSize = Screen.height / 50;
            style.normal.textColor = Color.HSVToRGB(0.48f, 0.26f, 1f);
            GUILayout.BeginArea(new Rect(10, 10, Screen.width / 2, Screen.height / 2));
            GUILayout.Label(_text, style);
            GUILayout.EndArea();
        }
    }
}