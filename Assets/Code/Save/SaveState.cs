using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Data;

namespace Astro.Save {
    public struct SaveStateHeader {
        public string PlayerCode;
        public long LastSaveTS;
        public double Playtime;
        public int Version;
    }

    public struct SaveStateManifest {
        public uint Length;
        public uint ChunkCount;
        public ulong Checksum;
        public byte Padding;
    }

    public struct SaveStateChunkHeader {
        public StringHash32 Id;
        public uint ChunkLength;
        public uint ChunkLengthUncompressed;
    }

    public struct SaveStateChunkConsts {
        public int Version;
    }

    public struct SaveScratchpad {
        public Unsafe.ArenaHandle Allocator;
        public UnsafeSpan<SaveScratchBlock> Blocks;
        public int BlockCount;

        public UnsafeSpan<T> Alloc<T>(int length) where T : unmanaged {
            return Allocator.AllocSpan<T>(length);
        }

        public UnsafeSpan<byte> GetBlock(StringHash32 blockId) {
            for(int i = 0; i < BlockCount; i++) {
                if (Blocks[i].Id == blockId) {
                    return Blocks[i].Data;
                }
            }

            Assert.Fail("No block with id '{0}'", blockId);
            return default;
        }

        public UnsafeSpan<T> GetBlock<T>(StringHash32 blockId) where T : unmanaged {
            for (int i = 0; i < BlockCount; i++) {
                if (Blocks[i].Id == blockId) {
                    var span = Blocks[i].Data;
                    unsafe {
                        return new UnsafeSpan<T>((T*) span.Ptr, (uint) (span.Length / sizeof(T)));
                    }
                }
            }

            Assert.Fail("No block with id '{0}'", blockId);
            return default;
        }

        public UnsafeSpan<byte> CreateBlock(StringHash32 blockId, int length) {
            Assert.True(BlockCount < Blocks.Length, "Cannot create more than {0} blocks", Blocks.Length);
            UnsafeSpan<byte> span = Allocator.AllocSpan<byte>(length);
            Blocks[BlockCount++] = new SaveScratchBlock() {
                Id = blockId,
                Data = span
            };
            return span;
        }

        public UnsafeSpan<T> CreateBlock<T>(StringHash32 blockId, int length) where T : unmanaged {
            Assert.True(BlockCount < Blocks.Length, "Cannot create more than {0} blocks", Blocks.Length);
            UnsafeSpan<T> span = Allocator.AllocSpan<T>(length);
            unsafe {
                Blocks[BlockCount++] = new SaveScratchBlock() {
                    Id = blockId,
                    Data = new UnsafeSpan<byte>((byte*) span.Ptr, (uint) (length * sizeof(T)))
                };
            }
            return span;
        }
    }

    public struct SaveScratchBlock {
        public StringHash32 Id;
        public UnsafeSpan<byte> Data;
    }

    public delegate void SaveStateChunkWriter(object context, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch);
    public delegate void SaveStateChunkReader(object context, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch);

    public interface ISaveStateChunkObject {
        void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch);
        void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch);
    }

    public interface ISaveStatePostLoad {
        void PostLoad(SaveStateChunkConsts consts, ref SaveScratchpad scratch);
    }
}