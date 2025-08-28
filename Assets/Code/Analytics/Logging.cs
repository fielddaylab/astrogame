

using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
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
        private InstrumentTypeMask m_UnlockedTools; // list of tool_id: name for each instrument 
        private WavelengthTypeMask m_UnlockedFilters; // list of filter_type: name for each filter
        private TelescopeOrientationData m_TelescopeOrientation; // quaternion? TODO: check in with Luke about expected format
        private int m_LocatorProximity;
        private WavelengthTypeMask m_WavelengthFilter;
        private bool m_MagnitudeModeIsApparent;
        private int m_PointsNeeded;
        private int m_PointsEarned;
        private StarData m_SelectedStar;
        private PuzzleData m_LogicPuzzle;


        private void SubmitGameState() {
            m_JsonBuilder.Begin()
                .Field("current_level", m_CurrentLevel)
                .BeginArray("unlocked_tools");
            foreach (var instrument in Bits.Enumerate(m_UnlockedTools)) { 
                m_JsonBuilder.Item(EnumLookup.InstrumentType[(byte)instrument]);
            }
            m_JsonBuilder.EndArray();
            m_JsonBuilder.BeginArray("unlocked_filters");
            foreach (var filter in Bits.Enumerate(m_UnlockedFilters)) { 
                m_JsonBuilder.Item(EnumLookup.WavelengthType[(byte)filter]);
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

        private void UpdateUnlockedTools(InstrumentTypeMask unlockedTool) {
            m_UnlockedTools |= unlockedTool;
            SubmitGameState();
        }

        private void UpdateUnlockedFilters(WavelengthTypeMask unlockedFilter) {
            m_UnlockedFilters |= unlockedFilter;
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

        private void UpdateSelectedStar(StarData star) {
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
            AstroGame.Events
                .Register<string>(GameEvents.TitleGameStarting, SetAnalyticsUserCode)
                .Register(GameEvents.TitleNewGameClicked, LogClickNewGame)
                .Register(GameEvents.TitleContinueGameClicked, LogClickContinueGame)
                .Register(GameEvents.TitleOptionsClicked, LogClickOptionsMenu)
                .Register<bool>(GameEvents.GameStart, LogGameStart)
                .Register<int>(GameEvents.BeginLevel, LogBeginLevel)
                ;
        }
        #endregion

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

        //click_free_play_menu (pending implementation)/
        private void LogClickFreePlayMenu() {
            // m_Log.NewEvent("click_options_menu");
        }
        
        //game_start/
        //* from_resume
        private void LogGameStart(bool fromResume) {
            m_Log.BeginEvent("game_start");
            m_Log.EventParam("from_resume", fromResume);
            m_Log.SubmitEvent();
        }
       
        //click_begin_level/
        //* level_number
        private void LogBeginLevel(int levelNum) {
            m_Log.BeginEvent("begin_level");
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
        private void LogDialogueAudioStart(string lineId, string scriptContent, string speakerId) {
            // TODO: this one's gonna be annoying
            m_Log.BeginEvent("dialog_audio_start");
            m_Log.EventParam("line_id", lineId);
            m_Log.EventParam("script_content", scriptContent);
            m_Log.EventParam("speaker_id", speakerId);
            m_Log.SubmitEvent();
        } 

        //dialog_audio_end/
        //* line_id
        //* speaker_id
        private void LogDialogueAudioEnd(string lineId, string speakerId) {
            m_Log.BeginEvent("dialog_audio_end");
            m_Log.EventParam("line_id", lineId);
            m_Log.EventParam("speaker_id", speakerId);
            m_Log.SubmitEvent();
        }

        //dialog_text_displayed/
        //* line_id
        //* script_content
        //* speaker_id
        private void LogDialogueTextDisplayed(string lineId, string scriptContent, string speakerId) {
            m_Log.BeginEvent("dialog_text_displayed");
            m_Log.EventParam("line_id", lineId);
            m_Log.EventParam("script_content", scriptContent);
            m_Log.EventParam("speaker_id", speakerId);
            m_Log.SubmitEvent();
        }

        //click_skip_dialog_line/
        //* line_id
        //* speaker_id
        private void LogClickSkipDialogueLine(string lineId, string speakerId) {
            m_Log.BeginEvent("click_skip_dialog_line");
            m_Log.EventParam("line_id", lineId);
            m_Log.EventParam("speaker_id", speakerId);
            m_Log.SubmitEvent();
        }

        //hint_displayed/
        //* hint_id
        //* text_content
        private void LogHintDisplayed(string hintId, string textContent) {
            m_Log.BeginEvent("hint_displayed");
            m_Log.EventParam("hint_id", hintId);
            m_Log.EventParam("text_content", textContent);
            m_Log.SubmitEvent();
        }

        //hint_hidden/
        //* hint_id
        private void LogHintHidden(string hintId) {
            m_Log.BeginEvent("hint_hidden");
            m_Log.EventParam("hint_id", hintId);
            m_Log.SubmitEvent();
        }

        //star_assigned/
        //* star_id
        private void LogStarAssigned(string starId) {
            m_Log.BeginEvent("star_assigned");
            m_Log.EventParam("star_id", starId);
            m_Log.SubmitEvent();
        }

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
        //* known_data
        //    * identification_type
        //    * category
        private void LogClickSelectStar(StarData star, List<StarKnownData> knownData) {
            m_Log.BeginEvent("click_select_star");
            // m_Log.EventParamJson("star_id", star);
            // TODO: json representation of StarData
            //m_Log.EventParam("known_data", knownData);
            // TODO: json representation of KnownData
        }

        //tool_unlocked/
        //* tool_name
        private void LogToolUnlocked(string toolName) {
            m_Log.BeginEvent("tool_unlocked");
            m_Log.EventParam("tool_name", toolName);
            m_Log.SubmitEvent();
        }

        //wavelength_filter_unlocked/
        //* wavelength_type
        private void LogWavelengthFilterUnlocked(string filterName) {
            m_Log.BeginEvent("wavelength_filter_unlocked");
            m_Log.EventParam("wavelength_type", filterName);
            m_Log.SubmitEvent();
        }

        ////TODO : events for scripted camera movements/view, maybe a switch_view? with view_node?

        //turn_telescope/
        //* direction
        //* new_orientation (initial orientation capture in game state should be orientation when key was pressed, new_orientation is when it was released)
        private void LogTurnTelescope(string dir, TelescopeOrientationData newOrientation) {
            m_Log.BeginEvent("turn_telescope");
            m_Log.EventParam("direction", dir);
            //m_Log.EventParamJson("new_orientation", newOrientation.ToString());
            // TODO: update with json
            m_Log.SubmitEvent();
        }

        //telescope_view_assigned/
        //* constellation_id
        //* goal_orientation

        private void LogTelescopeViewAssigned(string constellation, TelescopeOrientationData goal) {
            m_Log.BeginEvent("telescope_view_assigned");
            m_Log.EventParam("constellation_id", constellation);
            //m_Log.EventParam("goal_orientation", goal.ToJson);
            //TODO: update with json
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
    public struct StarData {
        public string Name;
        public string Constellation;
        public EqCoords Coordinates;
        public int ColorIndex;
        public int Temperature;
        public float VisMagnitude;
        public float BlueMagnitude;
        public float InfraredMagnitude;
        public float AbsoluteMagnitude;
        public SpectrographMaterialMask Elements;
        public bool IsHighlighted;

        //public string ToJsonString() {
        //    // TODO: json translation
        //    return "";
        //}
    }

    [Serializable]
    public struct TelescopeOrientationData {
        public float W;
        public float X;
        public float Y;
        public float Z;

        //public string ToJsonString() {
        //    // TODO: json translation
        //    return "";
        //}
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
    public struct StarKnownData {
        public ClassificationTypeMask IdentificationType; //  classification type (e.g. "Spectral Type")
        public string Classification; // specific classification (e.g. "A Type")
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
 
    #endregion //Data Structs

}