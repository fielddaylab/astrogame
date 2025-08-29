using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.HID {
    [CreateAssetMenu(menuName = "Field Day/Cursor Type", order = -220)]
    public class CursorType : NamedAsset {
        [Required] public Sprite DefaultImage;
        public Sprite HeldImage;

        [Space]
        public float DefaultScale = 1;
        public float HeldScaleOverride = 0;
    }
}