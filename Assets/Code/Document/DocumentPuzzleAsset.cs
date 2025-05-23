using BeauUtil;
using FieldDay.Assets;
using System;
using UnityEngine;
using UnityEngine.Video;

namespace Astro
{
    [CreateAssetMenu(menuName = "AstroGame/Document Puzzle Asset")]
    public sealed class DocumentPuzzleAsset : NamedAsset
    {
        [Serializable]
        public struct QuestionAnswerPair
        {
            public DocumentAsset Question;
            public DocumentAsset Answer;
        }

        [SerializeField] public QuestionAnswerPair[] SolutionPairs;
    }
}