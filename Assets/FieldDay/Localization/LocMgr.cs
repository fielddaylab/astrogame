namespace FieldDay.Localization {
    public sealed class LocMgr {
        #region State

        

        #endregion // State

        #region Events

        internal void Initialize(LanguageId defaultLanguage) {
            Loc.ConfigureDefaultLanguage(defaultLanguage);
        }

        internal void Shutdown() {

        }

        #endregion // Events
    }
}