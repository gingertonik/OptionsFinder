using System.Collections.Generic;

namespace OptionsFinder.Options;

// this is a list of all of the exposed options that aren't used so the tripwire doesn't
// send warnings on every plugin load about these options. if any of these end up exposed
// by the game, remove them from here.

public static class RejectedOptionNames
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>
    {
        "ActiveInstanceGuid", "ActiveLS_H", "ActiveLS_L", "ActiveProductGuid", "Alias", "AntiAliasing",
        "BannerContentsDispType", "BannerContentsNotice", "BannerContentsOrderType", "BattleEffect", "BGEffect", "ChatType",
        "ConfigVersion", "ContentsFinderListSortType", "ContentsFinderSupplyEnable", "ContentsFinderUseLangTypeDE", "ContentsFinderUseLangTypeEN", "ContentsFinderUseLangTypeFR",
        "ContentsFinderUseLangTypeJA", "ContentsReplayEnable", "DepthOfField", "DetailDispDelayType", "DisplayObjectLimitType2", "DistortionWater",
        "DktSessionId", "DynamicAroundRangeMode", "DynamicRezoEnableCutScene", "DynamicRezoType", "FellowshipShowNewNotice", "FirstConfigBackup",
        "FootEffect", "FriendListFilterType", "FriendListSortPriority", "FriendListSortType", "GBarrelDisp", "Glare",
        "GPoseMotionFilterAction", "GposePortraitRotateType", "GPoseTargetFilterNPCLookAt", "GrassQuality", "GroupPoseEnableEyelidManulOpening", "GroupPoseEyelidOpeningL",
        "GroupPoseEyelidOpeningR", "GroupPoseEyelidTracking", "GroupPosePortraitButtonType", "GroupPosePortraitUnlockAspectLimit", "GuidVersion", "HowTo",
        "InstanceGuid", "LangSelectSub", "Language", "LastLogin0", "LastLogin1", "LegacySeal",
        "LetterListFilterType", "LetterListSortType", "LodType", "LsListSortPriority", "MapResolution", "MipDispType",
        "MsqProgress", "OcclusionCulling", "PadAvailable", "PadGuid", "PadPovInput", "PartyFinderNewArrivalDisp",
        "PhysicsType", "PhysicsTypeEnemy", "PhysicsTypeOther", "PhysicsTypeParty", "PhysicsTypeSelf", "Port",
        "ProductGuid", "PromptConfigUpdate", "PvPFrontlinesGCFree", "RadialBlur", "ReflectionType", "Region",
        "RemotePlayRearTouchpadEnable", "ScreenLeft", "ScreenTop", "ServiceIndex", "ShadowCascadeCountType", "ShadowLightValidType",
        "ShadowLOD", "ShadowSoftShadowType", "ShadowTextureSizeType", "ShadowVisibilityType", "ShadowVisibilityTypeEnemy", "ShadowVisibilityTypeOther",
        "ShadowVisibilityTypeParty", "ShadowVisibilityTypeSelf", "SoundChocobo", "SSAO", "StreamingType", "SupportButtonAutorunEnable",
        "TelepoCategoryType", "TelepoTicketGilSetting", "TelepoTicketUseType", "TextureAnisotropicQuality", "TextureFilterQuality", "TouchPadButtonExtension",
        "TouchPadButton_Left", "TouchPadButton_Right", "TranslucentQuality", "UiBaseScale", "UiSystemEnlarge", "UPnP",
        "Vignetting", "WaterWet", "WindowDispNum", "WorldId",
    };
}
