namespace UmaAsset.Core.Models;

[Flags]
public enum AssetBundleGroup : int
{
    Default = 0x00,
    DeleteOnLogin = 0x01,
    DownloadLogin = 0x02,
    Tutorial = 0x04,
    HomeLogin = 0x08,
    RequiredTutorialStart = 0x10,
    DelayRelease = 0x20,
    RealFanfare = 0x40,
    WithRealFanfare = 0x80,
    DataManagementTarget = 0x100,
    DeleteOnLoginWithConditionStory = 0x10000,
    DeleteOnLoginWithConditionEvent = 0x20000,
    DeleteOnLoginWithConditionLive = 0x40000,
    DeleteOnLoginWithConditionRace = 0x80000,
    DeleteOnLoginWithConditionOthers = 0x100000,
}
