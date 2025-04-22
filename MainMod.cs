using MelonLoader;
using S1API;

[assembly: MelonInfo(typeof(ManorMod.ExampleMod), ManorMod.BuildInfo.Name, ManorMod.BuildInfo.Version, ManorMod.BuildInfo.Author, ManorMod.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ManorMod
{
    public static class BuildInfo
    {
        public const string Name = "ManorMod";
        public const string Description = "";
        public const string Author = "ChloeNow";
        public const string Company = null;
        public const string Version = "0.9.0";
        public const string DownloadLink = null;
    }

    public class ExampleMod : MelonMod
    {

    }
}
