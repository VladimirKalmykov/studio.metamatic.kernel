using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine;
using System.Text.RegularExpressions;
using System.IO;

namespace Metamatic {
    class DefaultValues {
        /// <summary>
        /// Default plane at zero axis
        /// </summary>
        /// <param name="Vector3(0"></param>
        /// <returns></returns>
        public readonly static Plane DEFAULT_PLANE = new Plane(Vector3.forward, new Vector3(0, 0, 0));
    }
    public static class MiscUtils {
        /// <summary>
        /// Get world distance from screen distabce 
        /// </summary>
        /// <param name="screenPos"></param>
        /// <param name="camera"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static Vector3 ScreenToPerspectivePoint(Vector2 screenPos, Camera camera = null, Plane? plane = null) {
            plane = plane.HasValue ? plane.Value : DefaultValues.DEFAULT_PLANE;
            if (camera == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException("You must pass cached camera to improove performance");
#else
                camera = Camera.main;
#endif
            }
            Ray ray = camera.ScreenPointToRay(screenPos);
            float distance;
            
            plane.Value.Raycast(ray, out distance);
            return ray.GetPoint(distance);
        }

        /// <summary>
        /// Create Plane by mesh (You should use it with Plane primitive)
        /// </summary>
        /// <param name="gameObject">The object must contains MeshFilter</param>
        /// <returns></returns>
        public static Plane MeshToPlane(GameObject gameObject) {
            var filter = gameObject.GetComponent<MeshFilter>();
            Vector3 normal = Vector3.zero;

            if(filter && filter.mesh.normals.Length > 0) {
                normal = filter.transform.TransformDirection(filter.mesh.normals[0]);

                return new Plane(normal, gameObject.transform.position);
            }

            return default;
        }

        /// <summary>
        /// Get world distance from viewport distabce 
        /// </summary>
        /// <param name="screenPos"></param>
        /// <param name="camera"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static Vector3 ViewportToPerspectivePoint(Vector2 viewportPos, Camera camera = null, Plane? plane = null) {
            plane = plane.HasValue ? plane.Value : DefaultValues.DEFAULT_PLANE;
            if (camera == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException("You must pass cached camera to improove performance");
#else
                camera = Camera.main;
#endif
            }
            Ray ray = camera.ViewportPointToRay(viewportPos);
            float distance;
            
            plane.Value.Raycast(ray, out distance);
            return ray.GetPoint(distance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="camera"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static float ScreenDistanceToPerspectivePoint(float distance, Camera camera = null, Plane? plane = null) {
            Debug.LogWarning("ScreenDistanceToPerspectivePoint is deprecated, use ScreenToWorldDistance");
            plane = plane ?? DefaultValues.DEFAULT_PLANE;
            if (camera == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException("You must pass cached camera to improove performance");
#else 
                camera = Camera.main;
#endif
            }
            Vector3 worldPoint1 = ScreenToPerspectivePoint(Vector2.zero, camera, plane.Value);
            Vector3 worldPoint2 = ScreenToPerspectivePoint(Vector2.zero + Vector2.right * distance, camera, plane.Value);
            return Vector3.Distance(worldPoint1, worldPoint2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="camera"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static float ScreenToWorldDistance(float distance, Camera camera = null, Plane? plane = null) {
            plane = plane ?? DefaultValues.DEFAULT_PLANE;
            if (camera == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException("You must pass cached camera to improove performance");
#else 
                camera = Camera.main;
#endif
            }
            Vector3 worldPoint1 = ScreenToPerspectivePoint(Vector2.zero, camera, plane.Value);
            Vector3 worldPoint2 = ScreenToPerspectivePoint(Vector2.zero + Vector2.right * distance, camera, plane.Value);
            return Vector3.Distance(worldPoint1, worldPoint2);
        }

        public static float ViewportToWorldDistance(float distance, Camera camera = null, Plane? plane = null) {
            plane = plane ?? DefaultValues.DEFAULT_PLANE;
            if (camera == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException("You must pass cached camera to improove performance");
#else 
                camera = Camera.main;
#endif
            }
            Vector3 worldPoint1 = ViewportToPerspectivePoint(Vector2.zero, camera, plane.Value);
            Vector3 worldPoint2 = ViewportToPerspectivePoint(Vector2.zero + Vector2.right * distance, camera, plane.Value);
            return Vector3.Distance(worldPoint1, worldPoint2);
        }

        public static Rect ScreenRectToWorldRect(Rect rect, Camera camera = null) {
            if (camera == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException("You must pass cached camera to improove performance");
#else 
                camera = Camera.main;
#endif
            }
            Vector3 min = camera.ScreenToWorldPoint(new Vector3(rect.xMin, rect.yMin, camera.nearClipPlane));
            Vector3 max = camera.ScreenToWorldPoint(new Vector3(rect.xMax, rect.yMax, camera.nearClipPlane));

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        /// <summary>
        /// Get result of lerp and indemnify it of stuck
        /// </summary>
        /// <param name="a">From</param>
        /// <param name="b">To</param>
        /// <param name="result">Result</param>
        /// <param name="gap">Gap</param>
        /// <returns></returns>    
        public static float Indemnify(float a, float b, float result, float gap = 0.01f) {
            if ((b > a && result + gap >= b) || (b < a && result - gap <= b)) {
                return b;
            }
            return result;
        }

        /// <summary>
        /// Get result of lerp and indemnify it of stuck
        /// </summary>
        /// <param name="a">From</param>
        /// <param name="b">To</param>
        /// <param name="result">Result</param>
        /// <param name="gap">Gap</param>
        /// <returns></returns>    
        public static float Indemnify(float value, float result, float gap = 0.01f) {
            if ((result + gap >= value) && (result - gap <= value)) {
                return result;
            }
            return value;
        }

        /// <summary>
        /// Hex color string to Color
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color HexCodeToColor(string color)
        {
            if (color.StartsWith("#", StringComparison.InvariantCulture))
            {
                color = color.Substring(1); // strip #
            }

            if (color.Length == 6)
            {
                color += "FF"; // add alpha if missing
            }

            var hex = Convert.ToUInt32(color, 16);
            var r = ((hex & 0xff000000) >> 0x18) / 255f;
            var g = ((hex & 0xff0000) >> 0x10) / 255f;
            var b = ((hex & 0xff00) >> 8) / 255f;
            var a = ((hex & 0xff)) / 255f;

            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Set color brightness
        /// </summary>
        /// <param name="color"></param>
        /// <param name="brightness">The brightness value</param>
        /// <param name="multiply">If true new brightness multiplied to current brightness</param>
        /// <returns></returns>
        public static Color SetColorBrightness(Color color, float brightness, bool multiply = false) {
            /* Create special color for additional smoke (it should have main color but with resetted saturation and value) */
            float H = 0f;
            float S = 0f;
            float V = 0f;
            
            Color.RGBToHSV(color, out H, out S, out V);

            V = multiply ? brightness * V : brightness;

            return  Color.HSVToRGB(H, S, V);
        }

        /// <summary>
        /// Inscribe rect with gived aspect ratio range to the another rect
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="minRatio"></param>
        /// <param name="maxRatio"></param>
        /// <returns></returns>
        public static Rect InscribeAspectRatioToRect(Rect rect, float minRatio, float maxRatio) {
            float rectRatio = rect.width / rect.height;
            float width = rect.width;
            float height = rect.height;

            // 1.7777
            // 0.5625

            if (rectRatio > maxRatio) {
                if (width > height) {
                    width = height * maxRatio;
                } else {
                    height = width / maxRatio;
                }
            } else if (rectRatio < minRatio) {
                if (width > height) {
                    width = height * minRatio;
                } else {
                    height = width / minRatio;
                }
            }

            float left = (rect.width - width) / 2;
            float top = (rect.height - height) / 2;

            return new Rect(left, top, width, height);
        }

        /// <summary>
        /// Fit rectangle with given aspect ratio range into the current screen and return world rect
        /// </summary>
        /// <param name="minRatio"></param>
        /// <param name="maxRatio"></param>
        /// <returns></returns>
        public static Rect fitScreenAspectRatioRangeToTheWorld(float minRatio, float maxRatio, Camera camera = null) {
            
            if (camera == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException("You must pass cached camera to improove performance");
#else
                camera = Camera.main;
#endif
            }

            float screenRatio = Screen.height / Screen.width;
            float areaScreenWidth = Screen.width;
            float areaScreenHeight = Screen.height;

            if (screenRatio < maxRatio) {
                areaScreenWidth = areaScreenHeight / maxRatio;
            } else if (screenRatio > minRatio) {
                areaScreenHeight = areaScreenWidth * minRatio;
            }

            float left = (Screen.width - areaScreenWidth) / 2;

            Vector2 bottomLeft = camera.ScreenToWorldPoint(new Vector3(left, 0, camera.nearClipPlane));
            Vector2 topLeft = camera.ScreenToWorldPoint(new Vector3(left + 0, areaScreenHeight, camera.nearClipPlane));
            Vector2 bottomRight = camera.ScreenToWorldPoint(new Vector3(left + areaScreenWidth, 0f, camera.nearClipPlane));

            float worldWidth = bottomRight.x - bottomLeft.x;
            float worldHeight = bottomLeft.y - topLeft.y;

            return new Rect(topLeft.x, topLeft.y, worldWidth, worldHeight);
        }

        /// <summary>
        /// Fit GameObject sprite to specified rect using transform scale
        /// </summary>
        /// <param name="spriteRenderer"></param>
        /// <param name="size"></param>
        public static void fitSprite2DScaleToRect(SpriteRenderer spriteRenderer, Rect rect) {
            float defaultBoundX = spriteRenderer.bounds.size.x / spriteRenderer.transform.localScale.x;
            float defaultBoundY = spriteRenderer.bounds.size.y / spriteRenderer.transform.localScale.y;
            float width = rect.width == 0 ? defaultBoundX : rect.width;
            float height = rect.height == 0 ? defaultBoundY : rect.height;
            float scaleX = width / defaultBoundX;
            float scaleY = height / defaultBoundY;
            spriteRenderer.transform.localScale = new Vector3(
                scaleX,
                scaleY,
                1
            );
            
            spriteRenderer.transform.localPosition = new Vector3(
                rect.x + (width / 2),
                rect.y + (height / 2),
                spriteRenderer.transform.localPosition.z
            );
        }

         /// <summary>
        /// Fit GameObject sprite to specified rect using transform scale
        /// </summary>
        /// <param name="spriteRenderer"></param>
        /// <param name="size"></param>
        public static void fitSprite2DScaleToSize(SpriteRenderer spriteRenderer, Vector2 size) {
            float defaultBoundX = spriteRenderer.bounds.size.x / spriteRenderer.transform.localScale.x;
            float defaultBoundY = spriteRenderer.bounds.size.y / spriteRenderer.transform.localScale.y;
            float width;
            float height;

            if (spriteRenderer.drawMode == SpriteDrawMode.Sliced) {
                width = size.x == 0 ? spriteRenderer.size.x : size.x;
                height = size.y == 0 ? spriteRenderer.size.y : size.y;
                spriteRenderer.size = new Vector2(
                    width,
                    height
                );
            } else {
                width = size.x == 0 ? defaultBoundX : size.x;
                height = size.y == 0 ? defaultBoundY : size.y;
                float scaleX = width / defaultBoundX;
                float scaleY = height / defaultBoundY;

                spriteRenderer.transform.localScale = new Vector3(
                    scaleX,
                    scaleY,
                    1
                );
            }
        }

        /// <summary>
        /// If twp numbers have the same sign, then finds max abs value, if the sign is different then sum it 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static float Superimpose(float x, float y) {
            float result = (x * y > 0.0f)
                    ? (x >= 0 ? Mathf.Max(x, y) : Mathf.Min(x, y))
                    : x + y;
            return result;
        }

        /// <summary>
        /// Get min of absolutized values (return min value with sign of first value)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <returns></returns>
        public static float MinAbs(float value, float min) {
            return Mathf.Min(Mathf.Abs(value), min) * Mathf.Sign(value);
        }

        /// <summary>
        /// Get max of absolutized values (return max value with sign of first value)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float MaxAbs(float value, float max) {
            return Mathf.Max(Mathf.Abs(value), max) * Mathf.Sign(value);
        }

        /// <summary>
        /// Get member field or property type
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static Type GetMemberUnderlyingType(MemberInfo member, bool throwException = true)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    if (throwException) {
                        throw new ArgumentException
                        (
                        "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
                        );
                    } else {
                        return null;
                    }
            }
        }

        /// <summary>
        /// Anyway can member be written
        /// </summary>
        /// <param name="member"></param>
        /// <param name="throwException"></param>
        /// <returns></returns>
        public static bool CanMemberWrite(MemberInfo member, bool throwException = true) {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return true;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).CanWrite;
                default:
                    if (throwException) {
                        throw new ArgumentException
                        (
                        "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
                        );
                    } else {
                        return false;
                    }
            }
        }

        /// <summary>
        /// Get combinig renderers bounding rect (renderers can be collected by method GetComponontsInChildren)
        /// </summary>
        /// <param name="renderers"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Rect GetRenderersBoundingRect(Renderer[] renderers) {
            Bounds bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            var ext = bounds.extents;

            float maxY = -999999, maxX = -999999, minY = 999999, minX = 999999;

            for (var x = -1; x < 2; x+=2)
            {
                for (var y = -1; y < 2; y+=2)
                {
                    for (var z = -1; z < 2; z+=2)
                    {
                        var worldPoint = bounds.center+new Vector3(ext.x*x, ext.y*y, ext.z*z);

                        maxY = Mathf.Max(maxY, worldPoint.y);
                        maxX = Mathf.Max(maxX, worldPoint.x);
                        minY = Mathf.Min(minY, worldPoint.y);
                        minX = Mathf.Min(minX, worldPoint.x);
                    }
                }
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Get combinig renderers bounding rect (renderers can be collected by method GetComponontsInChildren)
        /// </summary>
        /// <param name="renderers"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Bounds GetRenderersBounds(Renderer[] renderers) {
            Bounds bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            
            return bounds;
        }

        /// <summary>
        /// Get combinig renderers bounding rect (renderers can be collected by method GetComponontsInChildren)
        /// </summary>
        /// <param name="renderers"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Bounds GetColiidersBounds(Collider[] colliders) {
            Bounds bounds = colliders[0].bounds;
            for (var i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
            
            return bounds;
        }

        /// <summary>
        /// Get combinig renderers bounding rect (renderers can be collected by method GetComponontsInChildren)
        /// </summary>
        /// <param name="renderers"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Rect GetRenderersScreenBoundingRect(Renderer[] renderers, Camera camera = null) {
            if (camera == null) {
                camera = Camera.main;
            }
            Bounds bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            var ext = bounds.extents;

            float maxY = -999999, maxX = -999999, minY = 999999, minX = 999999;

            for (var x = -1; x < 2; x+=2)
            {
                for (var y = -1; y < 2; y+=2)
                {
                    for (var z = -1; z < 2; z+=2)
                    {
                        var vect = bounds.center+new Vector3(ext.x*x, ext.y*y, ext.z*z);
                        var worldPoint = camera.WorldToScreenPoint(vect);

                        maxY = Mathf.Max(maxY, worldPoint.y);
                        maxX = Mathf.Max(maxX, worldPoint.x);
                        minY = Mathf.Min(minY, worldPoint.y);
                        minX = Mathf.Min(minX, worldPoint.x);
                    }
                }
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Get combinig colliders bounding rect (renderers can be collected by method GetComponontsInChildren)
        /// </summary>
        /// <param name="renderers"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Rect GetCollidersScreenBoundingRect(Collider[] colliders, Camera camera = null) {
            if (camera == null) {
                camera = Camera.main;
            }
            Bounds bounds = colliders[0].bounds;
            for (var i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
            var ext = bounds.extents;

            float maxY = -999999, maxX = -999999, minY = 999999, minX = 999999;

            for (var x = -1; x < 2; x+=2)
            {
                for (var y = -1; y < 2; y+=2)
                {
                    for (var z = -1; z < 2; z+=2)
                    {
                        var vect = bounds.center+new Vector3(ext.x*x, ext.y*y, ext.z*z);
                        var worldPoint = camera.WorldToScreenPoint(vect);

                        maxY = Mathf.Max(maxY, worldPoint.y);
                        maxX = Mathf.Max(maxX, worldPoint.x);
                        minY = Mathf.Min(minY, worldPoint.y);
                        minX = Mathf.Min(minX, worldPoint.x);
                    }
                }
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
        
        /// <summary>
        /// Change 3d object volume in a case to get required mass
        /// </summary>
        /// <param name="mass"></param>
        /// <param name="rb"></param>
        public static void Set3DMass(Rigidbody rb, float mass) {
            rb.transform.localScale = rb.transform.localScale * (Mathf.Sqrt(mass) / Mathf.Sqrt(rb.mass));
        }

        public static Task AsyncOperationToTask(AsyncOperation asyncOperation, bool allowSceneActivation = true) {
            asyncOperation.allowSceneActivation = allowSceneActivation;
            var taskCompletionSource = new TaskCompletionSource<bool>();
            asyncOperation.completed += (AsyncOperation ao) => {
                taskCompletionSource.SetResult(true);
            };

            return taskCompletionSource.Task;
        }

        // <summary>
        /// Simplifies path, by removed upper folder references "folder\.." will be converted
        /// </summary>
        /// <param name="path">Path to simplify</param>
        /// <returns>Related path</returns>
        public static String PathSimplify(String path)
        {
            path = path.Replace($"{Path.DirectorySeparatorChar}.{Path.DirectorySeparatorChar}", $"{Path.DirectorySeparatorChar}");
            while (true)
            {
                String newPath = new Regex(@"[^\\/]+(?<!\.\.)[\\/]\.\.[\\/]").Replace(path, "" );
                if (newPath == path) break;
                path = newPath;
            }
            return path;
        }

        public static string NormalizePath(string path) {
            char replaceSymbol = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
            return MiscUtils.PathSimplify(path.Replace(replaceSymbol, Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Get transform's path (the names of all of its parents separated by delimiter)
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string GetGameObjectPath(Transform transform, string delimiter = ".") {
            var path = new List<string>();
            var currentTransform = transform;
            while(currentTransform != null && currentTransform.parent != null) {
                path.Add(currentTransform.name);
                currentTransform = currentTransform.parent;   
            }
            return String.Join(delimiter, path.ToArray());
        }

        /// <summary>
        /// Get game object's path (the names of all of its parents separated by delimiter)
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string GetGameObjectPath(GameObject gameObject, string delimiter = ".") {
            return GetGameObjectPath(gameObject.transform, delimiter);
        }
        
        /// <summary>
        /// Get two integer divisors of an integer which are maximum close to root of this number
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static Vector2Int GetOptimalDivisors(int n)
        {
            if (n <= 0)
            {
                return Vector2Int.zero;
            }
            var root = Mathf.Sqrt(n);
            var divisors = new List<int>();
            for (var i = 1; i <= root; i++)
            {
                if (n % i != 0) continue;
                divisors.Add(i);
                if (i != n / i)
                {
                    divisors.Add(n / i);
                }
            }

            divisors
                .Sort((d1, d2) =>
                {
                    if (d1 == d2)
                    {
                        return 0;
                    }

                    return Mathf.Abs(root - d1) > Mathf.Abs(root - d2) ? 1 : -1;
                });
            return new Vector2Int(divisors[0], n / divisors[0]);
        }
    }
}
