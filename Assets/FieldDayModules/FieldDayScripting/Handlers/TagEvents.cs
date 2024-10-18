using BeauUtil;
using BeauUtil.Tags;
using Leaf;

namespace FieldDay.Scripting {
    static public class TagEvents {
        static public readonly StringHash32 ConfigureVoxOverlap = "vox-overlap";
        static public readonly StringHash32 DispatchEvent = "dispatch-event";
        static public readonly StringHash32 SubtitleTimecodes = "subtitle-timecodes";

        static internal void ConfigureParsers(CustomTagParserConfig parser, ILeafPlugin plugin) {
            LeafUtils.ConfigureDefaultParsers(parser, plugin, null);

            parser.AddEvent("vox-overlap", ConfigureVoxOverlap).WithFloatData(-0.2f);
            parser.AddEvent("srt", SubtitleTimecodes).WithFloatData();
            parser.AddEvent("dispatch-event", DispatchEvent).WithStringHashData();

            parser.AddReplace("icon", ReplaceIcon);
        }

        static internal void ConfigureHandlers(TagStringEventHandler handler, ILeafPlugin plugin) {
            LeafUtils.ConfigureDefaultHandlers(handler, plugin);

            handler.Register(ConfigureVoxOverlap, Event_VoxOverlap);
            handler.Register(DispatchEvent, Event_DispatchEvent);
            handler.Register(LeafUtils.Events.Character, Event_NoOp);
            handler.Register(LeafUtils.Events.Pose, Event_NoOp);
            handler.Register(SubtitleTimecodes, Event_NoOp);
        }

        /// <summary>
        /// No-op event.
        /// </summary>
        static public readonly TagStringEventHandler.InstantEventWithContextDelegate Event_NoOp = (e, o) => { };

        static private string ReplaceIcon(TagData tag, object context) {
            return string.Format("<sprite name=\"{0}\">", tag.Data.ToString());
        }

        static private void Event_VoxOverlap(TagEventData evt, object context) {
            var thread = (ScriptThread) context;
            thread.SetVoxReleaseTime(evt.GetFloat());
        }

        static private void Event_DispatchEvent(TagEventData evt, object context) {
            Game.Events.Dispatch(evt.GetStringHash());
        }
    }
}