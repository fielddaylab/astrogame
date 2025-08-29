

using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scripting;
using FieldDay.Vox;
using OGD;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {


    public class Logging : MonoBehaviour {
        private const ushort CLIENT_LOG_VERSION = 1;
        private readonly JsonBuilder m_JsonBuilder = new JsonBuilder(Unsafe.KiB * 64); // json allocation capacity
        private OGDLog m_Log;
        [NonSerialized] private bool m_Debug;

        #region Inspector
        [SerializeField, Required] private string m_AppId;
        [SerializeField, Required] private string m_AppVersion;
        [SerializeField] private FirebaseConsts m_Firebase;
        [SerializeField] private bool m_Testing;

        #endregion //Inspector

        private void Start() {
            RegisterEvents();
            PrepareLogging();
        }

        #region Game State
        private int m_CurrentLevel;
        private List<string> m_UnlockedTools = new List<string>(); // list of tool_id: name for each instrument 
        private List<string> m_UnlockedFilters = new List<string>(); // list of filter_type: name for each filter
        private TelescopeOrientationData m_TelescopeOrientation; // quaternion? TODO: check in with Luke about expected format
        private int m_LocatorProximity;
        private WavelengthTypeMask m_WavelengthFilter;
        private bool m_MagnitudeModeIsApparent;
        private int m_PointsNeeded;
        private int m_PointsEarned;
        private StarLogData m_SelectedStar;
        private PuzzleData m_LogicPuzzle;


        private void SubmitGameState() {
            m_JsonBuilder.Clear();
            m_JsonBuilder.Begin()
                .Field("current_level", m_CurrentLevel)
                .BeginArray("unlocked_tools");
            foreach (var instrument in m_UnlockedTools) { 
                m_JsonBuilder.Item(instrument);
            }
            m_JsonBuilder.EndArray();
            m_JsonBuilder.BeginArray("unlocked_filters");
            foreach (var filter in m_UnlockedFilters) { 
                m_JsonBuilder.Item(filter);
            }
            m_JsonBuilder.EndArray();
            m_JsonBuilder.BeginArray("telescope_orientation")
                .Item(m_TelescopeOrientation.W)
                .Item(m_TelescopeOrientation.X)
                .Item(m_TelescopeOrientation.Y)
                .Item(m_TelescopeOrientation.Z);
            m_JsonBuilder.Field("locator_proximity", m_LocatorProximity);
            m_JsonBuilder.Field("wavelength_filter", EnumLookup.WavelengthType[(int)m_WavelengthFilter]);
            m_JsonBuilder.Field("magnitude_mode", m_MagnitudeModeIsApparent ? "APPARENT" : "ABSOLUTE");
            m_JsonBuilder.Field("points_needed", m_PointsNeeded);
            m_JsonBuilder.Field("points_earned", m_PointsEarned);
            m_JsonBuilder.BeginObject("selected_star");
            // TODO: star object
            m_JsonBuilder.EndObject();
            m_JsonBuilder.BeginArray("logic_puzzle");
            // TODO: all logic puzzle rows
            m_JsonBuilder.EndArray();

            m_Log.GameState(m_JsonBuilder.End());
        }

        private void UpdateCurrentLevel(int newLevel) {
            m_CurrentLevel = newLevel;
            SubmitGameState();
        }

        private void UpdateUnlockedTools(string unlockedTool) { 
            if (!m_UnlockedTools.Contains(unlockedTool)){
                m_UnlockedTools.Add(unlockedTool);
            }
            SubmitGameState();
        }

        private void UpdateUnlockedFilters(string unlockedFilter) {
            if (!m_UnlockedFilters.Contains(unlockedFilter)) {
                m_UnlockedFilters.Add(unlockedFilter);
            }
            SubmitGameState();
        }

        private void UpdateTelescopeOrientation(TelescopeOrientationData data) {
            m_TelescopeOrientation = data;
            SubmitGameState();
        }

        private void UpdateLocatorProximity(int prox) {
            m_LocatorProximity = prox;
            SubmitGameState();
        }

        private void UpdateCurrentFilter(WavelengthTypeMask currentFilter) {
            m_WavelengthFilter = currentFilter;
            SubmitGameState();
        }

        private void UpdateMagnitudeMode(bool magIsApparent) {
            m_MagnitudeModeIsApparent = magIsApparent;
            SubmitGameState();
        }

        private void UpdatePointsEarned(int ptsEarned) {
            m_PointsEarned = ptsEarned;
            SubmitGameState();
        }

        private void UpdatePointsNeeded(int ptsNeeded) {
            m_PointsNeeded = ptsNeeded;
            SubmitGameState();
        }

        private void UpdateSelectedStar(StarLogData star) {
            m_SelectedStar = star;
            SubmitGameState();
        }

        private void UpdateCurrentPuzzle(PuzzleData puzzle) {
            m_LogicPuzzle = puzzle;
            SubmitGameState();
        }

        #endregion // Game State

        #region Initialization

        private void PrepareLogging() {
#if DEVELOPMENT
            m_Debug = true;
#endif // DEVELOPMENT

            m_Log = new OGDLog(CreateOGDConsts(), new OGDLog.MemoryConfig(2048, Unsafe.KiB * 64, 256));

            if (!string.IsNullOrEmpty(m_Firebase.ApiKey)) {
                m_Log.UseFirebase(m_Firebase);
            }
            m_Log.SetDebug(m_Debug);

            // "testing mode" in editor: skip OGD upload but disable debug
#if UNITY_EDITOR
            if (!m_Testing) {
                m_Log.AddSettings(OGDLog.SettingsFlags.SkipOGDUpload);
                m_Log.SetDebug(false);
            }
#endif //UNITY_EDITOR

            OGDLog.SchedulingConfig sched = OGDLog.SchedulingConfig.Default;
            sched.FlushDelay = 2;
            m_Log.ConfigureScheduling(sched);
        }

        private void SetAnalyticsUserCode(string userCode) {
            Log.Msg("[OGDLog] Assigning user code {0}", userCode);
            m_Log.SetUserId(userCode);
            m_Log.Initialize(CreateOGDConsts());
            m_Log.NewEvent("session_start");
        }

        private OGDLogConsts CreateOGDConsts() {
            return new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                AppBranch = BuildInfo.Branch(),
                ClientLogVersion = CLIENT_LOG_VERSION,
            };
        }

        #endregion //Initialization

        #region Event Registration
        private void RegisterEvents() {
            // logging events
            AstroGame.Events
                .Register<string>(GameEvents.TitleGameStarting, SetAnalyticsUserCode)
                .Register(GameEvents.TitleNewGameClicked, LogClickNewGame)
                .Register(GameEvents.TitleContinueGameClicked, LogClickContinueGame)
                .Register(GameEvents.TitleOptionsClicked, LogClickOptionsMenu)
                .Register<bool>(GameEvents.GameStart, LogGameStart)
                .Register<int>(GameEvents.LevelStart, LogLevelStart)
                .Register<int>(GameEvents.LevelEnd, LogLevelEnd)
                .Register(GameEvents.ClickPauseGame, LogClickPauseGame)
                .Register(GameEvents.ClickResumeGame, LogClickResumeGame)
                .Register<string>(GameEvents.CutsceneStart, LogCutsceneStart)
                .Register<string>(GameEvents.CutsceneEnd, LogCutsceneEnd)
                .Register(GameEvents.DialogueAudioStart, LogDialogueAudioStart)
                .Register(GameEvents.DialogueAudioEnd, LogDialogueAudioEnd)
                .Register(GameEvents.DialogueTextDisplayed, LogDialogueTextDisplayed)
                .Register(GameEvents.ClickSkipDialogueLine, LogClickSkipDialogueLine)
                .Register(GameEvents.HintDisplayed, LogHintDisplayed)
                .Register(GameEvents.HintHidden, LogHintHidden)
                .Register(GameEvents.MeteorAreaAssigned, LogMeteorAreaAssigned)
                .Register(GameEvents.MeteorAreaHighlighted, LogMeteorAreaHighlighted)
                .Register(GameEvents.MeteorAreaUnhighlighted, LogMeteorAreaUnhighlighted)
                //.Register<string>(GameEvents.StarHighlighted, LogStarHighlighted)
                //.Register<string>(GameEvents.StarUnhighlighted, LogStarUnhighlighted)
                .Register<string>(GameEvents.HoverStar, LogHoverStar)
                .Register<StarLogData>(GameEvents.StarClicked, LogClickSelectStar)
                .Register<StringHash32>(GameEvents.InstrumentUnlocked, LogToolUnlocked)
                .Register<string>(GameEvents.TelescopeTurned, LogTurnTelescope)
                .Register<TelescopeViewLogData>(GameEvents.TelescopeViewAssigned, LogTelescopeViewAssigned)
                .Register<int>(GameEvents.LocatorCloser, LogLocatorCloser)
                .Register<int>(GameEvents.LocatorFurther, LogLocatorFurther)
                ;

            // state update events
            AstroGame.Events
                .Register<SubtitleLogData>(GameEvents.SubtitleDataChanged, HandleSubtitleDataChanged)
                .Register<HintLogData>(GameEvents.HintChanged, HandleHintChanged)
                ;

        }
        #endregion

        #region Logging Variables

        [NonSerialized] private SubtitleLogData m_LastKnownSubtitleData = default;
        [NonSerialized] private HintLogData m_LastKnownHint = default;

        private string m_TempStr;

        #endregion // Logging Variables

        #region State Handlers

        private void HandleSubtitleDataChanged(SubtitleLogData newData)
        {
            m_LastKnownSubtitleData = newData;
        }

        private void HandleHintChanged(HintLogData data)
        {
            m_LastKnownHint = data;
        }

        #endregion // State Handlers

        #region Logging



        //click_new_game/
        private void LogClickNewGame() {
            m_Log.NewEvent("click_new_game");
        }
        //click_resume_game/
        private void LogClickContinueGame() {
            m_Log.NewEvent("click_resume_game");
        }


        //click_options_menu (further settings events are TODO, pending implementation)/
        private void LogClickOptionsMenu() {
            m_Log.NewEvent("click_options_menu");
        }

        /*
        //click_free_play_menu (pending implementation)/
        private void LogClickFreePlayMenu() {
            // m_Log.NewEvent("click_options_menu");
        }
        */
        
        //game_start/
        //* from_resume
        private void LogGameStart(bool fromResume) {
            m_Log.BeginEvent("game_start");
            m_Log.EventParam("from_resume", fromResume);
            m_Log.SubmitEvent();
        }
       
        //level_start/
        //* level_number
        private void LogLevelStart(int levelNum) {
            m_Log.BeginEvent("level_start");
            m_Log.EventParam("level_number", levelNum);
            m_Log.SubmitEvent();

            UpdateCurrentLevel(levelNum);
        }

        //level_end/
        //* level_number
        private void LogLevelEnd(int levelNum)
        {
            m_Log.BeginEvent("level_end");
            m_Log.EventParam("level_number", levelNum);
            m_Log.SubmitEvent();
        }

        //click_pause_game/
        private void LogClickPauseGame() {
            m_Log.NewEvent("click_pause_game");
        }
        
        //click_resume_game/
        private void LogClickResumeGame() {
            m_Log.NewEvent("click_resume_game");
        }
        
        //cutscene_start/
        //* cutscene_id
        private void LogCutsceneStart(string cutsceneId) {
            m_Log.BeginEvent("cutscene_start");
            m_Log.EventParam("cutscene_id", cutsceneId);
            m_Log.SubmitEvent();
        }

        //cutscene_end/
        //* cutscene_id
        private void LogCutsceneEnd(string cutsceneId) {
            m_Log.BeginEvent("cutscene_end");
            m_Log.EventParam("cutscene_id", cutsceneId);
            m_Log.SubmitEvent();
        }

        //dialog_audio_start/
        //* line_id
        //* script_content
        //* speaker_id
        private void LogDialogueAudioStart() {
            m_Log.BeginEvent("dialog_audio_start");
            m_Log.EventParam("line_id", m_LastKnownSubtitleData.LineId.ToString());
            m_Log.EventParam("script_content", m_LastKnownSubtitleData.ScriptContent);
            m_Log.EventParam("speaker_id", VoxUtility.FindEmitter(m_LastKnownSubtitleData.CharacterId).CharacterId.Source());
            m_Log.SubmitEvent();
        } 

        //dialog_audio_end/
        //* line_id
        //* speaker_id
        private void LogDialogueAudioEnd() {
            m_Log.BeginEvent("dialog_audio_end");
            m_Log.EventParam("line_id", m_LastKnownSubtitleData.LineId.ToString());
            m_Log.EventParam("speaker_id", VoxUtility.FindEmitter(m_LastKnownSubtitleData.CharacterId).CharacterId.Source());
            m_Log.SubmitEvent();
        }

        //dialog_text_displayed/
        //* line_id
        //* script_content
        //* speaker_id
        private void LogDialogueTextDisplayed() {
            m_Log.BeginEvent("dialog_text_displayed");
            m_Log.EventParam("line_id", m_LastKnownSubtitleData.LineId.ToString());
            m_Log.EventParam("script_content", m_LastKnownSubtitleData.ScriptContent);
            m_Log.EventParam("speaker_id", VoxUtility.FindEmitter(m_LastKnownSubtitleData.CharacterId).CharacterId.Source());
            m_Log.SubmitEvent();
        }

        //click_skip_dialog_line/
        //* line_id
        //* speaker_id
        private void LogClickSkipDialogueLine() { // TODO: wait for non-debug implementation
            m_Log.BeginEvent("click_skip_dialog_line");
            m_Log.EventParam("line_id", m_LastKnownSubtitleData.LineId.ToString());
            m_Log.EventParam("speaker_id", VoxUtility.FindEmitter(m_LastKnownSubtitleData.CharacterId).CharacterId.Source());
            m_Log.SubmitEvent();
        }

        //hint_displayed/
        //* hint_id
        //* text_content
        private void LogHintDisplayed() {
            m_Log.BeginEvent("hint_displayed");
            m_Log.EventParam("hint_id", m_LastKnownHint.Id);
            m_Log.EventParam("text_content", m_LastKnownHint.Content);
            m_Log.SubmitEvent();
        }

        //hint_hidden/
        //* hint_id
        private void LogHintHidden() {
            m_Log.BeginEvent("hint_hidden");
            m_Log.EventParam("hint_id", m_LastKnownHint.Id);
            m_Log.SubmitEvent();
        }

        /* 
        //star_assigned/
        //* star_id
        private void LogStarAssigned(string starId) {
            m_Log.BeginEvent("star_assigned");
            m_Log.EventParam("star_id", starId);
            m_Log.SubmitEvent();
        }
        */

        /*
        //star_highlighted/
        //* star_id
        private void LogStarHighlighted(string starId) {
            m_Log.BeginEvent("star_highlighted");
            m_Log.EventParam("star_id", starId);
            m_Log.SubmitEvent();
        }

        //star_unhighlighted/
        //* star_id
        private void LogStarUnhighlighted(string starId) {
            m_Log.BeginEvent("star_unhighlighted");
            m_Log.EventParam("star_id", starId);
            m_Log.SubmitEvent();
        }
        */

        //meteor_area_assigned/
        private void LogMeteorAreaAssigned()
        {
            m_Log.NewEvent("meteor_area_assigned");
        }

        //meteor_area_highlighted/
        private void LogMeteorAreaHighlighted()
        {
            m_Log.NewEvent("meteor_area_highlighted");
        }

        //meteor_area_unhighlighted/
        private void LogMeteorAreaUnhighlighted()
        {
            m_Log.NewEvent("meteor_area_unhighlighted");
        }


        //hover_star/
        //* star_id
        private void LogHoverStar(string starId) {
            m_Log.BeginEvent("hover_star");
            m_Log.EventParam("star_id", starId);
            m_Log.SubmitEvent();
        }

        //click_select_star
        //* star_id
        //* constellation
        //* coordinates
        //* distance
        //* color
        //* temperature
        //* visible_magnitude
        //* blue_magnitude
        //* infrared_magnitude
        //* absolute_magnitude
        //* spectral_elements : List[Element]
        //* is_highlighted
        //* known_data: List[StarKnownData]
        //    * identification_type
        //    * category
        private void LogClickSelectStar(StarLogData star)
        {
            StarKnownData knownData = new StarKnownData();
            knownData.Data = new List<StarKnownDataItem>();
            var asset = Find.NamedAsset<CelestialAsset>(star.AssetID);
            var playerProgressState = Find.State<PlayerProgressState>();
            if (playerProgressState.Knowledge.ContainsKey(star.AssetID)) {
                var knowledge = playerProgressState.Knowledge[star.AssetID];
                for (int i = 0; i < asset.ClassIds.Length; i++)
                {
                    if (knowledge.Classifications[i])
                    {
                        StarKnownDataItem item = new StarKnownDataItem();
                        var refClass = Find.NamedAsset<ReferenceClassification>(asset.ClassIds[i]);

                        item.IdentificationType = CelestialDataDisplayUtil.MapTypeToLabel(refClass.Type);

                        switch (refClass.Type)
                        {
                            case ClassificationTypeMask.Photometer:
                                item.Classification = refClass.Label;
                                break;
                            case ClassificationTypeMask.ColorMeter:
                                item.Classification = refClass.Label;
                                break;
                            case ClassificationTypeMask.Spectrometer:
                                item.Classification = SpectrographUtility.ToSymbolsString(star.Elements);
                                break;
                            case ClassificationTypeMask.Luminosity:
                                item.Classification = refClass.Label;
                                break;
                            default:
                                break;
                        }
                        knownData.Data.Add(item);
                    }
                }
            }

            m_JsonBuilder.Clear();
            m_Log.BeginEvent("click_select_star");
            m_Log.EventParamJson("star_data", star.Append(m_JsonBuilder).End());
            m_Log.EventParamJson("known_data", knownData.Append(m_JsonBuilder).End());
            m_Log.SubmitEvent();
        }

        //tool_unlocked/
        //* tool_name
        private void LogToolUnlocked(StringHash32 toolId) {

            m_TempStr = ScriptUtility.FindActor(toolId).Source;

            // messy handling because we don't distinguish between wavelength filters and any other type of tool in code
            if (m_TempStr.Equals("VisibleWavelength") || m_TempStr.Equals("BlueWavelength") || m_TempStr.Equals("InfraredWavelength")) {
                if (m_UnlockedFilters.Contains(m_TempStr)) { return; }

                LogWavelengthFilterUnlocked(m_TempStr);
            }
            else {

                if (m_UnlockedTools.Contains(m_TempStr)) { return; }

                m_Log.BeginEvent("tool_unlocked");
                m_Log.EventParam("tool_name", ScriptUtility.FindActor(toolId).Source);
                m_Log.SubmitEvent();

                UpdateUnlockedTools(ScriptUtility.FindActor(toolId).Source);
            }
        }

        //wavelength_filter_unlocked/
        //* wavelength_type
        private void LogWavelengthFilterUnlocked(string filterName) {
            m_Log.BeginEvent("wavelength_filter_unlocked");
            m_Log.EventParam("wavelength_type", filterName);
            m_Log.SubmitEvent();

            UpdateUnlockedFilters(filterName);
        }

        ////TODO : events for scripted camera movements/view, maybe a switch_view? with view_node?

        //turn_telescope/
        //* direction
        //* new_orientation (initial orientation capture in game state should be orientation when key was pressed, new_orientation is when it was released)
        private void LogTurnTelescope(string dir) {
            TelescopeRig rig = Find.State<TelescopeRig>();
            TelescopeOrientationData orientation = new TelescopeOrientationData(
                rig.Base.localRotation.x,
                rig.Base.localRotation.y,
                rig.Base.localRotation.z,
                rig.Base.localRotation.w
                );

            m_JsonBuilder.Clear();
            m_Log.BeginEvent("turn_telescope");
            m_Log.EventParam("direction", dir);
            m_Log.EventParamJson("new_orientation", orientation.Append(m_JsonBuilder).End());
            m_Log.SubmitEvent();

            UpdateTelescopeOrientation(orientation);
        }

        //telescope_view_assigned/
        //* constellation_id
        //* goal_orientation

        private void LogTelescopeViewAssigned(TelescopeViewLogData logData) {
            m_Log.BeginEvent("telescope_view_assigned");
            m_Log.EventParam("constellation_id", EnumLookup.ConstellationType[(int)logData.Constellation]);
            m_Log.EventParam("goal_orientation", logData.Goal.ToString()); // TODO: no alloc stringify Vector3
            m_Log.SubmitEvent();
        }


        //locator_closer/
        //* new_proximity
        private void LogLocatorCloser(int newProximity) {
            m_Log.BeginEvent("locator_closer");
            m_Log.EventParam("new_proximity", newProximity);
            m_Log.SubmitEvent();
        }

        //locator_further/
        //* new_proximity
        private void LogLocatorFurther(int newProximity) {
            m_Log.BeginEvent("locator_further");
            m_Log.EventParam("new_proximity", newProximity);
            m_Log.SubmitEvent();
        }

        //found_telescope_view/
        //* constellation_id
        //* constellation: list[star_id]
        private void LogFoundTelescopeView(ConstellationData constellation) {
            m_Log.BeginEvent("found_telescope_view");
            m_Log.EventParam("constellation_id", constellation.Id);
            //m_Log.EventParamJson("constellation", constellation.Stars);
            // TODO: update with json
            m_Log.SubmitEvent();
        }

        //constellation_identification_assigned/
        //* constellation_id
        //* constellation : [star_id]
        //* points_needed
        //* identification_type
        private void LogConstellationIdAssigned(ConstellationData constellation, int pointsNeeded, ClassificationTypeMask IdType) {
            m_Log.BeginEvent("constellation_identification_assigned");
            m_Log.EventParam("constellation_id", constellation.Id);
            //m_Log.EventParamJson("constellation", constellation.Stars);
            // TODO: update with json
            m_Log.SubmitEvent();
        }

        //click_open_reference_guide/
        //* page_id
        private void LogClickOpenRefGuide(int pageId) {
            m_Log.BeginEvent("click_open_reference_guide");
            m_Log.EventParam("page_id", pageId);
            m_Log.SubmitEvent();
        }

        //click_dismiss_reference_guide/
        //* page_id
        private void LogClickDismissRefGuide(int pageId) {
            m_Log.BeginEvent("click_dismiss_reference_guide");
            m_Log.EventParam("page_id", pageId);
            m_Log.SubmitEvent();
        }

        //zoom_reference_guide/
        //* page_id
        private void LogZoomReferenceGuide(int pageId) {
            m_Log.BeginEvent("zoom_reference_guide");
            m_Log.EventParam("page_id", pageId);
            m_Log.SubmitEvent();
        }

        //unzoom_refence_guide/
        //* page_id
        private void LogUnzoomReferenceGuide(int pageId) {
            m_Log.BeginEvent("unzoom_reference_guide");
            m_Log.EventParam("page_id", pageId);
            m_Log.SubmitEvent();
        }

        //select_reference_guide_tab/
        //* tab_name
        //* new_page_id
        private void LogSelectRefGuideTab(string tabName, int newPageId) {
            m_Log.BeginEvent("unzoom_reference_guide");
            m_Log.EventParam("tab_name", tabName);
            m_Log.EventParam("new_page_id", newPageId);
            m_Log.SubmitEvent();
        }

        //turn_reference_page/
        //* direction : left | right
        //* new_page_id
        private void LogTurnRefGuidePage(bool isLeft, int newPageId) {
            m_Log.BeginEvent("turn_reference_page");
            m_Log.EventParam("direction", isLeft ? "LEFT" : "RIGHT");
            m_Log.EventParam("new_page_id", newPageId);
            m_Log.SubmitEvent();
        }

        //select_classification
        //* category : identification_category
        //* classification: Union[the specific category enums]
        private void LogSelectClassification(ClassificationTypeMask type, string classification) {
            m_Log.BeginEvent("select_classification");
            m_Log.EventParam("category", EnumLookup.FirstClassificationType(type));
            m_Log.EventParam("classification", classification);
            m_Log.SubmitEvent();
        }

        //toggle_spectral_element
        //* toggle : ON | OFF
        //* new_spectral_selection: List[element ID]
        private void LogToggleSpectralElement(bool toggle, SpectrographMaterialMask mask) {
            m_Log.BeginEvent("toggle_spectral_element");
            m_Log.EventParam("toggle", toggle ? "ON" : "OFF");
            m_Log.EventParam("new_spectral_selection", SpectrographUtility.ToSymbolsString(mask)); // TODO: make JSON elements list
            m_Log.SubmitEvent();
        }

        //click_submit_star_identification
        //* star_id
        //* category
        //* classification : str | List[element ID]
        private void LogClickSubmitStarId (string starId, ClassificationTypeMask type, string classification) {
            m_Log.BeginEvent("click_submit_star_identification");
            m_Log.EventParam("star_id", starId);
            m_Log.EventParam("category", EnumLookup.FirstClassificationType(type));
            m_Log.EventParam("classification", classification);
            m_Log.SubmitEvent();
        }

        //star_identification_accepted
        //* star_id
        //* category
        //* earned_point
        private void LogStarIdAccepted(string starId, ClassificationTypeMask type, bool scoredPoint) {
            m_Log.BeginEvent("click_submit_star_identification");
            m_Log.EventParam("star_id", starId);
            m_Log.EventParam("category", EnumLookup.FirstClassificationType(type));
            m_Log.EventParam("earned_point", scoredPoint);
            m_Log.SubmitEvent();
        }

        //star_identification_rejected
        //* star_id
        //* category
        //* classification : str | List[element ID]
        //* correct_classification
        private void LogStarIdRejected(string starId, ClassificationTypeMask type, string classification, string correctClassification) {
            m_Log.BeginEvent("star_identification_rejected");
            m_Log.EventParam("star_id", starId);
            m_Log.EventParam("category", EnumLookup.FirstClassificationType(type));
            m_Log.EventParam("classification", classification);
            m_Log.EventParam("correct_classification", correctClassification);
            m_Log.SubmitEvent();
        }

        //points_needed_displayed
        //* points_needed
        //* points_earned
        private void LogPointsNeededDisplayed (int ptsNeeded, int ptsEarned) {
            m_Log.BeginEvent("points_needed_displayed");
            m_Log.EventParam("points_needed", ptsNeeded);
            m_Log.EventParam("points_earned", ptsEarned);
            m_Log.SubmitEvent();
        }

        //start_radio_adjust
        //* radio_frequency
        private void LogStartRadioAdjust(int freq) {
            m_Log.BeginEvent("start_radio_adjust");
            m_Log.EventParam("radio_frequency", freq);
            m_Log.SubmitEvent();
        }

        //end_radio_adjust
        //* radio_frequency
        private void LogEndRadioAdjust(int freq) {
            m_Log.BeginEvent("end_radio_adjust");
            m_Log.EventParam("radio_frequency", freq);
            m_Log.SubmitEvent();
        }

        //radio_secret_found
        //* message_id
        //* radio_frequency
        private void LogRadioSecretFound(string msgId, int freq) {
            m_Log.BeginEvent("radio_secret_found");
            m_Log.EventParam("message_id", msgId);
            m_Log.EventParam("radio_frequency", freq);
            m_Log.SubmitEvent();
        }

        //telescope_stencil_displayed
        //* constellation_id
        //* constellation : List[star_id]
        //* connected_stars : List[Pair[star_id]]
        private void LogTelescopeStencilDisplayed(ConstellationData constellation, List<Tuple<string, string>> StarPairs) {
            m_Log.BeginEvent("telescope_stencil_displayed");
            m_Log.EventParam("constellation_id", constellation.Id);
            //m_Log.EventParamJson("constellation", constellation.Stars);
            //m_Log.EventParamJson("connected_stars", StarPairs)
            // TODO: update with json
            m_Log.SubmitEvent();
        }

        //logic_puzzle_start
        //* puzzle_info : List[str]
        //* puzzle_id
        //* puzzle_contents : List[Dict]
        //    * name
        //    * coords
        //    * value
        //    * is_filled
        //    * color/app mag/blue/ir
        private void LogLogicPuzzleStart(PuzzleData puzzle) {
            m_Log.BeginEvent("logic_puzzle_start");
            //m_Log.EventParamJson("puzzle_info", clues); //TODO: json array
            m_Log.EventParam("puzzle_id", puzzle.Id);
            //m_Log.EventParamJson("puzzle_contents", puzzle.Rows); //TODO: json array of structs
            m_Log.SubmitEvent();
        }

        //logic_puzzle_complete
        //* puzzle_id
        private void LogLogicPuzzleComplete(string puzzleId) {
            m_Log.BeginEvent("logic_puzzle_complete");
            m_Log.EventParam("puzzle_id", puzzleId);
            m_Log.SubmitEvent();
        }

        //click_tool_load
        //* tool_name
        //* star_id
        //* value
        private void LogClickToolLoad(PacketTransferData data) {
            m_Log.BeginEvent("click_tool_load");
            m_Log.EventParam("tool_name", EnumLookup.InstrumentType[(int)data.ToolId]);
            m_Log.EventParam("star_id", data.StarId);
            m_Log.EventParam("value", data.Value.ToString());
            m_Log.SubmitEvent();
        }

        //select_puzzle_cell
        //* tool_name
        //* star_id
        //* value : Optional[float | str]
        private void LogSelectPuzzleCell(PacketTransferData data) {
            m_Log.BeginEvent("select_puzzle_cell");
            m_Log.EventParam("tool_name", EnumLookup.InstrumentType[(int)data.ToolId]);
            m_Log.EventParam("star_id", data.StarId);
            m_Log.EventParam("value", data.Value.ToString());
            m_Log.SubmitEvent();
        }

        //transfer_value_to_cell
        //* tool_name
        //* star_id // TODO: disambiguate. is this star_id the id of the row that the data was transferred into?
        //* value
        //* source_star
        private void LogTransferValueToCell(PacketTransferData data) {
            m_Log.BeginEvent("transfer_value_to_cell");
            m_Log.EventParam("tool_name", EnumLookup.InstrumentType[(int)data.ToolId]);
            m_Log.EventParam("star_id", data.StarId);
            m_Log.EventParam("value", data.Value.ToString());
            m_Log.EventParam("source_star", data.StarId);
            m_Log.SubmitEvent();
        }

        //click_submit_puzzle
        //* puzzle_id
        //* puzzle_contents
        private void LogClickSubmitPuzzle(PuzzleData puzzle) {
            m_Log.BeginEvent("click_submit_puzzle");
            m_Log.EventParam("puzzle_id", puzzle.Id);
            // m_Log.EventParamJson("puzzle_contents", puzzle.Rows); // TODO: json array of structs
            m_Log.SubmitEvent();
        }

        //logic_puzzle_accepted
        //* puzzle_id
        private void LogLogicPuzzleAccepted(string puzzleId) {
            m_Log.BeginEvent("logic_puzzle_accepted");
            m_Log.EventParam("puzzle_id", puzzleId);
            m_Log.SubmitEvent();
        }

        //logic_puzzle_rejected
        //* puzzle_id
        //* incorrect_stars : List[star_id]
        //* new_puzzle_contents
        private void LogLogicPuzzleRejected(string puzzleId, string[] wrongStars, PuzzleRowData[] contents) {
            m_Log.BeginEvent("logic_puzzle_rejected");
            m_Log.EventParam("puzzle_id", puzzleId);
            m_Log.EventParam("incorrect_stars", puzzleId);
            // m_Log.EventParamJson("new_puzzle_contents", contents); // TODO: json array of structs
            m_Log.SubmitEvent();
        }

        //new_document_received
        //* document_id
        //* text_content
        private void LogNewDocReceived(DocumentData doc) {
            m_Log.BeginEvent("new_document_received");
            m_Log.EventParam("document_id", doc.Title);
            m_Log.EventParam("text_content", doc.Contents);
            m_Log.SubmitEvent();
        }

        //click_document_flip
        //* document_id
        //* to_side : FRONT | BACK
        //* text_content
        private void LogClickDocumentFlip(DocumentData doc, bool toFront) {
            m_Log.BeginEvent("click_document_flip");
            m_Log.EventParam("document_id", doc.Title);
            m_Log.EventParam("to_side", toFront ? "FRONT" : "BACK");
            m_Log.EventParam("text_content", doc.Contents);
            m_Log.SubmitEvent();
        }

        //click_dismiss_document
        private void LogClickDismissDocument() {
            m_Log.BeginEvent("click_dismiss_document");
            //m_Log.EventParam("document_id", docId); // would this be useful?
            m_Log.SubmitEvent();
        }


        //click_view_document
        //* document_id
        //* text_content
        private void LogClickViewDocument(DocumentData doc) {
            m_Log.BeginEvent("new_document_received");
            m_Log.EventParam("document_id", doc.Title);
            m_Log.EventParam("text_content", doc.Contents);
            m_Log.SubmitEvent();
        }


        //postit_recieved
        //* postit_id
        //* text_content
        private void LogPostitReceived(DocumentData doc) {
            m_Log.BeginEvent("postit_received");
            m_Log.EventParam("postit_id", doc.Title);
            m_Log.EventParam("text_content", doc.Contents);
            m_Log.SubmitEvent();
        }

        //click_dismiss_postit
        //* postit_id
        private void LogClickDismissPostit(string docId) {
            m_Log.BeginEvent("click_dismiss_postit");
            m_Log.EventParam("postit_id", docId);
            m_Log.SubmitEvent();
        }

        //grab_post_it
        //* postit_id
        private void LogGrabPostit(string docId) {
            m_Log.BeginEvent("grab_post_it");
            m_Log.EventParam("postit_id", docId);
            m_Log.SubmitEvent();
        }

        //place_post_it
        //* postit_id
        //* target_id : DocumentID | BOARD
        //* correct_target : DocumentID
        private void LogGrabPostit(string docId, string targetId, string correctTarget) {
            m_Log.BeginEvent("place_post_it");
            m_Log.EventParam("postit_id", docId);
            m_Log.EventParam("target_id", targetId);
            m_Log.EventParam("correct_target", correctTarget);
            m_Log.SubmitEvent();
        }

        //post_it_match_accepted
        //* postit_id
        private void LogPostitMatchAccepted(string docId) {
            m_Log.BeginEvent("post_it_match_accepted");
            m_Log.EventParam("postit_id", docId);
            m_Log.SubmitEvent();
        }

        //post_it_match_rejected
        //* postit_id
        //* target_id : DocumentID | BOARD
        //* correct_target : DocumentID
        private void LogPostitMatchRejected(string docId, string targetId, string correctTarget) {
            m_Log.BeginEvent("post_it_match_rejected");
            m_Log.EventParam("postit_id", docId);
            m_Log.EventParam("target_id", targetId);
            m_Log.EventParam("correct_target", correctTarget);
            m_Log.SubmitEvent();
        }

        //toggle_magnitude_mode:
        //* new_mode : ABSOLUTE | RELATIVE
        private void LogToggleMagMode(bool modeIsAbsolute) {
            m_Log.BeginEvent("toggle_magnitude_mode");
            m_Log.EventParam("new_mode", modeIsAbsolute ? "ABSOLUTE" : "RELATIVE");
            m_Log.SubmitEvent();
        }

        //switch_player_view:
        //* view_id : DOCUMENTS | INSTRUMENTS | MAP | FREE
        private void LogSwitchPlayerView(string viewId) {
            m_Log.BeginEvent("switch_player_view");
            m_Log.EventParam("view_id", viewId);
            m_Log.SubmitEvent();
        }


        // NEW EVENTS AFTER THIS - check with data team

        //set_volume:
        //* audio_bus: MASTER | VOICE | MUSIC | SFX\
        //* new_volume: float
        private void LogSetVolume(string bus, float val) {
            m_Log.BeginEvent("set_volume");
            m_Log.EventParam("audio_bus", bus);
            m_Log.EventParam("new_volume", val);
            m_Log.SubmitEvent();
        }
        //toggle_fullscreen:
        //*  toggled: ON | OFF
        private void LogToggleFullscreen(bool toggledOn) {
            m_Log.BeginEvent("toggle_fullscreen");
            m_Log.EventParam("toggled", toggledOn ? "ON" : "OFF");
            m_Log.SubmitEvent();
        }

        //toggle_camera_drift:
        //* toggled: ON | OFF
        private void LogToggleCamDrift(bool toggledOn) {
            m_Log.BeginEvent("toggle_camera_drift");
            m_Log.EventParam("toggled", toggledOn ? "ON" : "OFF");
            m_Log.SubmitEvent();
        }

        //set_decoder_letter:
        //* index: int (0 to 4)
        //* letter: char
        //* new_sequence: string
        private void LogSetDecoderLetter(int idx, char letter, string seq) {
            m_Log.BeginEvent("set_decoder_letter");
            m_Log.EventParam("index", idx);
            m_Log.EventParam("letter", letter);
            m_Log.EventParam("new_sequence", seq);
            m_Log.SubmitEvent();
        }

        //submit_decoder_guess:
        //* sequence: string
        private void LogSubmitDecoderGuess(string seq) {
            m_Log.BeginEvent("submit_decoder_guess");
            m_Log.EventParam("sequence", seq);
            m_Log.SubmitEvent();
        }
        //decoder_guess_accepted
        private void LogDecoderGuessAccepted() {
            m_Log.NewEvent("decoder_guess_accepted");
        }

        //decoder_guess_rejected
        private void LogDecoderGuessRejected() {
            m_Log.NewEvent("decoder_guess_rejected");
        }


        #endregion // Logging


    }

    #region Data Enums
    [Flags]
    public enum WavelengthTypeMask {
        /// Be sure to update EnumLookup if this is updated!
        VISIBLE = 0x1, 
        BLUE = 0x2, 
        INFRARED = 0x4, 
        ABSOLUTE = 0x8
    }

    [Flags]
    public enum InstrumentTypeMask {
        /// Be sure to update EnumLookup if this is updated!
        PHOTOMETER = 0x01, 
        COLOR_METER = 0x02, 
        TEMPERATURE_METER = 0x04, 
        SPECTROMETER = 0x08,
        PARALLAX = 0x10, 
        DECODER = 0x20
    }

    public enum MagnitudeMode {
        /// Be sure to update EnumLookup if this is updated!
        ABSOLUTE, RELATIVE
    }

    #endregion // Data Enums

    #region Enum Lookup
    public static class EnumLookup {
        public static readonly string[] WavelengthType = new string[] {
            "VISIBLE", "BLUE", "INFRARED", "ABSOLUTE"
        };
        public static readonly string[] InstrumentType = new string[] {
            "PHOTOMETER", "COLOR_METER", "TEMPERATURE_METER", "SPECTROMETER", "PARALLAX", "DECODER"
        };
        public static readonly string[] MagnitudeMode = new string[] {
            "ABSOLUTE", "RELATIVE"
        };
        public static readonly string[] ClassificationType = new string[] {
            "BRIGHTNESS", "SPECTRAL_TYPE", "ELEMENTS", "HISTORICAL", "SPECTRAL_TYPE_DWARF", "LUMINOSITY"
        };
        public static readonly string[] ConstellationType = new string[] {
            "URSA_MAJOR", "ANDROMEDA", "DRACO", "ERIDANUS", "HERCULES", "HYDRA", "LEO", "ORION", "PERSEUS", "TAURUS"
        };
        public static string FirstClassificationType(ClassificationTypeMask type) {
            foreach (var bit in Bits.Enumerate(type)) {
                return ClassificationType[(int)bit];
            }
            return "NONE";
        }

        // Bits.Enumerate(flags): indices of all the flags. for each, convert the index back to a string
    }
    #endregion // Enum Lookup

    #region Data Structs
    [Serializable]
    public struct StarLogData {
        public string Name;
        public EqCoords Coordinates;
        public double Distance;
        public double ColorIndex;
        public uint Temperature;
        public float VisMagnitude;
        public float BlueMagnitude;
        public float InfraredMagnitude;
        public float AbsoluteMagnitude;
        public SpectrographMaterialMask Elements;
        public bool IsHighlighted;

        public StringHash32 AssetID;

        public readonly JsonBuilder Append(JsonBuilder json)
        {
            json.Field("star_id", Name);
            json.BeginObject("coordinates");
            Coordinates.Append(json).EndObject();
            json.Field("distance", Distance);
            json.Field("color", ColorIndex);
            json.Field("temperature", Temperature);
            json.Field("visible_magnitude", VisMagnitude);
            json.Field("blue_magnitude", BlueMagnitude);
            json.Field("infrared_magnitude", InfraredMagnitude);
            json.Field("absolute_magnitude", AbsoluteMagnitude);
            json.Field("spectral_elements", SpectrographUtility.ToSymbolsString(Elements));
            json.Field("is_highlighted", IsHighlighted);
            return json;
        }
    }

    [Serializable]
    public struct TelescopeOrientationData {
        public float W;
        public float X;
        public float Y;
        public float Z;

        public TelescopeOrientationData(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public readonly JsonBuilder Append(JsonBuilder json)
        {
            json.Field("x", X);
            json.Field("y", Y);
            json.Field("z", Z);
            json.Field("w", W);
            return json;
        }
    }

    [Serializable]
    public struct TelescopeViewLogData
    {
        public ConstellationId Constellation;
        public Vector3 Goal;
    }

    [Serializable] // more portable version of PuzzleAsset?
    public struct PuzzleData {
        public string Id;
        public string[] Clues;
        public PuzzleRowData[] Rows;
    }

    [Serializable]
    public struct PuzzleRowData {
        public string Name;
        public EqCoords Coordinates;
        bool IsFilled;
        // color
        // apparent magnitude

        //public string ToJsonString() {
        //    // TODO: json translation
        //    return "";
        //}
    }

    [Serializable]
    public struct PuzzleCellData {
        public DataTypeMask DataType;
        public string Data;

        //public string ToJsonString() {
        //    // TODO: json translation
        //    return "";
        //}
    }

    [Serializable]
    public struct StarKnownDataItem {
        public string IdentificationType; //  classification type (e.g. "Spectral Type")
        public string Classification; // specific classification (e.g. "A Type")

        public readonly JsonBuilder Append(JsonBuilder json)
        {
            json.Field("identification_type", IdentificationType);
            json.Field("category", Classification);
            return json;
        }
    }

    [Serializable]
    public struct StarKnownData
    {
        public List<StarKnownDataItem> Data;

        public readonly JsonBuilder Append(JsonBuilder json)
        {
            foreach(var item in Data)
            {
                json.BeginObject("star_known_datum");
                item.Append(json).EndObject();
            }

            return json;
        }
    }

    [Serializable]
    public struct ConstellationData {
        public string Id;
        public List<string> Stars;

    }

    [Serializable]
    public struct PacketTransferData {
        public string StarId;
        public InstrumentTypeMask ToolId;
        public Variant Value;
    }

    [Serializable]
    public struct DocumentData {
        public string Title;
        public string Contents;
    }

    [Serializable]
    public struct SubtitleLogData
    {
        public StringSlice LineId;
        public string ScriptContent;
        public SerializedHash32 CharacterId;
    }


    [Serializable]
    public struct HintLogData
    {
        public string Id;
        public string Content;
    }

    #endregion //Data Structs

}