using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Mole : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Mole;
    private const int Id = 26000;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    public override bool BlockMoveInVent(PlayerControl pc) => !pc.Is(CustomRoles.Bloodthirst);
    //==================================================================\\

    private static OptionItem VentCooldown;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mole);
        VentCooldown = FloatOptionItem.Create(Id + 11, "MoleVentCooldown", new(5f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mole])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        var player = playerId.GetPlayer();
        if (player.Is(CustomRoles.Bloodthirst))
            AURoleOptions.ShapeshifterCooldown = VentCooldown.GetFloat();
        else
            AURoleOptions.EngineerCooldown = VentCooldown.GetFloat();
            AURoleOptions.EngineerInVentMaxTime = 1;
    }

    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        if (shapeshifter.Is(CustomRoles.Bloodthirst))
        {
            shapeshifter.RpcRandomVentTeleport();
        }
    }
    public override void OnExitVent(PlayerControl pc, int ventId)
    {
        if (pc.Is(CustomRoles.Bloodthirst)) return;

        float delay = 0.5f;

        _ = new LateTask(() =>
        {
            var vents = ShipStatus.Instance.AllVents.Where(x => x.Id != ventId).ToArray();
            var rand = IRandom.Instance;
            var vent = vents.RandomElement();

            Logger.Info($" {vent.transform.position}", "Mole vent teleport");
            pc.RpcTeleport(new Vector2(vent.transform.position.x, vent.transform.position.y + 0.3636f));
        }, delay, "Mole On Exit Vent");
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("MoleVentButtonText"));
    }
}
