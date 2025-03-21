using System.Collections.Generic;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    [CreateAssetMenu(fileName = "Places", menuName = "Metamatic/Workshop/Places")]
    public class Places: ScriptableObject
    {
        [SerializeField] private Place[] m_Index;
        
        private readonly Dictionary<string, Place> m_Map = new Dictionary<string, Place>();

        public void Awake()
        {
            foreach (var chapter in m_Index)
            {
                m_Map.Add(chapter.id, chapter);
            }
        }
        
        public Place GetChapter(string id)
        {
            return m_Map[id];
        }
        
        public Place[] GetChapters()
        {
            return m_Index;
        }
    }
}