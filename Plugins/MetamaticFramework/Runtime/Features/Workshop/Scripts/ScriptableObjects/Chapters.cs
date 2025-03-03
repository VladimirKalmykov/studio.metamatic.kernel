using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework.Workshop
{
    [CreateAssetMenu(fileName = "Chapters", menuName = "Metamatic/Workshop/Chapters")]
    public class Chapters: ScriptableObject
    {
        [SerializeField] private Chapter[] m_Index;
        
        private readonly Dictionary<string, Chapter> m_Map = new Dictionary<string, Chapter>();

        public void Awake()
        {
            foreach (var chapter in m_Index)
            {
                m_Map.Add(chapter.id, chapter);
            }
        }
        
        public Chapter GetChapter(string id)
        {
            return m_Map[id];
        }
        
        public Chapter[] GetChapters()
        {
            return m_Index;
        }
    }
}