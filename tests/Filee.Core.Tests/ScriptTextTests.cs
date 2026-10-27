// Scripts, style sheets and INI settings are plain text (issues #388, #368, #442, #630): recognised, and learnt by
// existing text donuts without taking the user's custom extension claims.

using Filee.Core.Formats;
using Filee.Core.Presets;
using Filee.Core.Profiles;
using Filee.Core.Settings;

namespace Filee.Core.Tests;

public class ScriptTextTests
{
    [Theory]
    [InlineData("init.lua")]
    [InlineData("site.css")]
    [InlineData("Backup.ps1")]
    [InlineData("settings.ini")]
    [InlineData("DEVICE.INI")]
    public void Scripts_and_style_sheets_are_text(string name) => Assert.Equal("txt", FormatRegistry.Detect(name)?.Id);

    [Fact]
    public void Version_33_text_donuts_learn_them_without_taking_custom_claims()
    {
        var presets = BuiltInData.CreatePresets();
        var profiles = BuiltInData.CreateProfiles();
        foreach (var profile in profiles)
            profile.Extensions.RemoveAll(e => e is "lua" or "css");
        profiles.Insert(0, new ToolbarProfile { Id = "my-web", Name = "Web", Extensions = ["css"] });

        Assert.True(SettingsMigrations.ApplyToLibrary(33, presets, profiles));

        var text = profiles.Single(p => p.Id == "text").Extensions;
        Assert.Contains("lua", text);
        Assert.DoesNotContain("css", text); // the user's own donut has it
        Assert.False(SettingsMigrations.ApplyToLibrary(33, presets, profiles));
    }

    [Fact]
    public void Version_38_text_donuts_learn_powershell_scripts()
    {
        var presets = BuiltInData.CreatePresets();
        var profiles = BuiltInData.CreateProfiles();
        foreach (var profile in profiles)
            profile.Extensions.Remove("ps1");

        Assert.True(SettingsMigrations.ApplyToLibrary(38, presets, profiles));

        Assert.Contains("ps1", profiles.Single(p => p.Id == "text").Extensions);
        Assert.False(SettingsMigrations.ApplyToLibrary(39, presets, profiles));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Version_48_text_donuts_learn_ini_without_taking_custom_claims(bool customClaim)
    {
        var presets = BuiltInData.CreatePresets();
        var profiles = BuiltInData.CreateProfiles();
        var text = profiles.Single(p => p.Id == "text").Extensions;
        text.Remove("ini");
        text.Remove("log"); // An unrelated extension the user removed stays removed.
        if (customClaim)
            profiles.Insert(0, new ToolbarProfile { Id = "my-config", Name = "Config", Extensions = ["INI"] });

        Assert.Equal(!customClaim, SettingsMigrations.ApplyToLibrary(48, presets, profiles));

        Assert.Equal(!customClaim, text.Contains("ini"));
        Assert.DoesNotContain("log", text);
        if (customClaim)
            Assert.Equal(["INI"], profiles.Single(p => p.Id == "my-config").Extensions);
        Assert.False(SettingsMigrations.ApplyToLibrary(48, presets, profiles));
        text.Remove("ini");
        Assert.False(SettingsMigrations.ApplyToLibrary(49, presets, profiles));
    }
}
