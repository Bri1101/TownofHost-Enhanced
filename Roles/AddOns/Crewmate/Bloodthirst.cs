using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Bloodthirst : IAddon
{
    public CustomRoles Role => CustomRoles.Bloodthirst;
    private const int Id = 21700;
    public AddonTypes Type => AddonTypes.Experimental;
    public static OptionItem KillCooldown;
    public static OptionItem CanVent;
    public static OptionItem HasImpostorVision;
    public static OptionItem CanUsesSabotage;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Bloodthirst, canSetNum: false, tab: TabGroup.Addons);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodthirst])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodthirst]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 12, "ImpostorVision", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodthirst]);
        CanUsesSabotage = BooleanOptionItem.Create(Id + 13, "CanUseSabotage", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodthirst]);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

    ///----------------------------------------Check Bloodthirst Assign----------------------------------------///
    public static bool CheckBloodthirstAssign()
    {
        int optnknum = NeutralKillingRolesMinPlayer.GetInt() + NeutralKillingRolesMaxPlayer.GetInt();
        int assignvalue = IRandom.Instance.Next(1, optnknum);

        if (optnknum == 0) return false;
        else if (assignvalue > optnknum) return false;

        return true;
    }

    public static int ExtraNKSpotBloodthirst
        => CheckBloodthirstAssign() ? 0 : 1;
    ///-------------------------------------------------------------------------------------------------///

    public static void ApplyGameOptions(IGameOptions opt, PlayerControl player)
    {
        if (player.Is(CustomRoles.Bloodthirst) && HasImpostorVision.GetBool())
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, opt.GetFloat(FloatOptionNames.ImpostorLightMod));
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, opt.GetFloat(FloatOptionNames.ImpostorLightMod));
        }
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static bool CanUseImpostorVentButton(PlayerControl pc) => pc.Is(CustomRoles.Bloodthirst) && CanVent.GetBool();
    public static bool CanUseSabotage(PlayerControl pc) => pc.Is(CustomRoles.Bloodthirst) && CanUsesSabotage.GetBool();
}
