using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChromExplorer.Assets
{
    internal enum ResourceType
    {
        None,
        Xaml,
        Font,
        Image
    }

    [MarkupExtensionReturnType(typeof(object))]
    public class AssetExtension : MarkupExtension
    {
        private static Regex ResourceRegex = new Regex(
            @"
				(?<Image>(\.png|\.jpg|\.bmp)$)
				| (?<Font>\#.+$)
				| (?<Xaml>\.xaml$)
			",
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace |
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        [ConstructorArgument("path")]
        public string Path { get; set; }

        public AssetExtension(string path)
        {
            Path = path;
        }

        public static Uri GetResourceUri(string path)
        {
            string asmName = Assembly.GetExecutingAssembly().GetName().Name;
            string uriString = string.Format("pack://application:,,,/{0};component/{1}", asmName, path);
            Uri uri = new Uri(uriString);

            return uri;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            IProvideValueTarget ipvt = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            if (ipvt == null) return null;

            ResourceType type = ParseResourceType(Path);
            switch (type)
            {
                case ResourceType.Image:
                    BitmapImage image = new BitmapImage(GetResourceUri(Path));
                    return image;

                case ResourceType.Font:
                    FontFamily family = new FontFamily(GetResourceUri(""), Path);
                    return family;

                case ResourceType.Xaml:
                    return GetResourceUri(Path);
            }

            return null;
        }

        private ResourceType ParseResourceType(string path)
        {
            ResourceType type = ResourceType.None;

            Match match = ResourceRegex.Match(path);
            if (match.Groups["Image"].Success) type = ResourceType.Image;
            else if (match.Groups["Font"].Success) type = ResourceType.Font;
            else if (match.Groups["Xaml"].Success) type = ResourceType.Xaml;

            return type;
        }
    }

}
