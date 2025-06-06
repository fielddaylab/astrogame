namespace FieldDay.Localization {
    /// <summary>
    /// Interface for a localized component.
    /// </summary>
    public interface ILocalizedComponent {
        void OnLocalizationUpdated(LanguageId language);
    }
}