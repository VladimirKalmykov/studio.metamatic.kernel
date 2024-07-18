using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.MicroOperations
{
    public static class AnimationOperations
    {
        public static async UniTask ShakeTransform(RectTransform transform, float duration, AnimationCurve strength, AnimationCurve frequency, float positionRange, float scaleRange, CancellationToken cancellationToken)
        {
            await MicroPromise.Run(async (resolve, reject, token) =>
            {
                Vector2 defaultPosition = transform.anchoredPosition;
                Vector2 defaultScale = transform.localScale;
                try
                {
                    var nextChangePosition = Time.time;
                    Vector2 nextPosition = Vector2.zero;
                    Vector2 nextScale = Vector2.zero;
                    await AsyncUtils.PlayerLoopAction(ratio =>
                    {
                        if (Time.time > nextChangePosition)
                        {
                            nextChangePosition = Time.time + frequency.Evaluate(ratio);
                            var randomVector2 = Random.insideUnitCircle;
                            var randomValue = (Random.value * 2f) - 1f;
                            var strengthValue = strength.Evaluate(ratio);
                            nextPosition = new Vector2(randomVector2.x * strengthValue * positionRange,
                                randomVector2.y * strengthValue * positionRange);
                            nextScale = Vector2.Lerp(Vector2.zero, Vector2.one * scaleRange, randomValue);
                        }

                        transform.anchoredPosition = Vector2.Lerp(transform.anchoredPosition, defaultPosition + nextPosition, Time.deltaTime * 50f);
                        transform.localScale = Vector2.Lerp(transform.localScale, defaultScale + nextScale, Time.deltaTime * 50f);
                    }, duration: duration, cancellationToken: token);
                }
                catch (System.Exception e)
                {
                    reject(e);
                }
                finally
                {
                    transform.localScale = defaultScale;
                    transform.anchoredPosition = defaultPosition;
                }
                resolve();
            }, cancellationToken).result;
        }
    }
}