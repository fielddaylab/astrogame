using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Data;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;

namespace Astro {
    public sealed class PlayerProgressState : ISharedState {
        public int DayIndex = 0;

        public Dictionary<StringHash32, PlayerCelestialAssetKnowledge> Knowledge = MapUtils.Create<StringHash32, PlayerCelestialAssetKnowledge>(64);

        [NonSerialized] public List<ArchiveLayout> DayLayouts = new List<ArchiveLayout>();
        [NonSerialized] public List<StringHash32> UnlockedInstruments = new List<StringHash32>();
    }

    public struct PlayerCelestialAssetKnowledge : IByteSerializable {
        public BitSet32 Classifications;
        public PlayerCelestialAssetKnowledgeFlags Flags;

        public void ReadFrom(ref ByteReader reader) {
            reader.Read(ref Classifications);
            reader.Read(ref Flags);
        }

        public void WriteTo(ref ByteWriter writer) {
            writer.Write(Classifications);
            writer.Write(Flags);
        }
    }

    [Flags]
    public enum PlayerCelestialAssetKnowledgeFlags : ushort {
        IdentifiedResources = 0x01,
    }

    static public class PlayerKnowledgeUtility {
        static public PlayerKnowledgeQueryResult KnowsClassification(StringHash32 assetId, StringHash32 classificationId, out PlayerCelestialAssetKnowledge record) {
            CelestialAsset asset = Find.NamedAsset<CelestialAsset>(assetId);
            int idx = Array.IndexOf(asset.ClassIds, classificationId);
            if (idx < 0) {
                record = default;
                return PlayerKnowledgeQueryResult.InvalidData;
            }

            PlayerProgressState state = Find.State<PlayerProgressState>();
            if (!state.Knowledge.TryGetValue(assetId, out record)) {
                return PlayerKnowledgeQueryResult.NotKnown;
            }

            return record.Classifications[idx] ? PlayerKnowledgeQueryResult.Known : PlayerKnowledgeQueryResult.NotKnown;
        }

        static public PlayerKnowledgeQueryResult KnowsClassification(CelestialAsset asset, StringHash32 classificationId, out PlayerCelestialAssetKnowledge record) {
            Assert.NotNullOrDestroyed(asset);

            int idx = Array.IndexOf(asset.ClassIds, classificationId);
            if (idx < 0) {
                record = default;
                return PlayerKnowledgeQueryResult.InvalidData;
            }

            PlayerProgressState state = Find.State<PlayerProgressState>();
            if (!state.Knowledge.TryGetValue(asset.AssetId, out record)) {
                return PlayerKnowledgeQueryResult.NotKnown;
            }

            return record.Classifications[idx] ? PlayerKnowledgeQueryResult.Known : PlayerKnowledgeQueryResult.NotKnown;
        }

        static public PlayerKnowledgeQueryResult MarkNewClassification(StringHash32 assetId, StringHash32 classificationId, out PlayerCelestialAssetKnowledge record) {
            CelestialAsset asset = Find.NamedAsset<CelestialAsset>(assetId);
            int idx = Array.IndexOf(asset.ClassIds, classificationId);
            if (idx < 0) {
                record = default;
                return PlayerKnowledgeQueryResult.InvalidData;
            }

            PlayerProgressState state = Find.State<PlayerProgressState>();
            if (!state.Knowledge.TryGetValue(assetId, out record)) {
                return PlayerKnowledgeQueryResult.NotKnown;
            }

            if (record.Classifications[idx]) {
                return PlayerKnowledgeQueryResult.Known;
            } else {
                record.Classifications.Set(idx);
                state.Knowledge[assetId] = record;
                return PlayerKnowledgeQueryResult.NewKnowledge;
            }
        }

        static public PlayerKnowledgeQueryResult MarkNewClassification(CelestialAsset asset, StringHash32 classificationId, out PlayerCelestialAssetKnowledge record) {
            Assert.NotNullOrDestroyed(asset);
            int idx = Array.IndexOf(asset.ClassIds, classificationId);
            if (idx < 0) {
                record = default;
                return PlayerKnowledgeQueryResult.InvalidData;
            }

            PlayerProgressState state = Find.State<PlayerProgressState>();
            if (!state.Knowledge.TryGetValue(asset.AssetId, out record)) {
                return PlayerKnowledgeQueryResult.NotKnown;
            }

            if (record.Classifications[idx]) {
                return PlayerKnowledgeQueryResult.Known;
            } else {
                record.Classifications.Set(idx);
                state.Knowledge[asset.AssetId] = record;
                return PlayerKnowledgeQueryResult.NewKnowledge;
            }
        }

        static public PlayerKnowledgeQueryResult KnowsFlags(StringHash32 assetId, PlayerCelestialAssetKnowledgeFlags flags, out PlayerCelestialAssetKnowledge record) {
            PlayerProgressState state = Find.State<PlayerProgressState>();
            if (!state.Knowledge.TryGetValue(assetId, out record)) {
                return PlayerKnowledgeQueryResult.NotKnown;
            }

            return (record.Flags & flags) != 0 ? PlayerKnowledgeQueryResult.Known : PlayerKnowledgeQueryResult.NotKnown;
        }
    
        static public void UpdateKnowledge(StringHash32 assetId, PlayerCelestialAssetKnowledge record) {
            PlayerProgressState state = Find.State<PlayerProgressState>();
            state.Knowledge[assetId] = record;
        }
    }

    public enum PlayerKnowledgeQueryResult {
        NotKnown,
        Known,
        InvalidData,
        NewKnowledge,
    }
}