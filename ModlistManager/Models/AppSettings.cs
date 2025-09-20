namespace ETS2ATS.ModlistManager.Models
{
    public class AppSettings
    {
        public string Language { get; set; } = "de";         // "de", "en"
        public string Theme { get; set; } = "Light";          // "Light" | "Dark"
        public string PreferredGame { get; set; } = "ETS2";   // "ETS2" | "ATS"

        public string? Ets2ProfilesPath { get; set; }         // optional
        public string? AtsProfilesPath  { get; set; }         // optional
        // Benutzerdefinierte Modlisten-Pfade (optional je Spiel)
        public string? Ets2ModlistsPath { get; set; }         // optional: <Ziel>/modlists für ETS2
        public string? AtsModlistsPath  { get; set; }         // optional: <Ziel>/modlists für ATS
        public string? Ets2WorkshopContentOverride { get; set; } // optional: direkte Angabe von steamapps/workshop/content/227300
        public string? AtsWorkshopContentOverride  { get; set; } // optional: direkte Angabe von steamapps/workshop/content/270880

        public bool ConfirmBeforeAdopt { get; set; } = true;  // Bestätigung vor „Modliste übernehmen“
    }
}