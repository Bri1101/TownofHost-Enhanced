using AmongUs.GameOptions;
using TOHE.Roles.AddOns;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Scammer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Scammer;
    private const int Id = 31800;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Madmate;
    //==================================================================\\
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    private static OptionItem OptionAffectNeutral;
    private static OptionItem OptionAffectCoven;
    private static OptionItem OptionCanSellHelpfulAddonToImpostor;
    private static OptionItem OptionSellOnlyEnabledAddons;

    private static List<CustomRoles> addons = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Scammer, 1, zeroOne: false);
        OptionAffectNeutral = BooleanOptionItem.Create(Id + 2, "ScammerCanAffectNeutral", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Scammer]);
        OptionAffectCoven = BooleanOptionItem.Create(Id + 3, "ScammerCanAffectCoven", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Scammer]);
        OptionCanSellHelpfulAddonToImpostor = BooleanOptionItem.Create(Id + 4, "ScammerCanSellHelpfulAddonToImpostor", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Scammer]);
        OptionSellOnlyEnabledAddons = BooleanOptionItem.Create(Id + 5, "MerchantSellOnlyEnabledAddons", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Scammer]);
        OverrideTasksData.Create(Id + 6, TabGroup.ImpostorRoles, CustomRoles.Scammer);
    }
    public override void Init()
    {
        addons.Clear();

        addons.AddRange(GroupedAddons[AddonTypes.Helpful]);
        addons.AddRange(GroupedAddons[AddonTypes.Harmful]);
        if (OptionSellOnlyEnabledAddons.GetBool())
        {
            addons = addons.Where(role => role.GetMode() != 0).ToList();
        }
    }
    public override void Add(byte playerId)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(OthersAfterPlayerDeathTask);
        }
    }
    public override void Remove(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Add(OthersAfterPlayerDeathTask);
    }

    public override bool CanUseKillButton(PlayerControl pc) => false;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive()) return true;

        if (addons.Count == 0)
        {
            player.Notify(ColorString(GetRoleColor(CustomRoles.Scammer), GetString("MerchantAddonSellFail")));
            Logger.Info("No addons to sell.", "Scammer");
            return true;
        }

        var rd = IRandom.Instance;
        CustomRoles addon = addons.RandomElement();
        var helpful = GroupedAddons[AddonTypes.Helpful].Where(x => addons.Contains(x)).ToList();
        var harmful = GroupedAddons[AddonTypes.Harmful].Where(x => addons.Contains(x)).ToList();

        List<PlayerControl> AllAlivePlayer =
            Main.AllAlivePlayerControls.Where(x =>
                x.PlayerId != player.PlayerId
                && (!x.Is(CustomRoles.Stubborn))
                && !addon.IsConverted()
                && CustomRolesHelper.CheckAddonConfilct(addon, x, checkLimitAddons: false)
                && (!Cleanser.CantGetAddon() || (Cleanser.CantGetAddon() && !x.Is(CustomRoles.Cleansed)))
            ).ToList();

        if (AllAlivePlayer.Any())
        {
            PlayerControl target = AllAlivePlayer.RandomElement();

            if (target.GetCustomRole().IsCrewmate()
                || (target.GetCustomRole().IsNeutralTeamV3() && OptionAffectNeutral.GetBool())
                || (target.GetCustomRole().IsCoven() && OptionAffectCoven.GetBool()))
            {
                addon = harmful.RandomElement();
            }
            else if (target.GetCustomRole().IsImpostorTeamV3() && OptionCanSellHelpfulAddonToImpostor.GetBool())
            {
                addon = helpful.RandomElement();
            }

            target.RpcSetCustomRole(addon);
            target.Notify(ColorString(GetRoleColor(CustomRoles.Scammer), GetString("ScammerAddonSell")));
            player.Notify(ColorString(GetRoleColor(CustomRoles.Scammer), GetString("MerchantAddonDelivered")));

            target.AddInSwitchAddons(target, addon);
        }
        else
        {
            player.Notify(ColorString(GetRoleColor(CustomRoles.Scammer), GetString("MerchantAddonSellFail")));
            Logger.Info("All Alive Player Count = 0", "Scammer");
            return true;
        }

        return true;
    }
    private void OthersAfterPlayerDeathTask(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (_Player == null || !_Player.IsAlive() || Main.AliveImpostorCount != 1) return;
        ChangeRole(inMeeting);
    }
    private void ChangeRole(bool inMeeting)
    {
        var scammer = _Player;

        if (Main.AliveImpostorCount < 2)
        {
            scammer.GetRoleClass().OnRemove(scammer.PlayerId);
            scammer.RpcChangeRoleBasis(CustomRoles.Refugee);
            scammer.RpcSetCustomRole(CustomRoles.Refugee);
            scammer.GetRoleClass().OnAdd(scammer.PlayerId);
            scammer.SyncSettings();

            scammer.RpcGuardAndKill();
            scammer.ResetKillCooldown();
            scammer.SetKillCooldown();

            if (inMeeting)
            {
                SendMessage(GetString("ScammerToRefugeeMsg"), sendTo: scammer.PlayerId);
            }
            else
            {
                scammer.Notify(string.Format(GetString("ScammerToRefugeeMsg"), CustomRoles.Refugee.ToString()));
            }
        }
    }
}
