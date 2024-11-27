using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System.Text;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Werewolf : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 18400;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem RampageCD;
    private static OptionItem RampageDur;

    private static readonly Dictionary<byte, long> RampageDuration = [];
    private static readonly Dictionary<byte, long> RampageCooldown = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Werewolf, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 3f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Werewolf])
            .SetValueFormat(OptionFormat.Seconds);
        RampageCD = FloatOptionItem.Create(Id + 11, "WWRampageCD", new(0f, 180f, 2.5f), 35f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Werewolf])
            .SetValueFormat(OptionFormat.Seconds);
        RampageDur = FloatOptionItem.Create(Id + 12, "WWRampageDur", new(0f, 180f, 1f), 12f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Werewolf])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        playerIdList.Clear();
        RampageCooldown.Clear();
        RampageDuration.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => IsRampaging(pc.PlayerId) || pc.inVent;
    public override bool CanUseKillButton(PlayerControl pc) => IsRampaging(pc.PlayerId);
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        if (IsRampaging(playerId))
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.CrewLightMod, opt.GetFloat(FloatOptionNames.ImpostorLightMod));
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, opt.GetFloat(FloatOptionNames.ImpostorLightMod));
        }
        else
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, opt.GetFloat(FloatOptionNames.CrewLightMod));
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, opt.GetFloat(FloatOptionNames.CrewLightMod));
        }
        AURoleOptions.ShapeshifterCooldown = RampageDur.GetFloat() + 0.5f;
    }
    private void SendRPC(PlayerControl pc)
    {
        if (!pc.IsNonHostModdedClient()) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, pc.GetClientId());
        writer.WriteNetObject(_Player);//SetWWTimer
        writer.Write((RampageCooldown.TryGetValue(pc.PlayerId, out var x) ? x : -1).ToString());
        writer.Write((RampageDuration.TryGetValue(pc.PlayerId, out var y) ? y : -1).ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        RampageCooldown.Clear();
        RampageDuration.Clear();
        long cooldown = long.Parse(reader.ReadString());
        long rampage = long.Parse(reader.ReadString());
        if (cooldown > 0) RampageCooldown.Add(PlayerControl.LocalPlayer.PlayerId, cooldown);
        if (rampage > 0) RampageDuration.Add(PlayerControl.LocalPlayer.PlayerId, rampage);
    }

    private static bool CanRampage(byte id)
        => GameStates.IsInTask && !RampageDuration.ContainsKey(id) && !RampageCooldown.ContainsKey(id);

    private static bool IsRampaging(byte id)
        => RampageDuration.ContainsKey(id);

    public override void AfterMeetingTasks()
    {
        RampageCooldown.Clear();
        RampageDuration.Clear();
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId)).ToArray())
        {
            RampageCooldown.Add(pc.PlayerId, GetTimeStamp());
            SendRPC(pc);
        }
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || !Main.IntroDestroyed) return;
        var playerId = player.PlayerId;
        var needSync = false;

        if (RampageCooldown.TryGetValue(playerId, out var time) && time + (long)RampageCD.GetFloat() < nowTime)
        {
            RampageCooldown.Remove(playerId);
            if (!player.IsModded()) player.Notify(GetString("WWCanRampage"));
            if (!player.IsModded()) player.RpcChangeRoleBasis(CustomRoles.Werewolf);
            needSync = true;
        }

        foreach (var werewolfInfo in RampageDuration)
        {
            var werewolfId = werewolfInfo.Key;
            var werewolf = GetPlayerById(werewolfId);
            if (werewolf == null) continue;

            var remainTime = werewolfInfo.Value + (long)RampageDur.GetFloat() - nowTime;

            if (remainTime < 0 || !werewolf.IsAlive())
            {
                RampageCooldown.Remove(werewolfId);
                RampageCooldown.Add(werewolfId, nowTime);
                werewolf.Notify(GetString("WWRampageOut"));
                if (!werewolf.IsModded()) werewolf.RpcChangeRoleBasis(CustomRoles.CrewmateTOHE);

                needSync = true;
                RampageDuration.Remove(werewolfId);
            }
            else if (remainTime <= 10)
            {
                if (!werewolf.IsModded())
                    werewolf.Notify(string.Format(GetString("WWRampageCountdown"), remainTime), sendInLog: false);
            }
        }

        if (needSync)
        {
            SendRPC(player);
        }
    }

    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        if (!AmongUsClient.Instance.AmHost || IsRampaging(shapeshifter.PlayerId)) return;
        _ = new LateTask(() =>
        {
            if (CanRampage(shapeshifter.PlayerId))
            {
                RampageDuration.Add(shapeshifter.PlayerId, GetTimeStamp());
                SendRPC(shapeshifter);
                shapeshifter.Notify(GetString("WWRampaging"), RampageDur.GetFloat());
                shapeshifter.RpcSetVentInteraction();
            }
            else return;
        }, 0.5f, "Werewolf Vent");
    }
    public override string GetLowerText(PlayerControl pc, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (pc == null || !GameStates.IsInTask || !PlayerControl.LocalPlayer.IsAlive()) return "";
        var str = new StringBuilder();
        if (IsRampaging(pc.PlayerId))
        {
            var remainTime = RampageDuration[pc.PlayerId] + (long)RampageDur.GetFloat() - GetTimeStamp();
            str.Append(string.Format(GetString("WWRampageCountdown"), remainTime + 1));
        }
        else if (RampageCooldown.TryGetValue(pc.PlayerId, out var time))
        {
            var cooldown = time + (long)RampageCD.GetFloat() - GetTimeStamp();
            str.Append(string.Format(GetString("WWCD"), cooldown + 2));
        }
        else
        {
            str.Append(GetString("WWCanRampage"));
        }
        return str.ToString();
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("KillButtonText"));
        hud.AbilityButton.OverrideText(GetString("WerewolfShapeshiftButtonText"));
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (Medic.IsProtected(target.PlayerId)) return false;
        if (!IsRampaging(killer.PlayerId)) return false;
        if (target.IsTransformedNeutralApocalypse()) return false;
        if ((target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && Mini.Age < 18) return false;
        return true;
    }
}
