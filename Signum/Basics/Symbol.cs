namespace Signum.Basics;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true), TicksColumn(false)]
public abstract class Symbol : Entity
{
    static Dictionary<Type, Dictionary<string, Symbol>> Symbols = new Dictionary<Type, Dictionary<string, Symbol>>();
    static Dictionary<Type, Dictionary<string, PrimaryKey>> Ids = new Dictionary<Type, Dictionary<string, PrimaryKey>>();

    public Symbol() { }

    /// <summary>
    /// Similar methods of inheritors will be automatically called by Signum.MSBuildTask using AutoInitiAttribute
    /// </summary>
    public Symbol(Type declaringType, string fieldName)
    {
        this.fieldInfo = declaringType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

        if (this.fieldInfo == null)
            throw new InvalidOperationException(string.Format("No field with name {0} found in {1}", fieldName, declaringType.Name));

        this.Key = declaringType.Name + "." + fieldName;

        try
        {
            Symbols.GetOrCreate(this.GetType()).Add(this.Key, this);
        }
        catch (Exception e) when (StartParameters.IgnoredCodeErrors != null)
        {
            //Could happend if Dynamic code has a duplicated name
            this.fieldInfo = null!;
            this.Key = null!;
            StartParameters.IgnoredCodeErrors.Add(e);
            return;
        }

        var dic = Ids.TryGetC(this.GetType());
        if (dic != null)
        {
            PrimaryKey? id = dic.TryGetS(this.Key);
            if (id != null)
                this.SetId(id.Value);
        }
    }

    [Ignore]
    FieldInfo fieldInfo;
    [HiddenProperty]
    public FieldInfo FieldInfo
    {
        get { return fieldInfo; }
        internal set { fieldInfo = value; }
    }


    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 200)]
    public string Key { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => this.Key);

    public bool BaseEquals(object obj)
    {
        return base.Equals(obj);
    }

    public override bool Equals(object? obj)
    {
        return obj is Symbol &&
            obj.GetType() == this.GetType() &&
            ((Symbol)obj).Key == Key;
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }

    internal static Action<Symbol> CallRetrieved = ss => { };

    public string NiceToString()
    {
        return this.FieldInfo.NiceName();
    }

    public static void SetSymbolIds<S>(Dictionary<string, PrimaryKey> symbolIds)
        where S : Symbol
    {
        Symbol.Ids[typeof(S)] = symbolIds;

        var symbols = Symbol.Symbols.TryGetC(typeof(S));

        if (symbols != null)
        {
            foreach (var kvp in symbolIds)
            {
                var s = symbols.TryGetC(kvp.Key);
                if (s != null)
                    s.SetId(kvp.Value);
            }
        }
    }

    private void SetId(PrimaryKey id)
    {
        this.id = id;
        this.IsNew = false;
        this.ToStr = this.Key;
        if (this.Modified != ModifiedState.Sealed)
            this.Modified = ModifiedState.Sealed;
    }

    internal static Dictionary<string, PrimaryKey> GetSymbolIds(Type type)
    {
        return Symbol.Ids.GetOrThrow(type);
    }
}
