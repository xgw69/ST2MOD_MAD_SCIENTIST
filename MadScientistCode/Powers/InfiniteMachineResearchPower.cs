using MegaCrit.Sts2.Core.Entities.Powers;

namespace MadScientist.MadScientistCode.Powers;

/// <summary>机器无限研究：消耗机器耐久的牌不再减少耐久。</summary>
public class InfiniteMachineResearchPower : MadScientistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
