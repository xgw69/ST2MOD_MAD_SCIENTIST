using MegaCrit.Sts2.Core.Entities.Powers;

namespace MadScientist.MadScientistCode.Powers;

/// <summary>机器损毁：本场战斗内不能再使用消耗耐久的牌，修复也失效。</summary>
public class MachineBrokenPower : MadScientistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;
}
