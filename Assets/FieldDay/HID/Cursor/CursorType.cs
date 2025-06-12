using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.HID {
    [CreateAssetMenu(menuName = "Field Day/Cursor Type", order = -220)]
    public class CursorType : NamedAsset {
        [Required] public Sprite DefaultImage;
        public Sprite HeldImage;
    }
}