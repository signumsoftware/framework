
namespace Signum.Engine.Maps;

public class NameSequence
{
    public static readonly NameSequence Void = new VoidNameSequence();

    class VoidNameSequence : NameSequence
    {
        public override string ToString()
        {
            return "Value";
        }
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
        if(PreSequence is VoidNameSequence)
            return Value;

        return "_".Combine(PreSequence.ToString(), Value);
    }
}
