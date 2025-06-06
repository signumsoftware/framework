
namespace Signum.Engine.Maps;

public class NameSequence
{
    public static NameSequence GetVoid(bool isPostgres) => isPostgres ? new VoidNameSequence("value") : new VoidNameSequence("Value");

    class VoidNameSequence : NameSequence
    {
        public VoidNameSequence(string value) :base(value, null!){}
        public override string ToString() => Value;
    }

    readonly string Value;
    readonly NameSequence PreSequence;

    private NameSequence() : this(null!, null!) { }
    private NameSequence(string value, NameSequence preSequence)
    {
        this.Value = value;
        this.PreSequence = preSequence;
    }

    public NameSequence Add(string name)
    {
        return new NameSequence(name, this);
    }

    public override string ToString()
    {
        if (PreSequence is VoidNameSequence)
            return Value;

        return "_".Combine(PreSequence.ToString(), Value);
    }
}
