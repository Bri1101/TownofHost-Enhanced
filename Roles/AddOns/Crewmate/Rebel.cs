using static TOHE.Options;

namespace TOHE.Roles.AddOns.Crewmate;

public class Rebel : IAddon
{
    private const int Id = 31400;
    public AddonTypes Type => AddonTypes.Misc;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Rebel, canSetNum: true, tab: TabGroup.Addons);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}