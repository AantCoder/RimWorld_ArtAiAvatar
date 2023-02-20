using UnityEngine;
using Verse;

namespace ArtAi
{
    public class ArtAiSettings : ModSettings
    {
        public static string ServerUrl = "http://localhost:8080/generate";

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ServerUrl, "serverUrl", "http://localhost:8080/generate");
            base.ExposeData();
        }
    }

    public class ArtAiMod : Mod
    {
        public ArtAiMod(ModContentPack content) : base(content)
        {
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Label("Generation server url");
            ArtAiSettings.ServerUrl = listingStandard.TextEntry(ArtAiSettings.ServerUrl);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "ArtAi";
        }
    }
}