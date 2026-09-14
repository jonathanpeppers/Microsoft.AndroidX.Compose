using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

/// <summary>Host-side checks for pinned, unmodified font assets and their declared matching metadata.</summary>
public class ResourceFontAssetTests
{
    [Theory]
    [InlineData("karla_regular", 400, "b2754c00295b6eb895d8419cb3df993d74a0ed97e143ee98fcd83fdca94f932c")]
    [InlineData("karla_bold", 700, "7a61886119056f23bfb3ec3efa1d4678769e3794e205e60ca34136cc0f9008e7")]
    [InlineData("montserrat_light", 300, "e0feb97ab7fdca79ccdfcc7df7b629f86705e33b7687b7463b388b003ffef865")]
    [InlineData("montserrat_regular", 400, "077cdab15161232a9ba7124d2ddd7a9425145750788e9a966c156cc66274f525")]
    [InlineData("montserrat_medium", 500, "421f26b23e2be6b98373d32acd3cb2897b154d4bf0a77d26534ce476e4cbed53")]
    [InlineData("montserrat_semibold", 600, "f227901ef48ac4d1fe4cc6ed0dbce99e6b38969babe5e05da2dfb33521b02944")]
    public void JetchatAssetsMatchPinnedRevisionAndDeclaredWeight(string name, int weight, string sha256)
    {
        var app = Path.Combine(Root(), "samples", "Jetchat");
        Check(app, name, weight, false, sha256);
        var gallery = Path.Combine(Root(), "src", "Microsoft.AndroidX.Compose.Gallery");
        if (File.Exists(Path.Combine(gallery, "Resources", "font", name + ".ttf")))
            Check(gallery, name, weight, false, sha256);
    }

    [Fact]
    public void GalleryItalicIsGenuineItalicAndNotAPinnedJetchatAsset()
    {
        var app = Path.Combine(Root(), "src", "Microsoft.AndroidX.Compose.Gallery");
        Check(app, "karla_italic", 400, true,
            "98c02551288197a43d9c7e62c105f4b6f283a57f0f06d623313ea32d5c701977");
        Assert.Contains("Copyright 2019 The Karla Project Authors",
            File.ReadAllText(Path.Combine(app, "Assets", "KARLA_ITALIC_LICENSE.txt")));
        Assert.False(File.Exists(Path.Combine(Root(), "samples", "Jetchat", "Resources", "font", "karla_italic.ttf")));
    }

    static void Check(string app, string name, int weight, bool italic, string sha256)
    {
        var bytes = File.ReadAllBytes(Path.Combine(app, "Resources", "font", name + ".ttf"));
        Assert.Equal(sha256, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(4));
        int os2 = -1;
        for (int i = 0; i < tableCount; i++)
        {
            int offset = 12 + 16 * i;
            if (Encoding.ASCII.GetString(bytes, offset, 4) == "OS/2")
                os2 = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset + 8)));
        }
        Assert.True(os2 >= 0, "Font has no OS/2 metadata.");
        Assert.Equal(weight, BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(os2 + 4)));
        Assert.Equal(italic, (BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(os2 + 62)) & 1) != 0);
        Assert.Contains("SIL OPEN FONT LICENSE", File.ReadAllText(Path.Combine(app, "Assets", "FONT_LICENSE.txt")));
        Assert.Contains(sha256, File.ReadAllText(Path.Combine(app, "Assets", "FONT_SOURCES.txt")));
    }

    static string Root([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)
            ?? throw new InvalidOperationException("Font asset test source directory unavailable."), "..", ".."));
}
