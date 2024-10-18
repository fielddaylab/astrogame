using BeauUtil;
using FieldDay.Assets;
using FieldDay.Components;
using FieldDay.SharedState;

namespace Astro {
    public sealed class DocumentMailboxState : SharedStateComponent {
        /// <summary>
        /// Ids of incoming documents.
        /// </summary>
        public RingBuffer<StringHash32> Incoming = new RingBuffer<StringHash32>(4, RingBufferMode.Expand);
    }
}