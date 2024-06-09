using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Metamatic.Extensions
{
    public enum SpriteSkinPointsGenerationMode
    {
        Random,
        Matrix
    }
    public static class SpriteSkinExtensions
    {
        /// <summary>
        /// Generate points within the convex hull of the sprite skin.
        /// </summary>
        /// <param name="spriteSkin">ref to SpriteSkin</param>
        /// <param name="mode">Mode of point generation. For Random specify 'count' argument, for Matrix specify 'matrixSize' argument. Matrix points is more accurate, while random gives more chaotic results</param>
        /// <param name="findingHullSteps">Increase steps to get more accurate hull (3-5 is optimal for most cases)</param>
        /// <param name="count">Count of points (only for Random mode)</param>
        /// <param name="matrixSize">Cell size of matrix (only for Matrix mode)</param>
        /// <returns></returns>
        public static ((Vector2, Color, Vector3Int, Vector3, Vector2)[], Vector2[]) GenerateColorSamplesWithinHull(
            this UnityEngine.U2D.Animation.SpriteSkin spriteSkin,
            SpriteSkinPointsGenerationMode mode = SpriteSkinPointsGenerationMode.Matrix,
            int findingHullSteps = 1,
            int count = 100,
            Vector2 matrixSize = default
        )
        {
            var spriteRenderer = spriteSkin.GetComponent<SpriteRenderer>();
            var spriteTexture = spriteRenderer.sprite.texture;
            var result = new List<(Vector2, Color, Vector3Int, Vector3, Vector2)>();

            Vector3[] deformedVertices = spriteSkin.GetDeformedVertexPositionData().ToArray();
            Vector2[] vertices2D = deformedVertices.Select(v => new Vector2(v.x, v.y)).ToArray();
            int[] hullIndices = Metamatic.Trigf.FindJarvisGreedyHull(vertices2D, findingHullSteps);

            Vector2[] hullVertices = hullIndices.Select(index => (Vector2)deformedVertices[index]).ToArray();
            List<(Vector2, Vector2, Vector3Int, Vector3)> randomPointsWithUVs = null;
            switch (mode)
            {
                case SpriteSkinPointsGenerationMode.Random:
                    randomPointsWithUVs = ConvexHullRandomPoints.GenerateRandomPointsInConvexHull(hullVertices, count, deformedVertices, spriteRenderer.sprite.uv);
                    break;
                case SpriteSkinPointsGenerationMode.Matrix:
                    randomPointsWithUVs = ConvexHullRandomPoints.GenerateMatrixPointsInConvexHull(hullVertices, matrixSize, deformedVertices, spriteRenderer.sprite.uv);
                    break;
            }
            
            for (int i = 0; i < randomPointsWithUVs.Count; i++)
            {
                Vector2 randomPoint2D = randomPointsWithUVs[i].Item1;
                Vector2 uv = randomPointsWithUVs[i].Item2;
                Vector3 randomPoint = new Vector3(randomPoint2D.x, randomPoint2D.y, 0);

                Color pixelColor = spriteTexture.GetPixelBilinear(uv.x, uv.y) * spriteRenderer.color;
                var triangle = randomPointsWithUVs[i].Item3;
                
                /* Map triangle back to original deform vertices indices */
                var mappedTriangle = new Vector3Int(
                    hullIndices[triangle.x], 
                    hullIndices[triangle.y], 
                    hullIndices[triangle.z]
                );
                
                result.Add((randomPoint, pixelColor, mappedTriangle, randomPointsWithUVs[i].Item4, uv));
            }

            return (result.ToArray(), hullVertices);
        }
    }
}