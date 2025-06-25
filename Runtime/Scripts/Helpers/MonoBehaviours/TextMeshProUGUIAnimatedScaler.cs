namespace Metamatic
{
    using System.Collections;
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;

    /// <summary>
    /// Allows to play scaling animation for TextMeshProUGUI. Unlike scaling of RectTransform this scale perform on vertexex level relative visual center.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextMeshProUGUIAnimatedScaler : MonoBehaviour
    {
        [Range(1f, 3f)] public float zoomFactor = 1.5f;

        [Tooltip("Общая длительность «вздутия» туда-обратно")]
        public float duration = 0.3f;

        private TextMeshProUGUI _tmp;
        private TMP_TextInfo _textInfo;
        private Vector3[][] _originalVertices;

        void Awake()
        {
            _tmp = GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Вызывайте этот метод, когда нужно «надувать» текст.
        /// </summary>
        [Button]
        public void PlayZoom()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            StopAllCoroutines();
            StartCoroutine(ZoomCoroutine());
        }

        private IEnumerator ZoomCoroutine()
        {
            // 1) Обновляем меш и кешируем оригинальные вершины
            _tmp.ForceMeshUpdate();
            _textInfo = _tmp.textInfo;

            int meshCount = _textInfo.meshInfo.Length;
            _originalVertices = new Vector3[meshCount][];
            for (int i = 0; i < meshCount; i++)
                _originalVertices[i] = (Vector3[])_textInfo.meshInfo[i].vertices.Clone();

            // 2) Анимируем «взлёт»
            float half = duration / 2f;
            yield return Animate(1f, zoomFactor, half);
            // 3) Анимируем «возврат»
            yield return Animate(zoomFactor, 1f, half);
        }

        private IEnumerator Animate(float from, float to, float time)
        {
            float t = 0f;
            while (t < time)
            {
                float scale = Mathf.Lerp(from, to, t / time);
                ApplyScale(scale);
                t += Time.deltaTime;
                yield return null;
            }

            // финальный снэп
            ApplyScale(to);
        }

        private void ApplyScale(float scale)
        {
            // для простоты пересчитываем текст каждый кадр
            _tmp.ForceMeshUpdate();
            _textInfo = _tmp.textInfo;

            // пробегаем по всем видимым символам
            for (int i = 0; i < _textInfo.characterCount; i++)
            {
                var charInfo = _textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int meshIdx = charInfo.materialReferenceIndex;
                int vertIndex = charInfo.vertexIndex;

                Vector3[] verts = _textInfo.meshInfo[meshIdx].vertices;
                Vector3[] orig = _originalVertices[meshIdx];

                // берем две противоположные вершины (нижний-левый и верхний-правый)
                Vector3 bl = orig[vertIndex + 0];
                Vector3 tr = orig[vertIndex + 2];
                // центр символа в локальных координатах
                Vector3 center = (bl + tr) * 0.5f;

                // для каждой из 4 вершин применяем scale относительно этого центра
                for (int j = 0; j < 4; j++)
                {
                    verts[vertIndex + j] = center + (orig[vertIndex + j] - center) * scale;
                }
            }

            // наконец пушим изменённые вершины в рендер
            for (int i = 0; i < _textInfo.meshInfo.Length; i++)
            {
                _tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }
        }
    }
}