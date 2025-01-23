﻿using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Gangster : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Gangster;
    private const int Id = 3300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Gangster);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CanRecruit(player.PlayerId) ? CustomButton.Get("Sidekick") : null;

    private static OptionItem RecruitLimitOpt;
    private static OptionItem KillCooldown;
    public static OptionItem SheriffCanBeMadmate;
    public static OptionItem MayorCanBeMadmate;
    public static OptionItem NGuesserCanBeMadmate;
    public static OptionItem JudgeCanBeMadmate;
    public static OptionItem RetributionistCanBeMadmate;
    public static OptionItem MarshallCanBeMadmate;
    public static OptionItem OverseerCanBeMadmate;
    public static OptionItem CovenCanBeMadmate;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Gangster);
        KillCooldown = FloatOptionItem.Create(Id + 10, "GangsterRecruitCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster])
            .SetValueFormat(OptionFormat.Seconds);
        RecruitLimitOpt = IntegerOptionItem.Create(Id + 12, "GangsterRecruitLimit", new(1, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster])
            .SetValueFormat(OptionFormat.Times);

        CovenCanBeMadmate = BooleanOptionItem.Create(Id + 21, "GanCovenCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        SheriffCanBeMadmate = BooleanOptionItem.Create(Id + 14, "GanSheriffCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        MayorCanBeMadmate = BooleanOptionItem.Create(Id + 15, "GanMayorCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        NGuesserCanBeMadmate = BooleanOptionItem.Create(Id + 16, "GanNGuesserCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        JudgeCanBeMadmate = BooleanOptionItem.Create(Id + 17, "GanJudgeCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        MarshallCanBeMadmate = BooleanOptionItem.Create(Id + 18, "GanMarshallCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        RetributionistCanBeMadmate = BooleanOptionItem.Create(Id + 20, "GanRetributionistCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        OverseerCanBeMadmate = BooleanOptionItem.Create(Id + 19, "GanOverseerCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = RecruitLimitOpt.GetInt();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanRecruit(id) ? KillCooldown.GetFloat() : Options.DefaultKillCooldown;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (CanRecruit(playerId))
            HudManager.Instance.KillButton.OverrideText(GetString("GangsterButtonText"));
        else
            HudManager.Instance.KillButton.OverrideText(GetString("KillButtonText"));
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!CanRecruit(killer.PlayerId))
        {
            return true;
        }

        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("CantRecruit")));
            return true;
        }

        if (CanRecruit(killer.PlayerId))
        {
            if (!killer.GetCustomSubRoles().Find(x => x.IsBetrayalAddonV2(), out var convertedAddon)
                && target.CanBeMadmate(forGangster: true)
                && CanBeGansterRecruit(target))
            {
                convertedAddon = CustomRoles.Madmate;
            }
            else if (killer.IsAnySubRole(x => x.IsBetrayalAddonV2()))
            {
                foreach (var subRole in killer.GetCustomSubRoles().Where(x => x.IsBetrayalAddonV2()))
                {
                    switch (subRole)
                    {
                        case CustomRoles.Admired when Admirer.CanBeAdmired(target, killer):
                            convertedAddon = CustomRoles.Admired;
                            Admirer.AdmiredList[killer.PlayerId].Add(target.PlayerId);
                            Admirer.SendRPC(killer.PlayerId, target.PlayerId);
                            break;
                        case CustomRoles.Enchanted when Ritualist.CanBeConverted(target):
                            convertedAddon = CustomRoles.Enchanted;
                            break;
                        case CustomRoles.Recruit when Jackal.CanBeSidekick(target):
                            convertedAddon = CustomRoles.Recruit;
                            break;
                        case CustomRoles.Charmed when Cultist.CanBeCharmed(target):
                            convertedAddon = CustomRoles.Charmed;
                            break;
                        case CustomRoles.Infected when Infectious.CanBeBitten(target):
                            convertedAddon = CustomRoles.Infected;
                            break;
                        case CustomRoles.Contagious when target.CanBeInfected():
                            convertedAddon = CustomRoles.Contagious;
                            break;
                    }
                }
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + convertedAddon.ToString(), "Ritualist Assign");
                target.RpcSetCustomRole(convertedAddon);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(convertedAddon), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(convertedAddon), GetString("BeRecruitedByGangster")));
            }
            else goto GangsterFailed;

            AbilityLimit--;

            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);

            killer.ResetKillCooldown();
            killer.SetKillCooldown(forceAnime: true);
            target.ResetKillCooldown();
            target.SetKillCooldown(forceAnime: true);

            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AbilityLimit}次招募机会", "Gangster");
            SendSkillRPC();
            return false;
        }

    GangsterFailed:
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("GangsterRecruitmentFailure")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{AbilityLimit}次招募机会", "Gangster");
        SendSkillRPC();
        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);
        return true;
    }
    public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(CanRecruit(playerId) ? Utils.GetRoleColor(CustomRoles.Gangster).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");

    private bool CanRecruit(byte id) => AbilityLimit >= 1;
    private static bool CanBeGansterRecruit(PlayerControl pc)
    {
        return pc != null && (pc.IsNonRebelCrewmate() || pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsCoven())
            && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Loyal)
            && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
}
