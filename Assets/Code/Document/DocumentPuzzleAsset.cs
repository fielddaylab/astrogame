using BeauUtil;
using FieldDay.Assets;
using UnityEngine;
using UnityEngine.Video;

namespace Astro
{
    [CreateAssetMenu(menuName = "AstroGame/Document Puzzle Asset")]
    public sealed class DocumentPuzzleAsset : NamedAsset
    {
        public DocumentAsset QuestionAsset;
        public DocumentAsset CorrectAnswer;
    }
}