namespace Metamatic.UI {
    using UnityEngine;

    [ExecuteAlways]
    public class FlexBox : MonoBehaviour, ISerializationCallbackReceiver
    {
        public enum DIRECTION {
            ROW,
            COLUMN
        }
        public DIRECTION direction = DIRECTION.ROW;
        [Tooltip("Update position of the elements even if they're not visible (influences performance expenses)")]
        public bool alwaysUpdate;
        public bool debug;
        RectTransform rectTransform;
        RectTransform[] cached_Children;
        Box[] cached_Boxes;
        private event System.Action OnBeforeDisableOrChangeDirection;
        private DIRECTION cached_Direction;
        void Awake() {
            rectTransform = (RectTransform)transform;
        }

        void OnEnable() {
            UpdateDirection();
            UpdateCache();
            UpdateChildren();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {

        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            cached_Direction = direction;
        }

        void UpdateDirection() {
            OnBeforeDisableOrChangeDirection?.Invoke();
            foreach(RectTransform child in transform) {
                // var dt = new DrivenRectTransformTracker();
                // dt.Clear();
                // dt.Add(this, child, direction == DIRECTION.ROW ? DrivenTransformProperties.AnchoredPositionX : DrivenTransformProperties.AnchoredPositionY);

                // ClearTrackerOnDestroyOrChangeDirection(dt);
            }
            UpdateCache();
        }

        void OnDisable() {
            OnBeforeDisableOrChangeDirection?.Invoke();
        }

        void ClearTrackerOnDestroyOrChangeDirection(DrivenRectTransformTracker dt) {
            OnBeforeDisableOrChangeDirection += () => {
                dt.Clear();
            };
        }

        void UpdateCache() {
            cached_Children = new RectTransform[transform.childCount];
            cached_Boxes = new Box[transform.childCount];
            int i = 0;
            foreach(RectTransform child in transform) {
                cached_Children[i] = child;
                cached_Boxes[i] = child.GetComponent<Box>();
                i++;

                child.anchorMax = new Vector2(
                    direction == DIRECTION.ROW ? 0 : 1,
                    1
                );
                child.anchorMin = new Vector2(
                    0,
                    direction == DIRECTION.ROW ? 0 : 1
                );
                child.pivot = new Vector2(
                    0,
                    1
                );
            }
        }

        public static Rect RectTransformToScreenSpace(RectTransform transform)
        {
            Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
            return new Rect((Vector2)transform.position - (size * 0.5f), size);
        }

        // Update is called once per frame
        void Update()
        {
            bool shouldUpdate = true;
            if (!alwaysUpdate) {
                Rect inScreenRect = RectTransformToScreenSpace(rectTransform);
                shouldUpdate = (
                    inScreenRect.xMin > 0f && inScreenRect.xMin < Screen.width
                    || inScreenRect.xMax > 0f && inScreenRect.xMax < Screen.width
                ) &&
                (
                    inScreenRect.yMin > 0f && inScreenRect.yMin < Screen.height
                    || inScreenRect.yMax > 0f && inScreenRect.yMax < Screen.height
                );
            }

            if (shouldUpdate) {
                UpdateChildren();
            }
        }

        void UpdateChildren() {
            if (transform.childCount != cached_Children.Length) {
                UpdateCache();
            }
            var totalSpace = direction == DIRECTION.ROW ? rectTransform.rect.width : rectTransform.rect.height;
            int i = 0;
            float totalGrow = 0f;
            float usedSpace = 0f;
            int growCount = 0;
            
            foreach(RectTransform child in transform) {
                if (cached_Children[i] != child) {
                    UpdateCache();
                    UpdateChildren();
                    return;
                }
                if (child.transform.gameObject.activeInHierarchy && child.transform.gameObject.activeSelf) {
                    if (cached_Boxes[i] != null) {
                        totalGrow += cached_Boxes[i].grow;
                        growCount++;
                    } else {
                        usedSpace += direction == DIRECTION.ROW ? child.rect.width : child.rect.height;
                    }
                }
                i++;
            }
            
            var restSpace = totalSpace - usedSpace;
            var growUnitWeight = growCount > 0 ? restSpace / totalGrow : 0;

            var margin = 0f;
            i = 0;
            foreach(RectTransform child in transform) {
                if (child.transform.gameObject.activeInHierarchy) {
                    if (cached_Boxes[i] != null) {
                        margin += direction == DIRECTION.ROW ? cached_Boxes[i].marginLeft : -cached_Boxes[i].marginTop;
                    }
                    
                    child.anchoredPosition = new Vector3(
                        direction == DIRECTION.ROW ? margin : 0,
                        direction == DIRECTION.ROW ? 0 : margin
                    );

                    if (cached_Boxes[i] != null) {
                        child.sizeDelta = direction == DIRECTION.COLUMN
                            ? new Vector2(0, (growUnitWeight * cached_Boxes[i].grow) - cached_Boxes[i].marginTop - cached_Boxes[i].marginBottom)
                            : new Vector2((growUnitWeight * cached_Boxes[i].grow) - cached_Boxes[i].marginLeft - cached_Boxes[i].marginRight, 0);

                        margin += direction == DIRECTION.ROW ? cached_Boxes[i].marginRight 
                                                             : -cached_Boxes[i].marginBottom;
                    }
                    
                    margin += direction == DIRECTION.ROW ? child.sizeDelta.x 
                        : -child.sizeDelta.y;
                }
                i++;
            }
        }


        void OnValidate() {
            UpdateCache();
            if (cached_Direction != direction) {
                UpdateDirection();
            }
        }
    }
}
