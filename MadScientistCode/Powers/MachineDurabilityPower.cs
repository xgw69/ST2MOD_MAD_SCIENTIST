using MegaCrit.Sts2.Core.Entities.Powers;

namespace MadScientist.MadScientistCode.Powers;

/// <summary>机器耐久：0~20 的增益层数，代表机器的剩余耐久。</summary>
public class MachineDurabilityPower : MadScientistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool AllowNegative => false;
}
