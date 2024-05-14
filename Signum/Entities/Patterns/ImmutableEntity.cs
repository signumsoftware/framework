using System.Runtime.CompilerServices;

namespace Signum.Entities;

public abstract class ImmutableEntity : Entity
{
    [Ignore]
    bool allowTemporaly = false;

    public bool AllowChange
    {
        get { return allowTemporaly || IsNew; }
        set { allowTemporaly = value; Notify(() => AllowChange); }
    }

    protected override bool Set<T>(ref T variable, T value, [CallerMemberName]string? automaticPropertyName = null)
    {
        if (AllowChange)
            return base.Set(ref variable, value, automaticPropertyName!);
        else
            return base.SetIfNew(ref variable, value, automaticPropertyName!);
    }

    protected internal override void PreSaving(PreSavingContext ctx)
    {
        if (AllowChange)
            base.PreSaving(ctx);
        else
            if (Modified == ModifiedState.SelfModified)
                throw new InvalidOperationException($"Attempt to save a not new modified ImmutableEntity ({this.GetType().TypeName()})");
    }

    public IDisposable AllowChanges()
    {
        bool old = this.AllowChange;
        this.AllowChange = true;
        return new Disposable(() => this.AllowChange = old);
    }
}

