using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MadScientist.MadScientistCode.Cards.Fusion;

/// <summary>
/// 融合牌：战斗内临时卡，持有若干母卡，打出时按顺序结算母卡效果。
/// 规则：费用 = 母卡费用之和；词条并集；含 X 费母卡时整张为 X 费。
/// 名称/卡图参考 CardFusion 的做法（名字拼接、卡图取第一张）。
/// </summary>
public sealed class FusedCard : CardModel
{
    private static readonly MethodInfo OnPlayMethod =
        typeof(CardModel).GetMethod("OnPlay", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly List<CardModel> _sources = new();

    /// <summary>Parameterless ctor required by the model database (canonical prototype).</summary>
    public FusedCard()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public FusedCard(IReadOnlyList<CardModel> sources)
        : base(
            SumCost(sources),
            PickType(sources),
            CardRarity.Common,
            PickTarget(sources))
    {
        _sources = sources.ToList();
    }

    public IReadOnlyList<CardModel> Sources => _sources;

    public override bool CanBeGeneratedByModifiers => false;

    public override string Title => string.Join("+", _sources.Select(c => c.Title));

    public override string PortraitPath => _sources.Count > 0 ? _sources[0].PortraitPath : base.PortraitPath;

    protected override int CanonicalEnergyCost => SumCost(_sources);

    protected override bool HasEnergyCostX => _sources.Any(c => c.EnergyCost.CostsX);

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        _sources.SelectMany(c => c.Keywords).Distinct();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var source in _sources)
        {
            if (OnPlayMethod.Invoke(source, [choiceContext, cardPlay]) is Task task)
            {
                await task;
            }
        }
    }

    private static int SumCost(IReadOnlyList<CardModel> sources)
    {
        if (sources.Any(c => c.EnergyCost.CostsX))
        {
            return 0;
        }

        return Math.Max(0, sources.Sum(c => c.EnergyCost.Canonical));
    }

    private static CardType PickType(IReadOnlyList<CardModel> sources)
    {
        if (sources.Any(c => c.Type == CardType.Attack)) return CardType.Attack;
        if (sources.Any(c => c.Type == CardType.Power)) return CardType.Power;
        return CardType.Skill;
    }

    private static TargetType PickTarget(IReadOnlyList<CardModel> sources)
    {
        if (sources.Any(c => c.TargetType == TargetType.AllEnemies)) return TargetType.AllEnemies;
        if (sources.Any(c => c.TargetType == TargetType.AnyEnemy)) return TargetType.AnyEnemy;
        return TargetType.Self;
    }
}
