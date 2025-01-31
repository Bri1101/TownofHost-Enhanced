using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Necroview : IAddon
{
    public CustomRoles Role => CustomRoles.Necroview;
    private const int Id = 19600;
    public AddonTypes Type => AddonTypes.Helpful;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Necroview, canSetNum: true, tab: TabGroup.Addons, teamSpawnOptions: true);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }

    public static string NameColorOptions(PlayerControl target)
    {
        var customRole = target.GetCustomRole();

        foreach (var SubRole in target.GetCustomSubRoles())
        {
            if (SubRole is CustomRoles.Charmed
                or CustomRoles.Infected
                or CustomRoles.Contagious
                or CustomRoles.Egoist
                or CustomRoles.Recruit)
                return Main.roleColors[CustomRoles.Knight];
        }

        if (customRole.IsImpostorTeamV2() || target.IsAnySubRole(role => role.IsImpostorTeamV2()))
        {
            return Main.roleColors[CustomRoles.Impostor];
        }

        if (customRole.IsCrewmateTeamV2() || target.Is(CustomRoles.Admired) || target.IsAnySubRole(role => role.IsCrewmateTeamV2()))
        {
            return Main.roleColors[CustomRoles.Bait];
        }

        if (customRole.IsCoven() || customRole.Equals(CustomRoles.Enchanted))
        {
            return Main.roleColors[CustomRoles.Coven];
        }
        return Main.roleColors[CustomRoles.Knight];
    }
}

