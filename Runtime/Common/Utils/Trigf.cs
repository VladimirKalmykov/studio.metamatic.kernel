using System;
using System.Linq;
using UnityEngine;

namespace Metamatic
{
    public class Trigf
    {
        public const float RAD45 = 0.785398f;
        public const float PI = 3.141592f;
        public const float PI2 = 3.141592f * 2f;
        public const float HALFPI = 3.141592f / 2f;

        /// <summary>
        /// Find edge "c" by edge "a" and edge "c" and the angle between "a" and "b" (С in radians)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="C">Angle between a and b in radians</param>
        /// <returns></returns>
        public static double SolveTriangleEdgeByCosineTheorem(double a, double b, double C)
        {
            return Math.Sqrt(
                Math.Pow(a, 2) + Math.Pow(b, 2) - (2 * a * b * Math.Cos(C))
            );
        }

        /// <summary>
        /// Find edge "c" by edge "a" and edge "c" and the angle between "a" and "b" (С in radians)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        public static float SolveTriangleEdgeByCosineTheorem(float a, float b, float C)
        {
            return Mathf.Sqrt(
                Mathf.Pow(a, 2) + Mathf.Pow(b, 2) - (2 * a * b * Mathf.Cos(C))
            );
        }

        /// <summary>
        /// Finds triangle angles by two sides and angle between it (angles in radian)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="C">Angle between</param>
        /// <returns></returns>
        public static Triplet<double> SolveTriangleAnglesByCosineTheorem(double a, double b, double C)
        {
            var c = SolveTriangleEdgeByCosineTheorem(a, b, C);
            var A = Math.Acos((b * b + c * c - a * a) / (2 * b * c));
            var B = (180 * Mathf.Deg2Rad) - A - C;

            return new Triplet<double>(A, B, C);
        }

        /// <summary>
        /// Return angle A by edges a, b, c
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static double SolveTriangleAngleByEdges(double a, double b, double c)
        {
            return Math.Acos((b * b + c * c - a * a) / (2 * b * c));
        }

        /// <summary>
        /// Find triangle edge by its opposite angle and another edge and its opposite angle
        /// </summary>
        /// <param name="a">Searched edge</param>
        /// <param name="A">Edge A opposite angle</param>
        /// <param name="X">Searched edge opposite angle</param>
        /// <returns></returns>
        public static double SolveTriangleEdgeBySinusTheorem(double a, double A, double X)
        {
            return a * (Math.Sin(X) / Math.Sin(A));
        }

        /// <summary>
        /// Returns reciprocal ratios of values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static float[] Ratio(float[] values)
        {
            var sum = 0f;
            foreach (var value in values)
            {
                sum += value;
            }

            var result = new float[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i] / sum;
            }

            return result;
        }

        /// <summary>
        /// Returns reciprocal ratios of values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static float[] Ratio(int[] values)
        {
            var sum = values.Aggregate(0f, (current, value) => current + value);

            var result = new float[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = (int)values[i] / sum;
            }

            return result;
        }

        /// <summary>
        /// Returns reciprocal ratios of values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double[] Ratio(double[] values)
        {
            double sum = 0f;
            foreach (var value in values)
            {
                sum += value;
            }

            var result = new double[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i] / sum;
            }

            return result;
        }

        /** Get signed angle (degree -180...180) between two points */
        public static float GetAngle(Vector2 point1, Vector2 point2)
        {
            return (float)((Math.Atan2(point2.y - point1.y, point2.x - point1.x) * 180) / Math.PI);
        }

        /** Get signed angle (degree -180...180) between two points in an inverted axis */
        public static float GetAngleInverted(Vector2 point1, Vector2 point2)
        {
            return (float)((Math.Atan2(point2.x - point1.x, point2.y - point1.y) * 180) / Math.PI);
        }

        /** Get unsigned angle (degree 0...180) between two points */
        public static float GetAbsAngle(Vector2 point1, Vector2 point2)
        {
            return (float)Math.Abs((Math.Atan2(point2.y - point1.y, point2.x - point1.x) * 180) / Math.PI);
        }

        /** Get unsigned angle (degree 0...180) between two points */
        public static float GetAbsAngleInverted(Vector2 point1, Vector2 point2)
        {
            return (float)Math.Abs((Math.Atan2(point2.x - point1.x, point2.y - point1.y) * 180) / Math.PI);
        }

        /** Get unsigned angle (360 degrees) between two points */
        public static float GetAngle360(Vector2 point1, Vector2 point2)
        {
            var degrees = (float)((Math.Atan2(point2.y - point1.y, point2.x - point1.x) * 180) / Math.PI);
            return degrees = (degrees + 360) % 360;
        }

        /** Get unsigned angle (360 degrees) between two points */
        public static float GetAngle360Inverted(Vector2 point1, Vector2 point2)
        {
            var degrees = (float)((Math.Atan2(point2.x - point1.x, point2.y - point1.y) * 180) / Math.PI);
            return degrees = (degrees + 360) % 360;
        }

        /** Get the coordinates betwen two points with t factor */
        public static Vector3 InterpolatePoints(Vector3 point1, Vector3 point2, float t)
        {
            var nx = point1.x + (point2.x - point1.x) * t;
            var ny = point1.y + (point2.y - point1.y) * t;
            var nz = point1.z + (point2.z - point1.z) * t;
            return new Vector3(nx, ny, nz);
        }

        /** Get angle between two lines */
        public static float GetAngleBetweenLines(Vector2 point1, Vector2 point2, Vector2 point3, Vector2 point4)
        {
            var dAx = point2.x - point1.x;
            var dAy = point2.y - point1.y;
            var dBx = point4.x - point3.x;
            var dBy = point4.y - point3.y;
            var angle = Math.Atan2(dAx * dBy - dAy * dBx, dAx * dBx + dAy * dBy);
            // if(angle < 0) {angle = angle * -1;}
            return (float)(angle * (180 / Math.PI));
        }

        /// <summary>
        /// Point of intersection of two rays
        /// </summary>
        /// <param name="A1">Line 1 start point</param>
        /// <param name="A2">Line 1 end point</param>
        /// <param name="B1">Line 2 start point</param>
        /// <param name="B2">Line 2 start point</param>
        /// <param name="found">Will true if intersect</param>
        /// <returns></returns>
        public static Vector2? GetRaysIntersectionPoint(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2)
        {
            var tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);

            if (tmp == 0)
            {
                // No solution!
                return null;
            }

            var mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;

            return new Vector2(
                B1.x + (B2.x - B1.x) * mu,
                B1.y + (B2.y - B1.y) * mu
            );
        }

        /// <summary>
        /// Check is point on the line
        /// </summary>
        /// <param name="A1">Line start</param>
        /// <param name="A2">Line end</param>
        /// <param name="point">Target point</param>
        /// <param name="thickness">Thickness of line (when zero intersection point will be difficult to determine, better value is 0.1f)</param>
        /// <returns></returns>
        public static bool IsPointOnTheLine(Vector2 A1, Vector2 A2, Vector2 point, float thickness = 0.1f)
        {
            var sum = Vector2.Distance(A1, point) + Vector2.Distance(A2, point) - thickness;
            return sum <= Vector2.Distance(A1, A2);
        }

        /// <summary>
        /// Point of intersection of two lines
        /// </summary>
        /// <param name="A1">Line 1 start</param>
        /// <param name="A2">Line 1 end</param>
        /// <param name="B1">Line 2 start</param>
        /// <param name="B2">Line 2 end</param>
        /// <param name="found">Will true if intersect</param>
        /// <param name="thickness">Thickness of line (when zero intersection point will be difficult to determine)</param>
        /// <returns></returns>
        public static Vector2? GetLinesIntersectionPoint(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2,
            float thickness = 0.1f)
        {
            var point = GetRaysIntersectionPoint(A1, A2, B1, B2);

            if (point.HasValue)
            {
                if (IsPointOnTheLine(A1, A2, point.Value, thickness) &&
                    IsPointOnTheLine(B1, B2, point.Value, thickness))
                {
                    return point.Value;
                }
            }


            return null;
        }

        /// <summary>
        /// Find the line intersection with rectangle edges in a case when the line starts inedge the rectangle but ends outedge the rectangle.
        /// </summary>
        /// <param name="topLeft">Top left corner of rectangle</param>
        /// <param name="topRight">Top right corner of rectangle</param>
        /// <param name="bottomRight">Bottom right corner of rectangle</param>
        /// <param name="bottomLeft">Bottom left corner of rectangle</param>
        /// <param name="innerPoint">Inner point</param>
        /// <param name="outerPoint">Outer point</param>
        /// <returns></returns>
        public static Vector2? GetInnerRectRayIntersect(Vector2 topLeft, Vector2 topRight, Vector2 bottomRight,
            Vector2 bottomLeft, Vector2 innerPoint, Vector2 outerPoint)
        {
            /* Get four angles between point and corners */

            var angleTopRight = Trigf.GetAngleInverted(
                innerPoint,
                topRight
            );
            var angleBottomRight = Trigf.GetAngleInverted(
                innerPoint,
                bottomRight
            );
            var angleBottomLeft = Trigf.GetAngleInverted(
                innerPoint,
                bottomLeft
            );
            /* Get angle between point and direction */
            var pointAngle = Trigf.GetAngleInverted(
                innerPoint,
                outerPoint
            );

            Vector2? intersection;
            // Check with topLeft angle and topRight angle
            if (pointAngle > Trigf.GetAngleInverted(
                    innerPoint,
                    topLeft
                ) && pointAngle <= angleTopRight)
            {
                intersection = Trigf.GetLinesIntersectionPoint(innerPoint, outerPoint, topLeft, topRight);
            }
            else if (pointAngle > angleTopRight && pointAngle <= angleBottomRight)
            {
                intersection = Trigf.GetLinesIntersectionPoint(innerPoint, outerPoint, topRight, bottomRight);
                /* Check with double condition, because GetAngle return value from -180 to 180, so we must check both negative and positive values */
            }
            else if ((pointAngle > 0 && pointAngle > angleBottomRight) || (pointAngle < 0 && pointAngle <
                         Trigf.GetAngleInverted(
                             innerPoint,
                             bottomLeft
                         )))
            {
                intersection = Trigf.GetLinesIntersectionPoint(innerPoint, outerPoint, bottomRight, bottomLeft);
            }
            else
            {
                intersection = Trigf.GetLinesIntersectionPoint(innerPoint, outerPoint, bottomLeft, topLeft);
            }

            return intersection;
        }

        /// <summary>
        /// Normalize angle with transform can be inverted horizontally by scale
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="horizontalScaleSign"></param>
        /// <returns></returns>
        public static float NormalizeAngleRelativeScaleInvertion(float angle, int horizontalScaleSign)
        {
            if (horizontalScaleSign > 0)
            {
                return angle;
            }
            else if (horizontalScaleSign < 0)
            {
                return 180 - angle;
            }

            return 180 - angle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rect">Bounding rect</param>
        /// <param name="innerPoint">Point inside</param>
        /// <param name="outerPoint">Point outside</param>
        /// <returns></returns>
        public static Vector2? GetInnerRectRayIntersect(Rect rect, Vector2 innerPoint, Vector2 outerPoint)
        {
            /* Get four angles between point and corners */
            var topLeft = new Vector2(0f, rect.height);
            var bottomRight = new Vector2(rect.width, 0f);
            var bottomLeft = new Vector2(0f, 0f);
            var topRight = new Vector2(rect.width, rect.height);
            var angleTopRight = Trigf.GetAngleInverted(
                innerPoint,
                topRight
            );
            var angleBottomRight = Trigf.GetAngleInverted(
                innerPoint,
                bottomRight
            );
            var angleBottomLeft = Trigf.GetAngleInverted(
                innerPoint,
                bottomLeft
            );
            /* Get angle between point and direction */
            var pointAngle = Trigf.GetAngleInverted(
                innerPoint,
                outerPoint
            );

            Vector2? intersection;
            // Check with topLeft angle and topRight angle
            if (pointAngle > Trigf.GetAngleInverted(
                    innerPoint,
                    topLeft
                ) && pointAngle <= angleTopRight)
            {
                intersection = Trigf.GetLinesIntersectionPoint(innerPoint, outerPoint, topLeft, topRight);
            }
            else if (pointAngle > angleTopRight && pointAngle <= angleBottomRight)
            {
                intersection = Trigf.GetLinesIntersectionPoint(innerPoint, outerPoint, topRight, bottomRight);
                /* Check with double condition, because GetAngle return value from -180 to 180, so we must check both negative and positive values */
            }
            else if ((pointAngle > 0 && pointAngle > angleBottomRight) || (pointAngle < 0 && pointAngle <
                         Trigf.GetAngleInverted(
                             innerPoint,
                             bottomLeft
                         )))
            {
                intersection = Trigf.GetLinesIntersectionPoint(innerPoint, outerPoint, bottomRight, bottomLeft);
            }
            else
            {
                intersection = Trigf.GetLinesIntersectionPoint(innerPoint, outerPoint, bottomLeft, topLeft);
            }

            return intersection;
        }

        /// <summary>
        /// Get point at specified angle and distance
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Vector2 GetPointAtAngle2D(float angle, float distance = 1)
        {
            var angle2 = 180 - 90 - angle;
            var x =
                (distance * (float)Math.Sin((double)(angle * Math.PI) / 180)) /
                (float)Math.Sin((double)(90 * Math.PI) / 180);
            var y =
                (distance * (float)Math.Sin((double)(angle2 * Math.PI) / 180)) /
                (float)Math.Sin((double)(90 * Math.PI) / 180);

            return new Vector2(y, x);
        }

        /// <summary>
        /// Rotate `point` around `pivot`
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pivot"></param>
        /// <param name="angles"></param>
        /// <returns></returns>
        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            var dir = point - pivot;
            dir = Quaternion.Euler(angles) * dir;
            point = dir + pivot;
            return point;
        }

        /// <summary>
        /// Rotate `point` around `pivot`
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pivot"></param>
        /// <param name="angles"></param>
        /// <returns></returns>
        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion angles)
        {
            var dir = point - pivot;
            dir = angles * dir;
            return dir + pivot;
        }

        /// <summary>
        /// Rotate point around pivot by given axis and angle
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pivot"></param>
        /// <param name="axis"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 axis, float angle)
        {
            var dir = point - pivot;
            dir = Quaternion.AngleAxis(angle, axis) * dir;
            point = dir + pivot;
            return point;
        }

        /// <summary>
        /// Rotate `point` around `pivot`
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pivot"></param>
        /// <param name="angles"></param>
        /// <returns></returns>
        public static Vector3 RotatePointAroundPivot(Vector2 point, Vector2 pivot, float angle)
        {
            var dir = point - pivot;
            dir = Quaternion.Euler(new Vector3(0f, 0f, angle)) * dir;
            point = dir + pivot;
            return point;
        }


        /// <summary>
        /// Move point in direction
        /// </summary>
        /// <param name="start"></param>
        /// <param name="direction"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Vector3 MovePointInDirection(Vector3 start, Vector3 direction, float distance)
        {
            return start + (direction.normalized * distance);
        }

        /// <summary>
        /// Get Random point on the perimeter of a circle with given radius
        /// </summary>
        /// <param name="radius"></param>
        /// <returns>Vector2 point</returns>
        public static UnityEngine.Vector2 GetRandomPointOnRadius2D(float radius)
        {
            var angle = UnityEngine.Random.Range(0f, 1f) * Math.PI * 2;

            var x = (float)Math.Cos(angle) * radius;
            var y = (float)Math.Sin(angle) * radius;

            return new Vector2(x, y);
        }

        /// <summary>
        /// Get exponent that required to multiply base number with exponential factor to get specific result. For example, 10 * Math.pow(0.5, ?) = 1.25, where 10 - base, 1.25 - result, factor - 0.5, ? - exponent
        /// </summary>
        /// <returns></returns>
        public static double getExponentByFactor(float result, float baseValue, float factor)
        {
            return Math.Log(result / baseValue) / Math.Log(factor);
        }

        /// <summary>
        /// Get projected angle around specified axis.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="forward"></param>
        /// <param name="axis"></param>
        /// <param name="clockwise"></param>
        /// <returns></returns>
        public static float AngleAroundAxis(Vector3 v, Vector3 forward, Vector3 axis, bool clockwise = false)
        {
            Vector3 right;
            if (clockwise)
            {
                right = Vector3.Cross(forward, axis);
                forward = Vector3.Cross(axis, right);
            }
            else
            {
                right = Vector3.Cross(axis, forward);
                forward = Vector3.Cross(right, axis);
            }

            return Mathf.Atan2(Vector3.Dot(v, right), Vector3.Dot(v, forward)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Get a twirl angle around the axis formed by the direction to the target (for example it can be facing a rocket that strives to target and uses feint) */
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pivot"></param>
        /// <param name="angle"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static Quaternion TwirlRotationAroundDirection(Vector3 pivot, Vector3 target, float angle,
            float progress)
        {
            /* Get direction to target relative pivot */
            var relativePos = target - pivot;
            /* Get twirl angles for x, y axis */
            var x = EasingFunction.EaseInOutSine(-angle, angle, Mathf.PingPong(progress + 0.5f, 1));
            var y = EasingFunction.EaseInOutSine(-angle, angle, Mathf.PingPong(progress, 1));
            var feint = new Vector3(
                x,
                y,
                0f
            );

            return Quaternion.LookRotation(relativePos,
                Vector3.up) * Quaternion.Euler(feint);
        }

        /// <summary>
        /// Get sign of float ot zero if float equals zero
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int SignOrZero(float value)
        {
            return value == 0f ? 0 : (int)Mathf.Sign(value);
        }

        /// <summary>
        /// Get sign of float ot zero if float equals zero
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int SignOrZero(float value, float gap)
        {
            return Mathf.Abs(value) < gap ? 0 : (int)Mathf.Sign(value);
        }

        /// <summary>
        /// Get reflected point of the target projection through the pivot at given distantion
        /// </summary>
        /// <explanation>
        /// The most similar real life situation is how the light particle gets to the photo matrix through the pinhole camera
        /// =                 |
        ///     -             |
        ///         -         |
        ///             -     |       |
        ///                 -         | Pinhole
        ///                   | -     | matrix
        ///                   |     - x
        ///                   |         
        ///                   |
        /// </explanation>
        /// <param name="target"></param>
        /// <param name="pivot"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private static Vector3 GetReflectedProjection(Vector3 target, float distanceToTarget,
            float distanceToProjection)
        {
            return target * -1 * (distanceToProjection / distanceToTarget);
        }

        /// <summary>
        /// Flip rotation euler z angle horizontal
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Quaternion FlipRotationEulerZVertical(Quaternion q)
        {
            var euler = q.eulerAngles;
            euler.z = euler.z >= 0 ? 180 - euler.z : -180 - euler.z;
            return Quaternion.Euler(euler);
        }

        /// <summary>
        /// Flip rotation euler z angle vertical
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Quaternion FlipRotationEulerZHorizontal(Quaternion q)
        {
            var euler = q.eulerAngles;
            euler.z *= -1;
            return Quaternion.Euler(euler);
        }

        /// <summary>
        /// Cast angle with range 0 360
        /// </summary>
        /// <param name="a"></param>
        /// <returns>Angle with range 0 360</returns>
        public static float OptimizeAngle(float a)
        {
            var normalizeAngle = a % 360;
            return normalizeAngle < 0f ? 360f + normalizeAngle : normalizeAngle;
        }

        /// <summary>
        /// Clamp angle 
        /// </summary>
        /// <param name="current"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float ClampAngle(float current, float min, float max)
        {
            var dtAngle = Mathf.Abs(((min - max) + 180) % 360 - 180);
            var hdtAngle = dtAngle * 0.5f;
            var midAngle = min + hdtAngle;

            var offset = Mathf.Abs(Mathf.DeltaAngle(current, midAngle)) - hdtAngle;
            if (offset > 0)
                current = Mathf.MoveTowardsAngle(current, midAngle, offset);
            return current;
        }

        /// <summary>
        /// Get angle progress between min and max
        /// </summary>
        /// <todo>What if angle is out of min max range?</todo>
        /// <param name="current"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float UnlerpAngle(float current, float min, float max)
        {
            var distance = Mathf.DeltaAngle(min, max);
            var minDistance = Mathf.DeltaAngle(current, min);

            return Mathf.Abs(minDistance / distance);
        }

        public static Quaternion SumQuaternions(Quaternion a, Quaternion b)
        {
            Vector3 slotEuler = a.eulerAngles;
            Vector3 moduleEuler = b.eulerAngles;

            Vector3 combinedEuler = slotEuler + moduleEuler;

            combinedEuler = new Vector3(
                (combinedEuler.x + 360) % 360,
                (combinedEuler.y + 360) % 360,
                (combinedEuler.z + 360) % 360
            );
            return Quaternion.Euler(combinedEuler);
        }

        /// <summary>
        /// Find effector position by joints and its angles with forward kinematics
        /// </summary>
        /// <param name="joints"></param>
        /// <param name="angles"></param>
        /// <returns></returns>
        public static Vector3 GetForwardKinematics2dEffector(Transform[] joints, float[] angles)
        {
            var prevPoint = joints[0].position;
            var rotation = Quaternion.identity;
            for (var i = 1; i < joints.Length; i++)
            {
                // Выполняет поворот вокруг новой оси
                rotation *= Quaternion.AngleAxis(angles[i - 1], Vector3.forward);
                var nextPoint = prevPoint + rotation * joints[i].localPosition;

                prevPoint = nextPoint;
            }

            return prevPoint;
        }

        /// <summary>
        /// Find effector position by joints and its angles with forward kinematics
        /// </summary>
        /// <param name="joints"></param>
        /// <param name="angles"></param>
        /// <returns></returns>
        public static Vector2 GetForwardKinematics2dEffector(Vector3[] joints, float[] angles)
        {
            var prevPoint = joints[0];
            var rotation = Quaternion.identity;
            for (var i = 1; i < joints.Length; i++)
            {
                // Выполняет поворот вокруг новой оси
                rotation *= Quaternion.AngleAxis(angles[i - 1], Vector3.forward);
                var nextPoint = prevPoint + rotation * joints[i];

                prevPoint = nextPoint;
            }

            return prevPoint;
        }

        /// <summary>
        /// Get medium point between multiple points relative their weight
        /// </summary>
        /// <param name="points"></param>
        /// <param name="weights"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Vector3 LerpArray(Vector3[] points, float[] weights)
        {
            if (weights.Length != points.Length)
            {
                throw new Exception("Mismatch points and weights count");
            }

            var sum = weights.Sum();
            var result = Vector3.zero;

            for (var i = 0; i < points.Length; i++)
            {
                result += points[i] * (weights[i] / sum);
            }

            return result;
        }

        /// <summary>
        /// Get the value of b or c, which is farthest from a
        /// </summary>
        /// <param name="value"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float FarthestFrom(float a, float b, float c)
        {
            return Mathf.Abs(a - b) > Mathf.Abs(a - c) ? b : c;
        }

        /// <summary>
        /// Devide vector
        /// </summary>
        /// <param name="x"></param>
        /// <param name="divider"></param>
        /// <returns></returns>
        public static Vector3 Divide(Vector3 x, Vector3 divider)
        {
            return new Vector3(x.x / divider.x, x.y / divider.y, x.z / divider.z);
        }

        /// <summary>
        /// (Experimanetal) Get magnitude of the vector in the specified direction
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static float GetMagnitudeInTheDirection(Vector3 vector, Vector3 direction)
        {
            var magnitude = vector.magnitude;
            return Vector3.Dot(vector.normalized, direction.normalized) * vector.magnitude;
        }

        // /// <summary>
        // /// Find 
        // /// </summary>
        // /// <param name="points"></param>
        // /// <returns></returns>
        // public static int[] FindJarvisConvexHull(Vector3[] points)
        // {
        //     /* Form indexes */
        //     var indexes = points.Select((p, index) => index).ToArray();
        //     
        //     /* Find most left point  */
        //     for (var i = 1; i < indexes.Length; i++)
        //     {
        //         var cachedIndex = indexes[i];
        //         if (!(points[indexes[0]].x > points[cachedIndex].x)) continue;
        //         indexes[i] = indexes[0];
        //         indexes[0] = cachedIndex;
        //     }
        //    
        //    /* Find nearest up point */
        //    var upIndex = FindJarvisHullMaxAnglePointIndex(Vector3.down, 0, points, indexes);
        //    var upPointIndex = indexes[upIndex];
        //
        //    var hullLength = 0;
        //    var vector = Vector3.down;
        //    var index = 0;
        //    do
        //    {
        //        var minAngleIndex = FindJarvisHullAnglePointIndex(vector, index, index + 1, points, indexes);
        //
        //        var pointIndex = indexes[minAngleIndex];
        //        var tempIndex = indexes[index + 1];
        //        indexes[index + 1] = pointIndex;
        //        indexes[minAngleIndex] = tempIndex;
        //        
        //        index++;
        //        
        //        if (pointIndex == upPointIndex)
        //        {
        //            break;
        //        }
        //        vector = points[indexes[index]] - points[indexes[index - 1]];
        //        
        //    } while (index < indexes.Length - 1);
        //
        //    hullLength = index + 1;
        //
        //    Array.Resize(ref indexes, hullLength);
        //    return indexes;
        // }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="steps">Count of details (searching vertexes steps)</param>
        /// <param name="minAngle">Min angle when find next hull vertex relative previous edge</param>
        /// <param name="maxAngle">Max angle when find next hull vertex relative previous edge</param>
        /// <param name="minReflexiveAngle">Min reflexive angle (when we go inside hull and the returns back)</param>
        /// <returns></returns>
        public static int[] FindJarvisGreedyHull(Vector2[] points, int steps = 1, float minAngle = -123f,
            float maxAngle = 88f, float minReflexiveAngle = 57f)
        {
            /* Form indexes */
            var indexes = points.Select((p, index) => index).ToArray();
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            /* Find most left point  */
            for (var i = 1; i < indexes.Length; i++)
            {
                var cachedIndex = indexes[i];

                /* Most right point */
                var currentPoint = points[cachedIndex];
                if (maxX < currentPoint.x)
                {
                    maxX = currentPoint.x;
                }

                /* Most right point */
                if (minY > currentPoint.y)
                {
                    minY = currentPoint.y;
                }

                /* Most right point */
                if (maxY < currentPoint.y)
                {
                    maxY = currentPoint.y;
                }

                /* Most left point */
                if (minX > currentPoint.x)
                {
                    minX = currentPoint.x;
                    indexes[i] = indexes[0];
                    indexes[0] = cachedIndex;
                }
            }

            var diagonal = Vector2.Distance(new Vector2(minX, minY), new Vector2(maxX, maxY));

            /* Find nearest up point */
            // var upIndex = FindJarvisHullAnglePointIndex(true, Vector3.up, 0, 1, points, indexes, float.MaxValue, -180f, 180f, true);
            // var upPointIndex = indexes[upIndex];

            var hullLength = 0;
            var vector = Vector2.down;
            var index = 0;
            do
            {
                var minAngleIndex = FindJarvisHullAnglePointIndex(false, vector, index, index + 1, points, indexes,
                    float.MaxValue, minAngle, maxAngle, true);
                if (minAngleIndex <= 0) break;

                var pointIndex = indexes[minAngleIndex];

                var tempIndex = indexes[index + 1];
                indexes[index + 1] = pointIndex;
                indexes[minAngleIndex] = tempIndex;

                index++;

                // if (pointIndex == upPointIndex)
                // {
                //     break;
                // }
                vector = points[indexes[index]] - points[indexes[index - 1]];
            } while (index < indexes.Length - 1);

            hullLength = index + 1;

            var reverseVector = Vector2.up;
            var currentEdgeStartIndex = 0;
            var currentEdgeEndIndex = 0;
            var pointA = Vector2.zero;
            var pointB = Vector2.zero;
            var distance = 0f;
            /* Handle each long section */
            for (var step = 0; step < steps; step++)
            {
                index = 0;
                do
                {
                    currentEdgeStartIndex = index;
                    currentEdgeEndIndex = index == hullLength - 1 ? 0 : index + 1;
                    // var previousEdgeStartIndex = index - 1;
                    // var previousEdgeEndIndex = index;
                    pointA = points[indexes[currentEdgeStartIndex]];
                    pointB = points[indexes[currentEdgeEndIndex]];
                    distance = Vector2.Distance(pointA, pointB);
                    vector = points[indexes[currentEdgeEndIndex]] - points[indexes[currentEdgeStartIndex]];

                    // var prevVector = points[indexes[previousEdgeEndIndex]] - points[indexes[previousEdgeStartIndex]];

                    /* Find new vertex between current edge for forth direction */
                    var minAngleIndex = FindJarvisHullAnglePointIndex(false, vector, currentEdgeStartIndex, hullLength,
                        points, indexes, distance, minAngle, maxAngle);

                    /* Find new vertex between current edge for back direction */
                    // 
                    var firstFoundedVertexWasInserted = false;
                    if (minAngleIndex >= 0)
                    {
                        var bestIndex = indexes[minAngleIndex];

                        /* Compare new vector between new point and next point to current vector */
                        var newInVector = points[indexes[currentEdgeStartIndex]] - points[bestIndex];
                        var newOutVector = points[indexes[currentEdgeEndIndex]] - points[bestIndex];

                        var reflexiveAngle = Vector2.SignedAngle(newInVector, newOutVector);
                        if (Mathf.Abs(reflexiveAngle) < minReflexiveAngle)
                        {
                            // Debug.Log($"InOut Angle of {indexes[minAngleIndex]} is out of limit: {reflexiveAngle}");
                            index++;
                            continue;
                        }

                        if (minAngleIndex > index + 1)
                        {
                            /* Shift array */
                            for (var s = minAngleIndex; s > index + 1; s--)
                            {
                                indexes[s] = indexes[s - 1];
                            }

                            indexes[index + 1] = bestIndex;
                            index++;
                            firstFoundedVertexWasInserted = true;
                        }

                        hullLength++;
                    }

                    if (hullLength >= indexes.Length)
                    {
                        break;
                    }

                    /* Find vertex from right to left */
                    var secondStepIndex = firstFoundedVertexWasInserted ? index - 1 : index;
                    currentEdgeStartIndex = secondStepIndex;
                    currentEdgeEndIndex = secondStepIndex == hullLength - 1 ? 0 : secondStepIndex + 1;
                    pointA = points[indexes[currentEdgeStartIndex]];
                    pointB = points[indexes[currentEdgeEndIndex]];
                    distance = Vector2.Distance(pointA, pointB);
                    reverseVector = points[indexes[currentEdgeStartIndex]] - points[indexes[currentEdgeEndIndex]];
                    var minBackAndleIndex = FindJarvisHullAnglePointIndex(true, reverseVector, currentEdgeEndIndex,
                        hullLength, points, indexes, distance, minAngle, maxAngle, false);

                    if (minBackAndleIndex >= 0)
                    {
                        var bestIndex = indexes[minBackAndleIndex];

                        /* Compare new vector between new point and next point to current vector */
                        var newInVector = points[indexes[currentEdgeEndIndex]] - points[bestIndex];
                        var newOutVector = points[indexes[currentEdgeStartIndex]] - points[bestIndex];

                        var reflexiveAngle = Vector2.SignedAngle(newInVector, newOutVector);
                        if (Mathf.Abs(reflexiveAngle) < minReflexiveAngle)
                        {
                            index++;
                            continue;
                        }

                        if (minBackAndleIndex > index)
                        {
                            /* Shift array */
                            for (var s = minBackAndleIndex; s > currentEdgeStartIndex; s--)
                            {
                                indexes[s] = indexes[s - 1];
                            }

                            indexes[currentEdgeStartIndex + 1] = bestIndex;
                            index++;
                        }

                        hullLength++;
                    }


                    index++;
                } while (index < hullLength);
            }

            Array.Resize(ref indexes, hullLength);
            return indexes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector">Vector of previously edge</param>
        /// <param name="edgeStartIndex">Last index of edge</param>
        /// <param name="startIndex">Index we can start iterate</param>
        /// <param name="points">Array of points</param>
        /// <param name="indexes">Array of points indexes sorted as consistent vertexes</param>
        /// <param name="maxMagnitude">Max magnitude of test vector</param>
        /// <returns></returns>
        private static int FindJarvisHullAnglePointIndex(bool findMaxAngle, Vector2 vector, int edgeStartIndex,
            int startIndex, Vector2[] points, int[] indexes, float maxMagnitude = float.MaxValue,
            float minAngle = -180f, float maxAngle = 180f, bool closing = false, bool debug = false)
        {
            var bestAngle = findMaxAngle ? -360f : 360f;
            var bestIndex = -1;

            /* Compare with zero point first, then others aster startIndex */
            var y = !closing || edgeStartIndex == 0 ? startIndex : 0;
            if (y >= indexes.Length)
            {
                return -1;
            }

            do
            {
                var testVector = points[indexes[y]] - points[indexes[edgeStartIndex]];
                if (testVector.magnitude > maxMagnitude)
                {
                    goto Next;
                }

                var angle = Vector2.SignedAngle(vector, testVector);

                if (angle > minAngle && angle < maxAngle && (findMaxAngle ? angle > bestAngle : angle < bestAngle))
                {
                    bestAngle = angle;
                    bestIndex = y;
                }

                Next:
                y = (y == 0) ? startIndex : y + 1;
            } while (y < indexes.Length);

            if (debug && bestIndex >= 0)
            {
                Debug.Log($"bestAngle: {bestAngle} for point #{indexes[edgeStartIndex]} (index: {indexes[bestIndex]})");
            }

            return bestIndex;
        }

        public static float PingPong(float t, float x = 0.5f)
        {
#if DEBUG
            if (t < 0f || t > 1f)
            {
                throw new System.ArgumentOutOfRangeException("t", "Value must be between 0 and 1.");
            }

            if (x <= 0f || x >= 1f)
            {
                throw new System.ArgumentOutOfRangeException("x", "Value must be between 0 and 1, exclusive.");
            }
#endif

            // Приведение t к диапазону от 0 до 1 (не обязательно, если t всегда в диапазоне [0, 1])
            t = Mathf.Clamp01(t);

            if (t < x)
            {
                // Линейная интерполяция от 0 до 1
                return t / x;
            }
            else
            {
                // Линейная интерполяция от 1 до 0
                return (1f - (t - x) / (1f - x));
            }
        }
        
        /// <summary>
        /// Calculate force needed to perform a jump in 2D space to reach a specific point.
        /// </summary>
        /// <param name="gravity"></param>
        /// <param name="mass"></param>
        /// <param name="linearDrag"></param>
        /// <param name="currentVelocity"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="additionalHeight">Add additional height to get higher then target actually is</param>
        /// <param name="force">Current force</param>
        /// <returns></returns>
        public static bool CalculateArcJumpForce2D(
            float gravity,
            float mass,
            float linearDrag,
            Vector2 currentVelocity,
            Vector2 point1,
            Vector2 point2,
            float additionalHeight,
            out Vector2 force)
        {
            force = Vector2.zero;

            // Разница позиций
            float deltaX = point2.x - point1.x;
            float deltaY = point2.y - point1.y;

            // Проверка на возможность прыжка
            if (deltaX == 0 && deltaY == 0)
            {
                return false; // Нет необходимости прыгать
            }

            // Желаемая максимальная высота
            float H_max = point2.y + additionalHeight;

            // Ускорение свободного падения должно быть положительным
            gravity = Mathf.Abs(gravity);

            // Вычисляем начальную вертикальную скорость без учёта сопротивления
            float v0y;
            try
            {
                v0y = Mathf.Sqrt(2 * gravity * Mathf.Max(0, (H_max - point1.y)));
            }
            catch
            {
                return false; // Невозможно достичь желаемой высоты
            }

            // Время подъёма до максимальной высоты
            float tUp = v0y / gravity;

            // Время спуска от максимальной высоты до цели
            float tDown;
            try
            {
                tDown = Mathf.Sqrt(2 * Mathf.Max(0, (H_max - point2.y)) / gravity);
            }
            catch
            {
                return false; // Невозможно вычислить время спуска
            }

            // Общее время полёта
            float tTotal = tUp + tDown;

            // Учитываем линейное сопротивление по оси X
            float k = linearDrag / mass;
            if (k == 0) k = 0.0001f; // Предотвращение деления на ноль

            float denom = 1 - Mathf.Exp(-k * tTotal);

            if (Mathf.Approximately(denom, 0))
            {
                return false; // Невозможно вычислить начальную горизонтальную скорость
            }

            float v0x = (deltaX * k) / denom;

            // Вычисляем требуемые силы с учётом текущей скорости
            float Fx = mass * (v0x - currentVelocity.x);
            float Fy = mass * (v0y - currentVelocity.y);

            force = new Vector2(Fx, Fy);

            return true;
        }
        
        public enum AngleReference
        {
            Right,   // Справа (по умолчанию 0 градусов)
            Top,     // Сверху (по умолчанию 90 градусов)
            Left,    // Слева (по умолчанию 180 градусов)
            Bottom   // Снизу (по умолчанию 270 градусов)
        }
        
        /// <summary>
        /// Get ellipse radius at specified angle
        /// </summary>
        /// <param name="ellipseDiameter">Diameter of ellipse (be attention to specify double values if you hove radius)</param>
        /// <param name="angleDegrees"></param>
        /// <param name="referencePoint"></param>
        /// <returns></returns>
        public static float GetEllipseRadius(Vector2 ellipseDiameter, float angleDegrees, AngleReference referencePoint = AngleReference.Right)
        {
            // Преобразуем угол в радианы
            float angleRadians = Mathf.Deg2Rad * angleDegrees;

            // Сместим угол в зависимости от точки отсчета
            switch (referencePoint)
            {
                case AngleReference.Top:
                    angleRadians -= Mathf.PI / 2; // Сдвиг на 90 градусов (сверху)
                    break;
                case AngleReference.Left:
                    angleRadians -= Mathf.PI; // Сдвиг на 180 градусов (слева)
                    break;
                case AngleReference.Bottom:
                    angleRadians -= 3 * Mathf.PI / 2; // Сдвиг на 270 градусов (снизу)
                    break;
            }

            // Радиусы эллипса
            float a = ellipseDiameter.x / 2f; // Радиус по ширине
            float b = ellipseDiameter.y / 2f; // Радиус по высоте

            // Вычисляем радиус эллипса под углом angleRadians
            float cosTheta = Mathf.Cos(angleRadians);
            float sinTheta = Mathf.Sin(angleRadians);

            float radius = (a * b) / Mathf.Sqrt((b * cosTheta) * (b * cosTheta) + (a * sinTheta) * (a * sinTheta));

            return radius;
        }
    }
}