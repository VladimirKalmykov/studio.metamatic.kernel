namespace Metamatic
{
    using UnityEngine;
    using System.Collections.Generic;

    public static class BeveledPathBuilder
    {
        /// <summary>
        /// Строит обходную кривую фасочного прямоугольника с филетами.
        /// </summary>
        /// <param name="r">Исходный прямоугольник.</param>
        /// <param name="bevel">Размер фаски для 4 углов: TL, TR, BR, BL.</param>
        /// <param name="bevelRadius">Радиус скругления фаски (дуга вместо прямой).</param>
        /// <param name="cornerRadius">Радиус скругления углов при стыке фаски и стороны.</param>
        /// <param name="segments">Число сегментов для всех дуг.</param>
        public static List<Vector2> GenerateBeveledPath(
            Rect r,
            Vector4 bevel,
            float bevelRadius,
            float cornerRadius,
            int segments = 6)
        {
            var pts = new List<Vector2>( /* ≈ 4*(segments*2+4) */);

            // Четыре угла и направления обхода (по часовой стрелке):
            Vector2[] corners =
            {
                new Vector2(r.xMin, r.yMax), // TL
                new Vector2(r.xMax, r.yMax), // TR
                new Vector2(r.xMax, r.yMin), // BR
                new Vector2(r.xMin, r.yMin), // BL
            };
            // Направления от угла вдоль сторон (соответственно TL→право, ↓; TR→вниз, ← и т.д.)
            Vector2[] dirA = { Vector2.right, Vector2.down, Vector2.left, Vector2.up };
            Vector2[] dirB = { Vector2.up * -1, Vector2.right * -1, Vector2.down * -1, Vector2.left * -1 };

            // Для каждого из 4 углов:
            for (int i = 0; i < 4; i++)
            {
                Vector2 C = corners[i];
                Vector2 A = dirA[i]; // вдоль первой прилегающей стороны
                Vector2 B = dirB[i]; // вдоль второй
                float b = i == 0 ? bevel.x
                    : i == 1 ? bevel.y
                    : i == 2 ? bevel.z
                    : bevel.w;

                // точки отступа от угла по сторонам
                Vector2 P1 = C + A * b;
                Vector2 P2 = C + B * b;

                // --- 1) скругление на стыке стороны и фаски (cornerRadius) ---

                if (cornerRadius > 0f)
                {
                    // точка начала дуги (по стороне A)
                    Vector2 Q1 = P1 - A * cornerRadius;
                    pts.Add(Q1);

                    // дуга от Q1 до точки R1
                    AddArc(
                        pts,
                        center: P1,
                        radius: cornerRadius,
                        fromDir: A,
                        toDir: (P1 - C).normalized, // направление к фаске
                        segs: segments
                    );
                }
                else
                {
                    // без скругления – сразу точка P1
                    pts.Add(P1);
                }

                // --- 2) фаска: либо прямая, либо дуга (bevelRadius) ---

                if (bevelRadius > 0f && bevelRadius < b)
                {
                    // точки начала/конца дуги фаски
                    Vector2 R1 = C + A * bevelRadius;
                    Vector2 R2 = C + B * bevelRadius;

                    pts.Add(R1);
                    AddArc(
                        pts,
                        center: C,
                        radius: bevelRadius,
                        fromDir: (P1 - C).normalized,
                        toDir: (P2 - C).normalized,
                        segs: segments
                    );
                    pts.Add(R2);
                }
                else
                {
                    // просто переход от P1 к P2
                    pts.Add(P2);
                }

                // --- 3) скругление на стыке фаски и стороны B (cornerRadius) ---

                if (cornerRadius > 0f)
                {
                    // точка начала дуги на стороне B
                    Vector2 Q2 = P2 - B * cornerRadius;
                    AddArc(
                        pts,
                        center: P2,
                        radius: cornerRadius,
                        fromDir: (P2 - C).normalized,
                        toDir: B,
                        segs: segments
                    );
                    pts.Add(Q2);
                }
                else
                {
                    // без скругления – уже добавили P2 выше
                }
            }

            return pts;
        }

        /// <summary>
        /// Добаляет в pts точки дуги от fromDir до toDir (по часовой), центр – center.
        /// </summary>
        static void AddArc(
            List<Vector2> pts,
            Vector2 center,
            float radius,
            Vector2 fromDir,
            Vector2 toDir,
            int segs)
        {
            float a0 = Mathf.Atan2(fromDir.y, fromDir.x);
            float a1 = Mathf.Atan2(toDir.y, toDir.x);
            // Убедимся, что идём по часовой: если разница положительна, вычесть 2π
            float delta = a1 - a0;
            if (delta > 0) delta -= Mathf.PI * 2;

            for (int i = 1; i <= segs; i++)
            {
                float t = (float)i / (segs + 1);
                float a = a0 + delta * t;
                pts.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
        }
    }
}