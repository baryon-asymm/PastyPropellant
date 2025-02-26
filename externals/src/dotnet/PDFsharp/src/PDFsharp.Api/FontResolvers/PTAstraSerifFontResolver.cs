using System.Reflection;
using PdfSharp.Fonts;

namespace PDFsharp.Api.FontResolvers;

internal class PTAstraSerifFontResolver : IFontResolver
{
    internal static PTAstraSerifFontResolver? OurGlobalFontResolver;

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        // Ignore case of font names.
        var name = familyName.ToLower().TrimEnd('#');

        // Deal with the fonts we know.
        switch (name)
        {
            case "ptastraserif":
                if (isBold)
                {
                    if (isItalic)
                        return new FontResolverInfo("PTAstraSerif#bi");
                    return new FontResolverInfo("PTAstraSerif#b");
                }

                if (isItalic)
                    return new FontResolverInfo("PTAstraSerif#i");
                return new FontResolverInfo("PTAstraSerif#");
        }

        // We pass all other font requests to the default handler.
        // When running on a web server without sufficient permission, you can return a default font at this stage.
        return PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic);
    }

    public byte[]? GetFont(string faceName)
    {
        switch (faceName)
        {
            case "PTAstraSerif#":
                return FontHelper.PTAstraSerif;

            case "PTAstraSerif#b":
                return FontHelper.PTAstraSerifBold;

            case "PTAstraSerif#i":
                return FontHelper.PTAstraSerifItalic;

            case "PTAstraSerif#bi":
                return FontHelper.PTAstraSerifBoldItalic;
        }

        return null;
    }

    /// <summary>
    ///     Ensure the font resolver is only applied once (or an exception is thrown)
    /// </summary>
    internal static void Apply()
    {
        if (OurGlobalFontResolver == null || GlobalFontSettings.FontResolver == null)
        {
            if (OurGlobalFontResolver == null)
                OurGlobalFontResolver = new PTAstraSerifFontResolver();

            GlobalFontSettings.FontResolver = OurGlobalFontResolver;
        }
    }
}

/// <summary>
///     Helper class that reads font data from embedded resources.
/// </summary>
public static class FontHelper
{
    public static byte[] PTAstraSerif => LoadFontData("PDFsharp.Api.fonts.pt_astra_serif.pt_astra_serif.ttf");

    public static byte[] PTAstraSerifBold => LoadFontData("PDFsharp.Api.fonts.pt_astra_serif.pt_astra_serif_b.ttf");

    public static byte[] PTAstraSerifItalic => LoadFontData("PDFsharp.Api.fonts.pt_astra_serif.pt_astra_serif_i.ttf");

    public static byte[] PTAstraSerifBoldItalic =>
        LoadFontData("PDFsharp.Api.fonts.pt_astra_serif.pt_astra_serif_bi.ttf");

    /// <summary>
    ///     Returns the specified font from an embedded resource.
    /// </summary>
    private static byte[] LoadFontData(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Test code to find the names of embedded fonts
        //var ourResources = assembly.GetManifestResourceNames();

        using (var stream = assembly.GetManifestResourceStream(name))
        {
            if (stream == null)
                throw new ArgumentException("No resource with name " + name);

            var count = (int)stream.Length;
            var data = new byte[count];
            stream.Read(data, 0, count);
            return data;
        }
    }
}
