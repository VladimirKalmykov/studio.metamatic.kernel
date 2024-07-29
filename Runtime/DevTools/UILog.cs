using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic.DevTools {
    /// <summary>
    /// Provides static 
    /// </summary>
    public class UILog: MonoBehaviour {
        protected enum ValueDefinitionType {
            String,
            Func
        }
        protected class ValueDefinition {
            public string value;
            public Func<object> func;
            public ValueDefinitionType type;
        }

        protected const string COMMON_SECTION_NAME = "Common";
        private static GameObject GetGo() {
            var UILogGo = GameObject.Find("__METAMATIC.UILOG__");
            if (UILogGo is { }) return UILogGo;
            if (Application.isPlaying) {
                UILogGo = UILog.InstantiateGo();
            } else {
                return null;
            }

            return UILogGo;
        }
        private static GameObject InstantiateGo() {
            var go = new GameObject("__METAMATIC.UILOG__");

            if (go != null) {
                go.AddComponent<UILog>();   
            }
            return go;
        }
        public static void Add(string key, object message) {
            if (!Application.isPlaying) {
                return;
            }
            var go = UILog.GetGo();

            if (go == null) return;
            var db = go.GetComponent<UILog>();
            db.InternalAdd(key, message);
        }
        public static DisposableAction Add(string sectionName, string key, object message) {
            if (!Application.isPlaying) {
                throw new InvalidOperationException("UILog.Add can only be called in play mode");
            }
            var go = UILog.GetGo();

            if (go == null) throw new InvalidOperationException("UILog.GetGo returned null");
            var db = go.GetComponent<UILog>();
            db.InternalAdd(sectionName, key, message);
            return DisposableAction.Create(() => {
                db.InternalRemove(sectionName, key);
            });
        }
        public static DisposableAction Add(string sectionNane, string key, Func<object> getter) {
            if (!Application.isPlaying)
            {
                return DisposableAction.Empty;
            }
            var go = UILog.GetGo();

            if (go == null) return DisposableAction.Empty;
            var db = go.GetComponent<UILog>();
            db.InternalAdd(sectionNane, key, getter);
            return DisposableAction.Create(() => {
                db.InternalRemove(sectionNane, key);
            });
        }
        public static void Add(string key, Func<object> getter) {
            if (!Application.isPlaying) {
                return;
            }
            var go = UILog.GetGo();

            if (go == null) return;
            var db = go.GetComponent<UILog>();
            db.InternalAdd(key, getter);
        }

        public static void Remove(string sectionName, string key) {
            if (!Application.isPlaying) {
                return;
            }
            var go = UILog.GetGo();

            var db = go.GetComponent<UILog>();
            db.InternalRemove(sectionName, key);
        }
        
        public static void Remove(string key) {
            if (!Application.isPlaying) {
                return;
            }
            var go = UILog.GetGo();

            var db = go.GetComponent<UILog>();
            db.InternalRemove(COMMON_SECTION_NAME, key);
        }
        
        protected class LogPair: Dictionary<string, ValueDefinition> {}
        private IDictionary<string, LogPair> m_Log = new Dictionary<string, LogPair>();
        private string _text = "";
        
        public void InternalAdd(string sectionName, string key, object message) {
            if (!m_Log.ContainsKey(sectionName))
            {
                m_Log.Add(sectionName, new LogPair());
            }

            var section = m_Log[sectionName];
            if (!section.ContainsKey(key)) {
                section.Add(key, new ValueDefinition {
                    value = "",
                    type = ValueDefinitionType.String
                });
            }
            section[key].type = ValueDefinitionType.String;
            section[key].value = message == null ? "null" : message.ToString();
        }
        
        public void InternalAdd(string sectionName, string key, Func<object> getter) {
            if (!m_Log.ContainsKey(sectionName))
            {
                m_Log.Add(sectionName, new LogPair());
            }

            var section = m_Log[sectionName];
            if (!section.ContainsKey(key)) {
                section.Add(key, new ValueDefinition {
                    value = "",
                    type = ValueDefinitionType.String
                });
            }
            section[key].type = ValueDefinitionType.Func;
            section[key].func = getter;
        }

        public void InternalAdd(string key, object message)
        {
            InternalAdd(COMMON_SECTION_NAME, key, message);
        }

        public void InternalAdd(string key, Func<object> getter) {
            InternalAdd(COMMON_SECTION_NAME, key, getter);
        }

        public void InternalRemove(string sectionName, string key)
        {
            if (!m_Log.ContainsKey(sectionName)) return;
            var section = m_Log[sectionName];
            if (!section.ContainsKey(key)) return;
            section.Remove(key);
            if (section.Count == 0)
            {
                m_Log.Remove(sectionName);
            }
        }

        public void InternalRemove(string key)
        {
            InternalRemove(COMMON_SECTION_NAME, key);
        }

        private void CompileMessages() {
            var lines = new List<string>();
            foreach (var secionPair in m_Log)
            {
                lines.Add($"<b>{secionPair.Key}</b>:");
                foreach( var item in secionPair.Value) {
                    if (item.Value.type == ValueDefinitionType.String) {
                        lines.Add($"{item.Key}: {item.Value.value}");
                    } else
                    {
                        var payload = item.Value.func();
                        lines.Add($"{item.Key}: {(payload == null ? "null" : payload.ToString())}");
                    }
                }
                lines.Add("");
            }
            
            _text = string.Join("\n ", lines.ToArray());
        }

        private void OnGUI() {
            CompileMessages();

            Draw();
            DrawShadow();
        }

        private void Draw()
        {
            var style = new GUIStyle();
            style.fontSize = Screen.height / 50;
            style.normal.textColor = Color.white;

            GUILayout.BeginArea(new Rect(10, 10, Screen.width / 2, Screen.height / 2));

            // Используем GUILayout.Label для текста с фоном
            foreach (var line in _text.Split('\n')) {
                GUILayout.Label(line);
            }

            GUILayout.EndArea();
        }
        
        private void DrawShadow()
        {
            var style = new GUIStyle();
            style.fontSize = Screen.height / 50;
            style.normal.textColor = Color.black;

            GUILayout.BeginArea(new Rect(9, 11, Screen.width / 2, Screen.height / 2));

            // Используем GUILayout.Label для текста с фоном
            foreach (var line in _text.Split('\n')) {
                GUILayout.Label(line);
            }

            GUILayout.EndArea();
        }
    }
}