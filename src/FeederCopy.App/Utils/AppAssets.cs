using System.Reflection;

namespace FeederCopy.App.Utils
{
    public static class AppAssets
    {
        private const string Prefix = "FeederCopy.Assets.";

        private static readonly Lazy<Icon> AppIcon = new(() => new Icon(Open("app.ico")));
        private static readonly Lazy<Image> AppLogo = new(() =>
        {
            using Stream stream = Open("soreco-logo.png");
            using Image image = Image.FromStream(stream);
            return new Bitmap(image);
        });

        public static Icon Icon => AppIcon.Value;

        public static Image Logo => AppLogo.Value;

        private static Stream Open(string fileName) =>
            Assembly.GetExecutingAssembly().GetManifestResourceStream(Prefix + fileName)
            ?? throw new InvalidOperationException($"The program file is missing its built-in {fileName}. Rebuild the program.");
    }
}
