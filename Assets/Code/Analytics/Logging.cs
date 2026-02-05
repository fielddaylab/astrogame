

using Astro.Radio;
using Astro.Reference;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Vox;
using OGD;
using System;
using System.Collections.Generic;
using System.Text;
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
        private CelestialObjectVisMask m_WavelengthFilter;
        private bool m_MagnitudeModeIsAbsolute;
        private int m_PointsNeeded;
        private int m_PointsEarned;
        private StarLogData m_SelectedStar;
        private PuzzleAsset m_LogicPuzzle = default;


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
            m_JsonBuilder.Field("wavelength_filter", EnumLookup.FirstWavelengthType(m_WavelengthFilter));
            m_JsonBuilder.Field("magnitude_mode", m_MagnitudeModeIsAbsolute ? "ABSOLUTE" : "APPARENT");
            m_JsonBuilder.Field("points_needed", m_PointsNeeded);
            m_JsonBuilder.Field("points_earned", m_PointsEarned);
            m_JsonBuilder.BeginObject("selected_star");
            m_SelectedStar.Append(m_JsonBuilder);
            m_JsonBuilder.EndObject();
            m_JsonBuilder.BeginObject("logic_puzzle");
            AppendLogicPuzzle(m_JsonBuilder);
            m_JsonBuilder.EndObject();

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

        private void UpdateCurrentFilter(CelestialObjectVisMask currentFilter) {
            m_WavelengthFilter = currentFilter;
            SubmitGameState();
        }

        private void UpdateMagnitudeMode(bool magIsAbsolute) {
            m_MagnitudeModeIsAbsolute = magIsAbsolute;
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
            if (!m_SelectedStar.IsValid && !star.IsValid) { return; }
            m_SelectedStar = star;
            SubmitGameState();
        }

        private void UpdateCurrentPuzzle(PuzzleAsset puzzle) {
            m_LogicPuzzle = puzzle;
            SubmitGameState();
        }

        private void AppendLogicPuzzle(JsonBuilder json) {
            if (m_LogicPuzzle == null) { return; }

            m_WorkingStrList.Items.Clear();
            m_WorkingStrList.FieldId = "clue";
            for (int i = 0; i < m_LogicPuzzle.ClueText.Length; i++) {
                m_WorkingStrList.Items.Add(m_LogicPuzzle.ClueText[i]);
            }

            m_WorkingPuzzleContentsData.Contents.Clear();
            for (int r = 0; r < m_LogicPuzzle.Rows.Length; r++) {
                var rowData = new PuzzleRowProvidedLogData();
                if ((m_LogicPuzzle.Rows[r].ProvidedProperties & DataTypeMask.Name) != 0) {
                    CelestialAsset asset = Find.NamedAsset<CelestialAsset>(m_LogicPuzzle.Rows[r].Object);
                    rowData.Name = asset.DisplayName;
                }
                if ((m_LogicPuzzle.Rows[r].ProvidedProperties & DataTypeMask.Coordinates) != 0) {
                    CelestialAsset asset = Find.NamedAsset<CelestialAsset>(m_LogicPuzzle.Rows[r].Object);
                    asset.Coords.Declination.Sanitize();
                    asset.Coords.RightAscension.Sanitize();
                    m_WorkingStringBuilder.Clear();
                    asset.Coords.RightAscension.ToString(m_WorkingStringBuilder);
                    m_WorkingStringBuilder.Append(",\n");
                    asset.Coords.Declination.ToString(m_WorkingStringBuilder);
                    rowData.Coords = m_WorkingStringBuilder.ToString();
                    m_WorkingStringBuilder.Clear();
                }

                m_WorkingPuzzleContentsData.Contents.Add(rowData);
            }

            json.BeginObject("puzzle_info");
            m_WorkingStrList.AppendItems(json);
            json.EndObject();
            // m_Log.EventParamJson("puzzle_info", m_WorkingStrList.AppendItems(m_JsonBuilder).End());

            m_WorkingStrList.Items.Clear();
            m_WorkingStrList.FieldId = "property";
            EnumLookup.GatherDataTypes(ref m_WorkingStrList.Items, m_LogicPuzzle.RequiredProperties);

            json.Field("puzzle_id", m_LogicPuzzle.DisplayName);
            json.BeginObject("puzzle_contents");
            m_WorkingPuzzleContentsData.AppendContents(json);
            json.EndObject();
            json.BeginArray("puzzle_properties");
            m_WorkingStrList.AppendItemsNoField(json);
            json.EndArray();
        }

        #endregion // Game State

        #region Initialization

        private void PrepareLogging() {
#if DEVELOPMENT
            m_Debug = true;
#endif // DEVELOPMENT

            m_Log = new OGDLog(CreateOGDConsts(), new OGDLog.MemoryConfig(2048 * 2, Unsafe.KiB * 64, 256));

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

            InitVars();
        }

        private void InitVars() {
            m_WorkingStrList.Items = new List<string>();
            m_WorkingStarEdgeList.Edges = new List<Tuple<string, string>>();
            m_WorkingPuzzleContentsData.Contents = new List<PuzzleRowProvidedLogData>();
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
                .Register<TelescopeViewLogData>(GameEvents.FoundTelescopeView, LogFoundTelescopeView)
                .Register<TelescopeViewLogData>(GameEvents.ConstellationIdAssigned, LogConstellationIdAssigned)
                .Register(GameEvents.ClickRefGuideOpened, LogClickOpenRefGuide)
                .Register(GameEvents.ClickRefGuideClosed, LogClickDismissRefGuide)
                .Register(GameEvents.RefGuideZoomed, LogZoomReferenceGuide)
                .Register(GameEvents.RefGuideUnzoomed, LogUnzoomReferenceGuide)
                .Register<string>(GameEvents.SelectRefGuideTab, LogSelectRefGuideTab)
                .Register<bool>(GameEvents.TurnRefGuidePage, LogTurnRefGuidePage)
                .Register<ClassificationLogData>(GameEvents.SelectClassification, LogSelectClassification)
                .Register<bool>(GameEvents.ToggleSpectralElement, LogToggleSpectralElement)
                .Register(GameEvents.ClickSubmitStarId, LogClickSubmitStarId)
                .Register(GameEvents.ValidOpenIdSubmission, LogValidOpenId)
                .Register(GameEvents.UnacceptedOpenIdSubmission, LogUnacceptedOpenId)
                .Register(GameEvents.ValidKnowledgeSubmission, LogUnacceptedOpenId)
                .Register(GameEvents.DuplicateOpenIdSubmission, LogStarIdRejected)
                .Register(GameEvents.IncorrectOpenIdSubmission, LogStarIdRejected)
                .Register(GameEvents.InvalidOpenIdSubmission, LogStarIdRejected)
                .Register<int>(GameEvents.PointsUpdated, LogPointsNeededDisplayed)
                .Register(GameEvents.StartAdjustRadio, LogStartRadioAdjust)
                .Register<int>(GameEvents.EndAdjustRadio, LogEndRadioAdjust)
                .Register<string>(GameEvents.RadioSecretFound, LogRadioSecretFound)
                .Register(GameEvents.TelescopeStencilDisplayed, LogTelescopeStencilDisplayed)
                .Register(GameEvents.AfterPuzzleModeStart, LogLogicPuzzleStart)
                .Register(GameEvents.LogicPuzzleModeComplete, LogLogicPuzzleComplete)
                .Register<PacketTransferData>(GameEvents.ClickToolLoad, LogClickToolLoad)
                .Register<PacketTransferData>(GameEvents.SelectPuzzleCell, LogSelectPuzzleCell)
                .Register<PacketTransferData>(GameEvents.TransferValueToCell, LogTransferValueToCell)
                .Register(GameEvents.ClickSubmitPuzzle, LogClickSubmitPuzzle)
                .Register(GameEvents.LogicPuzzleAccepted, LogLogicPuzzleAccepted)
                .Register<List<string>>(GameEvents.LogicPuzzleRejected, LogLogicPuzzleRejected)
                .Register(GameEvents.NewDocReceived, LogNewDocReceived)
                .Register<bool>(GameEvents.DocFlipped, LogClickDocumentFlip)
                .Register(GameEvents.DocDismissed, LogClickDismissDocument)
                .Register(GameEvents.DocViewed, LogClickViewDocument)
                .Register(GameEvents.NewPostitReceived, LogPostitReceived)
                .Register(GameEvents.PostitDismissed, LogClickDismissPostit)
                .Register(GameEvents.GrabPostit, LogGrabPostit)
                .Register<StringPair>(GameEvents.PlacePostit, LogPlacePostit)
                .Register(GameEvents.PostitMatchAccepted, LogPostitMatchAccepted)
                .Register(GameEvents.PostitMatchRejected, LogPostitMatchRejected)
                .Register(GameEvents.ToggleMagMode, LogToggleMagMode)
                .Register(GameEvents.SwitchPlayerView, LogSwitchPlayerView)
                ;

            // state update events
            AstroGame.Events
                .Register<SubtitleLogData>(GameEvents.SubtitleDataChanged, HandleSubtitleDataChanged)
                .Register<HintLogData>(GameEvents.HintChanged, HandleHintChanged)
                .Register<StringHash32>(GameEvents.RefGuideControlPageChanged, HandleRefGuideControlPageChanged)
                .Register(GameEvents.SubmittedStarChanged, HandleSubmittedStarChanged)
                .Register<string>(GameEvents.ActiveDocChanged, HandleActiveDocChanged)
                .Register<string>(GameEvents.LatestMovedDocChanged, HandleLatestMovedDocChanged)
                .Register<ViewNode>(GameEvents.ViewChanged, HandleViewChanged)
                .Register(GameEvents.MonitorEmptySpaceClicked, HandleMonitorEmptySpaceClicked)
                .Register(GameEvents.StopOpenMode, HandleStopOpenMode)
                .Register<CelestialObjectVisMask>(GameEvents.MonitorSwitchedFilter, HandleFilterSwitched)
                ;

        }
        #endregion

        #region Logging Variables

        [NonSerialized] private SubtitleLogData m_LastKnownSubtitleData = default;
        [NonSerialized] private HintLogData m_LastKnownHint = default;
        [NonSerialized] private StringHash32 m_LastKnownControlPageId = default;
        [NonSerialized] private string m_LastKnownControlPageName = default;
        [NonSerialized] private StarLogData m_LastKnownMonitorSelectedStar = default;

        [NonSerialized] private CelestialAsset m_LastKnownSubmittedStarAsset = default;
        [NonSerialized] private ReviewSubmissionClassification m_LastKnownReviewSubmissionClassification = default;
        [NonSerialized] private ReferenceClassification m_LastKnownSubmittedRefClassification = default;
        
        [NonSerialized] private string m_LastKnownDocTitle = default;
        [NonSerialized] private string m_LastKnownMovedDoc = default;
        [NonSerialized] private StringPair m_LastKnownPostitTargetPair = default;

        [NonSerialized] private string m_LastKnownViewNodeName = default;

        private string m_TempStr;
        private StringList m_WorkingStrList = new StringList();
        private StarEdgeList m_WorkingStarEdgeList = new StarEdgeList();
        private PuzzleContentsLogData m_WorkingPuzzleContentsData = new PuzzleContentsLogData();
        private StringBuilder m_WorkingStringBuilder = new StringBuilder();

        private static string LEFT = "LEFT";
        private static string RIGHT = "RIGHT";
        private static string ON = "ON";
        private static string OFF = "OFF";

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

        private void HandleRefGuideControlPageChanged(StringHash32 id) {
            m_LastKnownControlPageId = id;
            m_LastKnownControlPageName = Find.NamedAsset<ReferencePageAsset>(m_LastKnownControlPageId).name;
        }

        private void HandleSubmittedStarChanged() {
            RefGuideState rgs = Find.State<RefGuideState>();
            ReviewState pps = Find.State<ReviewState>();
            m_LastKnownSubmittedStarAsset = Find.NamedAsset<CelestialAsset>(pps.Identification.AssetId);
            m_LastKnownReviewSubmissionClassification = pps.Identification;
            m_LastKnownSubmittedRefClassification = rgs.SelectedRefClassification;
        }

        private void HandleActiveDocChanged(string docTitle) {
            m_LastKnownDocTitle = docTitle;
        }

        private void HandleLatestMovedDocChanged(string docTitle) {
            m_LastKnownMovedDoc = docTitle;
        }

        private void HandleViewChanged(ViewNode node) {
            if (node.IsTitle) { return; }
            m_LastKnownViewNodeName = node.name.ToUpper();
        }

        private void HandleMonitorEmptySpaceClicked() {
            UpdateSelectedStar(default);
        }

        private void HandleStopOpenMode() {
            UpdatePointsNeeded(-1);
            UpdatePointsEarned(-1);
        }

        private void HandleFilterSwitched(CelestialObjectVisMask filter) {
            UpdateCurrentFilter(filter);
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
            m_LastKnownMonitorSelectedStar = star;
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
            m_JsonBuilder.Clear();
            m_Log.EventParamJson("known_data", knownData.Append(m_JsonBuilder).End());
            m_JsonBuilder.Clear();
            m_Log.SubmitEvent();

            UpdateSelectedStar(star);
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
            m_JsonBuilder.Clear();
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

            UpdateLocatorProximity(newProximity);
        }

        //locator_further/
        //* new_proximity
        private void LogLocatorFurther(int newProximity) {
            m_Log.BeginEvent("locator_further");
            m_Log.EventParam("new_proximity", newProximity);
            m_Log.SubmitEvent();

            UpdateLocatorProximity(newProximity);
        }

        //found_telescope_view/
        //* constellation_id
        //* constellation: list[star_id]
        private void LogFoundTelescopeView(TelescopeViewLogData logData) {
            m_JsonBuilder.Clear();
            m_Log.BeginEvent("found_telescope_view");
            m_Log.EventParam("constellation_id", EnumLookup.ConstellationType[(int)logData.Constellation]);
            m_Log.EventParamJson("constellation", logData.AppendStars(m_JsonBuilder).End());
            m_JsonBuilder.Clear();
            m_Log.SubmitEvent();
        }

        //constellation_identification_assigned/
        //* constellation_id
        //* constellation : [star_id]
        //* points_needed
        //* identification_type
        private void LogConstellationIdAssigned(TelescopeViewLogData logData) {
            DayConfigAsset config = DayConfigUtil.GetConfigForState();

            m_Log.BeginEvent("constellation_identification_assigned");
            m_Log.EventParam("constellation_id", EnumLookup.ConstellationType[(int)logData.Constellation]);
            m_JsonBuilder.Clear();
            m_Log.EventParamJson("constellation", logData.AppendStars(m_JsonBuilder).End());
            m_JsonBuilder.Clear();
            m_Log.EventParam("points_needed", config.NumNeutrinoPoints.ToStringLookup());
            m_Log.EventParam("identification_type", EnumLookup.FirstClassificationType(config.AcceptedIDSubmissions));
            m_Log.SubmitEvent();
        }

        //click_open_reference_guide/
        //* page_id
        private void LogClickOpenRefGuide() {
            m_Log.BeginEvent("click_open_reference_guide");
            m_Log.EventParam("page_id", m_LastKnownControlPageName);
            m_Log.SubmitEvent();
        }

        //click_dismiss_reference_guide/
        //* page_id
        private void LogClickDismissRefGuide() {
            m_Log.BeginEvent("click_dismiss_reference_guide");
            m_Log.EventParam("page_id", m_LastKnownControlPageName);
            m_Log.SubmitEvent();
        }

        //zoom_reference_guide/
        //* page_id
        private void LogZoomReferenceGuide() {
            m_Log.BeginEvent("zoom_reference_guide");
            m_Log.EventParam("page_id", m_LastKnownControlPageName);
            m_Log.SubmitEvent();
        }

        //unzoom_refence_guide/
        //* page_id
        private void LogUnzoomReferenceGuide() {
            m_Log.BeginEvent("unzoom_reference_guide");
            m_Log.EventParam("page_id", m_LastKnownControlPageName);
            m_Log.SubmitEvent();
        }

        //select_reference_guide_tab/
        //* tab_name
        //* new_page_id
        private void LogSelectRefGuideTab(string tabName) {
            m_Log.BeginEvent("select_reference_guide_tab");
            m_Log.EventParam("tab_name", tabName);
            m_Log.EventParam("new_page_id", m_LastKnownControlPageName);
            m_Log.SubmitEvent();
        }

        //turn_reference_page/
        //* direction : left | right
        //* new_page_id
        private void LogTurnRefGuidePage(bool isLeft) {
            m_Log.BeginEvent("turn_reference_page");
            m_Log.EventParam("direction", isLeft ? LEFT : RIGHT);
            m_Log.EventParam("new_page_id", m_LastKnownControlPageName);
            m_Log.SubmitEvent();
        }

        //select_classification
        //* category : identification_category
        //* classification: Union[the specific category enums]
        private void LogSelectClassification(ClassificationLogData data) {
            m_Log.BeginEvent("select_classification");
            m_Log.EventParam("category", EnumLookup.FirstClassificationType(data.Type));
            m_Log.EventParam("classification", data.Label);
            m_Log.SubmitEvent();
        }

        //toggle_spectral_element
        //* toggle : ON | OFF
        //* new_spectral_selection: List[element ID]
        private void LogToggleSpectralElement(bool toggle) {
            var rgs = Find.State<RefGuideState>();
            m_JsonBuilder.Clear();

            m_Log.BeginEvent("toggle_spectral_element");
            m_Log.EventParam("toggle", toggle ? ON : OFF);
            m_Log.EventParamJson("new_spectral_selection", SpectrographUtility.Append(rgs.SelectedMaterials, m_JsonBuilder).End());
            m_JsonBuilder.Clear();
            m_Log.SubmitEvent();
        }

        //click_submit_star_identification
        //* star_id
        //* category
        //* classification : str | List[element ID]
        private void LogClickSubmitStarId () {
            var rgs = Find.State<RefGuideState>();
            m_WorkingStringBuilder.Clear();

            if (!m_LastKnownReviewSubmissionClassification.Classification.IsEmpty) {
                var refClassification = Find.NamedAsset<ReferenceClassification>(m_LastKnownReviewSubmissionClassification.Classification);
                m_WorkingStringBuilder.Append(refClassification.Label);
            } else if (m_LastKnownReviewSubmissionClassification.Materials != 0) {
            m_JsonBuilder.Clear();
                m_WorkingStringBuilder.Append(SpectrographUtility.Append(m_LastKnownReviewSubmissionClassification.Materials, m_JsonBuilder).End().ToString());
                m_JsonBuilder.Clear();
            }

            m_Log.BeginEvent("click_submit_star_identification");
            m_Log.EventParam("star_id", m_LastKnownSubmittedStarAsset.DisplayName);
            m_Log.EventParam("category", EnumLookup.FirstClassificationType(rgs.SelectedRefClassification.Type));
            m_Log.EventParam("classification", m_WorkingStringBuilder.ToString());
            m_Log.SubmitEvent();

            m_WorkingStringBuilder.Clear();
        }

        // Wrapper for StarIdAccepted event
        private void LogValidOpenId() {
            LogStarIdAccepted(true);
        }

        // Wrapper for StarIdAccepted event
        private void LogUnacceptedOpenId() {
            LogStarIdAccepted(false);
        }

        //star_identification_accepted
        //* star_id
        //* category
        //* earned_point
        private void LogStarIdAccepted(bool scoredPoint) {
            var rgs = Find.State<RefGuideState>();

            m_Log.BeginEvent("star_identification_accepted");
            m_Log.EventParam("star_id", m_LastKnownSubmittedStarAsset.DisplayName);
            m_Log.EventParam("category", EnumLookup.FirstClassificationType(m_LastKnownSubmittedRefClassification.Type));
            m_Log.EventParam("earned_point", scoredPoint);
            m_Log.SubmitEvent();
        }

        //star_identification_rejected
        //* star_id
        //* category
        //* classification : str | List[element ID]
        //* correct_classification
        private void LogStarIdRejected() {
            var rgs = Find.State<RefGuideState>();
            StringBuilder submittedClassificationStrBuilder = new StringBuilder();
            StringBuilder correctClassificationStrBuilder = new StringBuilder();

            var refClassification = Find.NamedAsset<ReferenceClassification>(m_LastKnownReviewSubmissionClassification.Classification);

            // get string of submitted classification
            if (!m_LastKnownReviewSubmissionClassification.Classification.IsEmpty) {
                submittedClassificationStrBuilder.Append(refClassification.Label);
            } else if (m_LastKnownReviewSubmissionClassification.Materials != 0) {
                m_JsonBuilder.Clear();
                submittedClassificationStrBuilder.Append(SpectrographUtility.Append(m_LastKnownReviewSubmissionClassification.Materials, m_JsonBuilder).End().ToString());
                m_JsonBuilder.Clear();
            }

            // get string of correct classification
            if (!m_LastKnownReviewSubmissionClassification.Classification.IsEmpty) {
                if (refClassification != null) {
                    for (int i = 0; i < m_LastKnownSubmittedStarAsset.ClassIds.Length; i++) {
                        if ((Find.NamedAsset<ReferenceClassification>(m_LastKnownSubmittedStarAsset.ClassIds[i]).Type
                            & refClassification.Type) != 0) {
                            // found what the classification should have been
                            correctClassificationStrBuilder.Append(Find.NamedAsset<ReferenceClassification>(m_LastKnownSubmittedStarAsset.ClassIds[i]).Label);
                            break;
                        }
                    }
                }
            }
            else if (m_LastKnownSubmittedStarAsset.Spectrograph != 0) {
                m_JsonBuilder.Clear();
                correctClassificationStrBuilder.Append(SpectrographUtility.Append(m_LastKnownSubmittedStarAsset.Spectrograph, m_JsonBuilder).End().ToString());
                m_JsonBuilder.Clear();
            }

            m_Log.BeginEvent("star_identification_rejected");
            m_Log.EventParam("star_id", m_LastKnownSubmittedStarAsset.DisplayName);
            m_Log.EventParam("category", EnumLookup.FirstClassificationType(rgs.SelectedRefClassification.Type));
            m_Log.EventParam("classification", submittedClassificationStrBuilder.ToString());
            m_Log.EventParam("correct_classification", correctClassificationStrBuilder.ToString());
            m_Log.SubmitEvent();
        }

        //points_needed_displayed
        //* points_needed
        //* points_earned
        private void LogPointsNeededDisplayed (int ptsEarned) {
            DayConfigAsset config = DayConfigUtil.GetConfigForState();

            m_Log.BeginEvent("points_needed_displayed");
            m_Log.EventParam("points_needed", config.NumNeutrinoPoints);
            m_Log.EventParam("points_earned", ptsEarned);
            m_Log.SubmitEvent();

            UpdatePointsNeeded(config.NumNeutrinoPoints);
            UpdatePointsEarned(ptsEarned);
        }

        //start_radio_adjust
        //* radio_frequency
        private void LogStartRadioAdjust() {
            RadioRig rig = Find.State<RadioRig>();

            m_Log.BeginEvent("start_radio_adjust");
            m_Log.EventParam("radio_frequency", rig.LastKnownFrequency);
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
        private void LogRadioSecretFound(string msgId) {
            RadioRig rig = Find.State<RadioRig>();

            m_Log.BeginEvent("radio_secret_found");
            m_Log.EventParam("message_id", msgId);
            m_Log.EventParam("radio_frequency", rig.LastKnownFrequency);
            m_Log.SubmitEvent();
        }

        //telescope_stencil_displayed
        //* constellation_id
        //* constellation : List[star_id]
        //* connected_stars : List[Pair[star_id]]
        private void LogTelescopeStencilDisplayed() {
            PuzzleState puzzleState = Find.State<PuzzleState>();

            m_WorkingStrList.Items.Clear();
            m_WorkingStrList.FieldId = "star_id";
            for (int i = 0; i < puzzleState.ActivePuzzle.ConstellationStars.Length; i++) {
                m_WorkingStrList.Items.Add(Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.ConstellationStars[i]).DisplayName);
            }

            m_WorkingStarEdgeList.Edges.Clear();
            for (int i = 0; i < puzzleState.ActivePuzzle.Edges.Length; i++) {
                m_WorkingStarEdgeList.Edges.Add(
                    new Tuple<string, string>(
                        Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.Edges[i].Object1).DisplayName,
                        Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.Edges[i].Object2).DisplayName
                        )
                    );
            }

            m_JsonBuilder.Clear();
            m_Log.BeginEvent("telescope_stencil_displayed");
            m_Log.EventParam("constellation_id", EnumLookup.ConstellationType[(int)puzzleState.ActivePuzzle.Constellation]);
            m_Log.EventParamJson("constellation", m_WorkingStrList.AppendItems(m_JsonBuilder).End());
            m_JsonBuilder.Clear();
            m_Log.EventParamJson("connected_stars", m_WorkingStarEdgeList.AppendEdges(m_JsonBuilder).End());
            m_JsonBuilder.Clear();
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
        private void LogLogicPuzzleStart() {
            PuzzleState puzzleState = Find.State<PuzzleState>();
            if (puzzleState.ActivePuzzle == null) { return; }

            m_WorkingStrList.Items.Clear();
            m_WorkingStrList.FieldId = "clue";
            for (int i = 0; i < puzzleState.ActivePuzzle.ClueText.Length; i++) {
                m_WorkingStrList.Items.Add(puzzleState.ActivePuzzle.ClueText[i]);
            }

            m_WorkingPuzzleContentsData.Contents.Clear();
            for (int r = 0; r < puzzleState.ActivePuzzle.Rows.Length; r++) {
                var rowData = new PuzzleRowProvidedLogData();
                if ((puzzleState.ActivePuzzle.Rows[r].ProvidedProperties & DataTypeMask.Name) != 0) {
                    CelestialAsset asset = Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.Rows[r].Object);
                    rowData.Name = asset.DisplayName;
                }
                if ((puzzleState.ActivePuzzle.Rows[r].ProvidedProperties & DataTypeMask.Coordinates) != 0) {
                    CelestialAsset asset = Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.Rows[r].Object);
                    asset.Coords.Declination.Sanitize();
                    asset.Coords.RightAscension.Sanitize();
                    m_WorkingStringBuilder.Clear();
                    asset.Coords.RightAscension.ToString(m_WorkingStringBuilder);
                    m_WorkingStringBuilder.Append(",\n");
                    asset.Coords.Declination.ToString(m_WorkingStringBuilder);
                    rowData.Coords = m_WorkingStringBuilder.ToString();
                    m_WorkingStringBuilder.Clear();
                }

                m_WorkingPuzzleContentsData.Contents.Add(rowData);
            }

            m_JsonBuilder.Clear();
            m_Log.BeginEvent("logic_puzzle_start");
            m_Log.EventParamJson("puzzle_info", m_WorkingStrList.AppendItems(m_JsonBuilder).End());
            m_JsonBuilder.Clear();

            m_WorkingStrList.Items.Clear();
            m_WorkingStrList.FieldId = "property";
            EnumLookup.GatherDataTypes(ref m_WorkingStrList.Items, puzzleState.ActivePuzzle.RequiredProperties);

            m_Log.EventParam("puzzle_id", puzzleState.ActivePuzzle.DisplayName);
            m_JsonBuilder.Clear();
            m_Log.EventParamJson("puzzle_contents", m_WorkingPuzzleContentsData.AppendContents(m_JsonBuilder).End());
            m_JsonBuilder.Clear();
            m_Log.EventParam("puzzle_properties", m_WorkingStrList.AppendItemsNoField(m_JsonBuilder).End());
            m_JsonBuilder.Clear();
            m_Log.SubmitEvent();

            UpdateCurrentPuzzle(puzzleState.ActivePuzzle);
        }

        //logic_puzzle_complete
        //* puzzle_id
        private void LogLogicPuzzleComplete() {
            PuzzleState puzzleState = Find.State<PuzzleState>();
            if (puzzleState.ActivePuzzle == null) { return; }

            m_Log.BeginEvent("logic_puzzle_complete");
            m_Log.EventParam("puzzle_id", puzzleState.ActivePuzzle.DisplayName);
            m_Log.SubmitEvent();

            UpdateCurrentPuzzle(null);
        }

        //click_tool_load
        //* tool_name
        //* star_id // currently selected star
        //* value
        private void LogClickToolLoad(PacketTransferData data) {
            m_Log.BeginEvent("click_tool_load");
            m_Log.EventParam("tool_name", EnumLookup.InstrumentType[(int)data.ToolId]);
            m_Log.EventParam("star_id", m_LastKnownMonitorSelectedStar.Name);
            m_Log.EventParam("value", data.ValueStr);
            m_Log.SubmitEvent();
        }

        //select_puzzle_cell
        //* tool_name
        //* star_id // star in cell slot
        //* value : Optional[float | str]
        private void LogSelectPuzzleCell(PacketTransferData data) {
            m_Log.BeginEvent("select_puzzle_cell");
            m_Log.EventParam("tool_name", EnumLookup.InstrumentType[(int)data.ToolId]);
            m_Log.EventParam("star_id", data.StarId);
            m_Log.EventParam("value", data.ValueStr);
            m_Log.SubmitEvent();
        }

        //transfer_value_to_cell
        //* tool_name
        //* star_id
        //* value
        //* source_star
        private void LogTransferValueToCell(PacketTransferData data) {
            m_Log.BeginEvent("transfer_value_to_cell");
            m_Log.EventParam("tool_name", EnumLookup.InstrumentType[(int)data.ToolId]);
            m_Log.EventParam("star_id", data.StarId);
            m_Log.EventParam("value", data.ValueStr);
            m_Log.EventParam("source_star", m_LastKnownMonitorSelectedStar.Name);
            m_Log.SubmitEvent();
        }

        //click_submit_puzzle
        //* puzzle_id
        //* puzzle_contents
        private void LogClickSubmitPuzzle() {
            PuzzleState puzzleState = Find.State<PuzzleState>();
            if (puzzleState.ActivePuzzle == null) { return; }

            m_WorkingPuzzleContentsData.Contents.Clear();
            for (int r = 0; r < puzzleState.ActivePuzzle.Rows.Length; r++) {
                var rowData = new PuzzleRowProvidedLogData();
                if ((puzzleState.ActivePuzzle.Rows[r].ProvidedProperties & DataTypeMask.Name) != 0) {
                    CelestialAsset asset = Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.Rows[r].Object);
                    rowData.Name = asset.DisplayName;
                }
                if ((puzzleState.ActivePuzzle.Rows[r].ProvidedProperties & DataTypeMask.Coordinates) != 0) {
                    CelestialAsset asset = Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.Rows[r].Object);
                    asset.Coords.Declination.Sanitize();
                    asset.Coords.RightAscension.Sanitize();
                    m_WorkingStringBuilder.Clear();
                    asset.Coords.RightAscension.ToString(m_WorkingStringBuilder);
                    m_WorkingStringBuilder.Append(",\n");
                    asset.Coords.Declination.ToString(m_WorkingStringBuilder);
                    rowData.Coords = m_WorkingStringBuilder.ToString();
                    m_WorkingStringBuilder.Clear();
                }

                m_WorkingPuzzleContentsData.Contents.Add(rowData);
            }

            m_Log.BeginEvent("click_submit_puzzle");
            m_Log.EventParam("puzzle_id", puzzleState.ActivePuzzle.DisplayName);
            m_JsonBuilder.Clear();
            m_Log.EventParamJson("puzzle_contents", m_WorkingPuzzleContentsData.AppendContents(m_JsonBuilder).End());
            m_JsonBuilder.Clear();
            m_Log.SubmitEvent();
        }

        //logic_puzzle_accepted
        //* puzzle_id
        private void LogLogicPuzzleAccepted() {
            PuzzleState puzzleState = Find.State<PuzzleState>();

            m_Log.BeginEvent("logic_puzzle_accepted");
            m_Log.EventParam("puzzle_id", puzzleState.ActivePuzzle.DisplayName);
            m_Log.SubmitEvent();
        }

        //logic_puzzle_rejected
        //* puzzle_id
        //* incorrect_stars : List[star_id]
        //* new_puzzle_contents
        private void LogLogicPuzzleRejected(List<string> incorrectRows) {
            PuzzleState puzzleState = Find.State<PuzzleState>();

            if (puzzleState.ActivePuzzle == null) { return; }

            // wrong stars
            m_WorkingStrList.Items.Clear();
            m_WorkingStrList.FieldId = "star_id";

            for (int r = 0; r < incorrectRows.Count; r++) {
                m_WorkingStrList.Items.Add(incorrectRows[r]);
            }

            // new contents
            m_WorkingPuzzleContentsData.Contents.Clear();
            for (int r = 0; r < puzzleState.ActivePuzzle.Rows.Length; r++) {
                var rowData = new PuzzleRowProvidedLogData();
                if ((puzzleState.ActivePuzzle.Rows[r].ProvidedProperties & DataTypeMask.Name) != 0) {
                    CelestialAsset asset = Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.Rows[r].Object);
                    rowData.Name = asset.DisplayName;
                }
                if ((puzzleState.ActivePuzzle.Rows[r].ProvidedProperties & DataTypeMask.Coordinates) != 0) {
                    CelestialAsset asset = Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.Rows[r].Object);
                    asset.Coords.Declination.Sanitize();
                    asset.Coords.RightAscension.Sanitize();
                    m_WorkingStringBuilder.Clear();
                    asset.Coords.RightAscension.ToString(m_WorkingStringBuilder);
                    m_WorkingStringBuilder.Append(",\n");
                    asset.Coords.Declination.ToString(m_WorkingStringBuilder);
                    rowData.Coords = m_WorkingStringBuilder.ToString();
                    m_WorkingStringBuilder.Clear();
                }

                m_WorkingPuzzleContentsData.Contents.Add(rowData);
            }

            m_Log.BeginEvent("logic_puzzle_rejected");
            m_Log.EventParam("puzzle_id", puzzleState.ActivePuzzle.DisplayName);
            m_JsonBuilder.Clear();
            m_Log.EventParam("incorrect_stars", m_WorkingStrList.AppendItems(m_JsonBuilder).End());
            m_JsonBuilder.Clear();
            m_Log.EventParamJson("new_puzzle_contents", m_WorkingPuzzleContentsData.AppendContents(m_JsonBuilder).End());
            m_JsonBuilder.Clear();
            m_Log.SubmitEvent();
        }

        //new_document_received
        //* document_id
        //* text_content
        private void LogNewDocReceived() {
            m_Log.BeginEvent("new_document_received");
            m_Log.EventParam("document_id", m_LastKnownDocTitle);
            // m_Log.EventParam("text_content", doc.Contents);
            m_Log.SubmitEvent();
        }

        //click_document_flip
        //* document_id
        //* to_side : FRONT | BACK
        //* text_content
        private void LogClickDocumentFlip(bool toFront) {
            m_Log.BeginEvent("click_document_flip");
            m_Log.EventParam("document_id", m_LastKnownDocTitle);
            m_Log.EventParam("to_side", toFront ? "FRONT" : "BACK");
            // m_Log.EventParam("text_content", doc.Contents);
            m_Log.SubmitEvent();
        }

        //click_dismiss_document
        private void LogClickDismissDocument() {
            m_Log.BeginEvent("click_dismiss_document");
            // m_Log.EventParam("document_id", docId); // would this be useful?
            m_Log.SubmitEvent();
        }


        //click_view_document
        //* document_id
        //* text_content
        private void LogClickViewDocument() {
            m_Log.BeginEvent("new_document_received");
            m_Log.EventParam("document_id", m_LastKnownDocTitle);
            // m_Log.EventParam("text_content", doc.Contents);
            m_Log.SubmitEvent();
        }


        //postit_recieved
        //* postit_id
        //* text_content
        private void LogPostitReceived() {
            m_Log.BeginEvent("postit_received");
            m_Log.EventParam("postit_id", m_LastKnownDocTitle);
            // m_Log.EventParam("text_content", doc.Contents);
            m_Log.SubmitEvent();
        }

        //click_dismiss_postit
        //* postit_id
        private void LogClickDismissPostit() {
            m_Log.BeginEvent("click_dismiss_postit");
            m_Log.EventParam("postit_id", m_LastKnownDocTitle);
            m_Log.SubmitEvent();
        }

        //grab_post_it
        //* postit_id
        private void LogGrabPostit() {
            m_Log.BeginEvent("grab_post_it");
            m_Log.EventParam("postit_id", m_LastKnownMovedDoc);
            m_Log.SubmitEvent();
        }

        //place_post_it
        //* postit_id
        //* target_id : DocumentID | BOARD
        //* correct_target : DocumentID
        private void LogPlacePostit(StringPair pair) {
            m_LastKnownPostitTargetPair = pair;
            if (m_LastKnownPostitTargetPair.SecondStr == default) {
                m_LastKnownPostitTargetPair.SecondStr = "BOARD";
            }

            m_Log.BeginEvent("place_post_it");
            m_Log.EventParam("postit_id", m_LastKnownMovedDoc);
            m_Log.EventParam("target_id", m_LastKnownPostitTargetPair.FirstStr);
            m_Log.EventParam("correct_target", m_LastKnownPostitTargetPair.SecondStr);
            m_Log.SubmitEvent();
        }

        //post_it_match_accepted
        //* postit_id
        private void LogPostitMatchAccepted() {
            m_Log.BeginEvent("post_it_match_accepted");
            m_Log.EventParam("postit_id", m_LastKnownMovedDoc);
            m_Log.SubmitEvent();
        }

        //post_it_match_rejected
        //* postit_id
        //* target_id : DocumentID | BOARD
        //* correct_target : DocumentID
        private void LogPostitMatchRejected() {
            m_Log.BeginEvent("post_it_match_rejected");
            m_Log.EventParam("postit_id", m_LastKnownMovedDoc);
            m_Log.EventParam("target_id", m_LastKnownPostitTargetPair.FirstStr);
            m_Log.EventParam("correct_target", m_LastKnownPostitTargetPair.SecondStr);
            m_Log.SubmitEvent();
        }

        //toggle_magnitude_mode:
        //* new_mode : ABSOLUTE | RELATIVE
        private void LogToggleMagMode() {
            HistoricalDataState hds = Find.State<HistoricalDataState>();

            m_Log.BeginEvent("toggle_magnitude_mode");
            m_Log.EventParam("new_mode", hds.SendingAbsMag ? "ABSOLUTE" : "RELATIVE");
            m_Log.SubmitEvent();

            UpdateMagnitudeMode(hds.SendingAbsMag);
        }

        //switch_player_view:
        //* view_id
        private void LogSwitchPlayerView() {
            if (m_LastKnownViewNodeName == default) { return; }

            m_Log.BeginEvent("switch_player_view");
            m_Log.EventParam("view_id", m_LastKnownViewNodeName);
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

    public enum InstrumentTypeMask {
        /// Be sure to update EnumLookup if this is updated!
        PHOTOMETER,
        COLOR_METER,
        TEMPERATURE_METER,
        SPECTROMETER,
        PARALLAX,
        DECODER,
        COORDINATES,
        NONE,
    }

    public enum MagnitudeMode {
        /// Be sure to update EnumLookup if this is updated!
        ABSOLUTE, RELATIVE
    }

    #endregion // Data Enums

    #region Enum Lookup
    public static class EnumLookup {
        public static readonly string[] WavelengthType = new string[] {
            "VISIBLE", "BLUE", "INFRARED"
        };
        public static readonly string[] InstrumentType = new string[] {
            "PHOTOMETER", "COLOR_METER", "TEMPERATURE_METER", "SPECTROMETER", "PARALLAX", "DECODER", "COORDINATES", "NONE"
        };
        public static readonly string[] MagnitudeMode = new string[] {
            "ABSOLUTE", "RELATIVE"
        };
        public static readonly string[] ClassificationType = new string[] {
            "BRIGHTNESS", "SPECTRAL_TYPE", "ELEMENTS", "HISTORICAL", "SPECTRAL_TYPE_DWARF", "LUMINOSITY"
        };
        public static readonly string[] DataType = new string[] {
            "NAME", "COORDINATES", "COLOR", "APPARENT_MAGNITUDE", "ABSOLUTE_MAGNITUDE", "MATERIAL_SPECTRUM",
            "TEMPERATURE", "DISTANCE", "HISTORICAL_COORDINATES", "HISTORICAL_APPARENT_MAGNITUDE",
            "BLUE_MAGNITUDE", "INFRARED_MAGNITUDE", "HISTORICAL_COLOR", "COLOR_INDEX"
        };
        public static readonly string[] ConstellationType = new string[] {
            "URSA_MAJOR", "ANDROMEDA", "DRACO", "ERIDANUS", "HERCULES", "HYDRA", "LEO", "ORION", "PERSEUS", "TAURUS"
        };
        public static string FirstClassificationType(ClassificationTypeMask type) {
            if ((type & ClassificationTypeMask.Photometer) != 0) {
                return ClassificationType[0];
            } else if ((type & ClassificationTypeMask.ColorMeter) != 0) {
                return ClassificationType[1];
            } else if ((type & ClassificationTypeMask.Spectrometer) != 0) {
                return ClassificationType[2];
            } else if ((type & ClassificationTypeMask.Historical) != 0) {
                return ClassificationType[3];
            } else if ((type & ClassificationTypeMask.Infrared) != 0) {
                return ClassificationType[4];
            } else if ((type & ClassificationTypeMask.Luminosity) != 0) {
                return ClassificationType[5];
            }

            /* buggy
            foreach (var bit in Bits.Enumerate(type)) {
                return ClassificationType[(int)bit];
            }
            */
            return "NONE";
        }

        public static string FirstWavelengthType(CelestialObjectVisMask type) {
            if ((type & CelestialObjectVisMask.Visible) != 0) {
                return WavelengthType[0];
            } else if ((type & CelestialObjectVisMask.Blue) != 0) {
                return WavelengthType[1];
            } else if ((type & CelestialObjectVisMask.Infrared) != 0) {
                return WavelengthType[2];
            }
            return "NONE";
        }

        public static void GatherDataTypes(ref List<string> workList, DataTypeMask type) {
            if (workList == null) {
                workList = new List<string>();
            }

            if ((type & DataTypeMask.Name) != 0) {
                workList.Add(DataType[0]);
            }
            if ((type & DataTypeMask.Coordinates) != 0) {
                workList.Add(DataType[1]);
            }
            if ((type & DataTypeMask.Color) != 0) {
                workList.Add(DataType[2]);
            }
            if ((type & DataTypeMask.ApparentMagnitude) != 0) {
                workList.Add(DataType[3]);
            }
            if ((type & DataTypeMask.AbsoluteMagnitude) != 0) {
                workList.Add(DataType[4]);
            }
            if ((type & DataTypeMask.MaterialSpectrum) != 0) {
                workList.Add(DataType[5]);
            }
            if ((type & DataTypeMask.Temperature) != 0) {
                workList.Add(DataType[6]);
            }
            if ((type & DataTypeMask.Distance) != 0) {
                workList.Add(DataType[7]);
            }
            if ((type & DataTypeMask.Historical_Coordinates) != 0) {
                workList.Add(DataType[8]);
            }
            if ((type & DataTypeMask.Historical_ApparentMagnitude) != 0) {
                workList.Add(DataType[9]);
            }
            if ((type & DataTypeMask.BlueMagnitude) != 0) {
                workList.Add(DataType[10]);
            }
            if ((type & DataTypeMask.InfraredMagnitude) != 0) {
                workList.Add(DataType[11]);
            }
            if ((type & DataTypeMask.Historical_Color) != 0) {
                workList.Add(DataType[12]);
            }
            if ((type & DataTypeMask.ColorIndex) != 0) {
                workList.Add(DataType[13]);
            }
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

        public bool IsValid;

        public readonly JsonBuilder Append(JsonBuilder json)
        {
            if (!IsValid) {
                return json;
            }

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
        public List<string> Stars;

        public readonly JsonBuilder AppendStars(JsonBuilder json)
        {
            if (Stars != null) {
                foreach (var star in Stars) {
                    json.Field("star_id", star);
                }
            }

            return json;
        }
    }

    [Serializable]
    public struct StringList {
        public List<string> Items;
        public string FieldId;

        public readonly JsonBuilder AppendItems(JsonBuilder json) {
            if (Items != null) {
                foreach (var item in Items) {
                    json.Field(FieldId, item);
                }
            }

            return json;
        }

        public readonly JsonBuilder AppendItemsNoField(JsonBuilder json) {
            if (Items != null) {
                foreach (var item in Items) {
                    json.Item(item);
                }
            }

            return json;
        }
    }

    [Serializable]
    public struct StarEdgeList {
        public List<Tuple<string, string>> Edges;

        public readonly JsonBuilder AppendEdges(JsonBuilder json) {
            if (Edges != null) {
                foreach (var edge in Edges) {
                    json.BeginObject("pair");
                    json.Field("edgeA", edge.Item1);
                    json.Field("edgeB", edge.Item2);
                    json.EndObject();
                }
            }

            return json;
        }
    }

    [Serializable]
    public struct PuzzleRowProvidedLogData {
        public string Name;
        public string Coords;

        public readonly JsonBuilder Append(JsonBuilder json) {
            json.Field("name", Name);
            json.Field("coords", Coords);

            return json;
        }
    }

    [Serializable]
    public struct PuzzleContentsLogData {
        public List<PuzzleRowProvidedLogData> Contents;

        public readonly JsonBuilder AppendContents(JsonBuilder json) {
            if (Contents != null) {
                foreach (var row in Contents) {
                    json.BeginObject("row");
                    row.Append(json);
                    json.EndObject();
                }
            }

            return json;
        }
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
        public string ValueStr;
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

    [Serializable]
    public struct ClassificationLogData {
        public ClassificationTypeMask Type;
        public string Label;
    }

    [Serializable]
    public struct StringPair {
        public string FirstStr;
        public string SecondStr;
    }

    #endregion //Data Structs

}