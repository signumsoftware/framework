namespace Signum.Basics;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
[EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
public abstract class SemiSymbol : Entity
{
    static Dictionary<Type, Dictionary<string, SemiSymbol>> Symbols = new Dictionary<Type, Dictionary<string, SemiSymbol>>();
    static Dictionary<Type, Dictionary<string, SemiSymbol>> FromDatabase = new Dictionary<Type, Dictionary<string, SemiSymbol>>();

    public SemiSymbol() { }

    /// <summary>
    /// Similar methods of inheritors will be automatically called by Signum.MSBuildTask using AutoInitiAttribute
    /// </summary>
    public SemiSymbol(Type declaringType, string fieldName)
    {
        this.fieldInfo = declaringType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (this.fieldInfo == null)
            throw new InvalidOperationException(string.Format("No field with name {0} found in {1}", fieldName, declaringType.Name));

        this.Key = declaringType.Name + "." + fieldName;
        this.Name = fieldName;

        try
        {
            Symbols.GetOrCreate(this.GetType()).Add(this.Key, this);
        }
        catch (Exception e) when (StartParameters.IgnoredCodeErrors != null)
        {
            //Could happend if Dynamic code has a duplicated name
            this.fieldInfo = null;
            this.Key = null;
            StartParameters.IgnoredCodeErrors.Add(e);
            return;
        }

        var dic = FromDatabase.TryGetC(this.GetType());
        if (dic != null)
        {
            SemiSymbol? semiS = dic.TryGetC(this.Key!);
            if (semiS != null)
                this.SetIdAndProps(semiS);
        }
    }

    [Ignore]
    FieldInfo? fieldInfo;
    [HiddenProperty]
    public FieldInfo? FieldInfo
    {
        get { return fieldInfo; }
        internal set { fieldInfo = value; }
    }

    [UniqueIndex(AllowMultipleNulls = true)]
    [StringLengthValidator(Min = 3, Max = 200)]
    public string? Key { get; set; }

    internal static Action<SemiSymbol> CallRetrieved = ss => { };

    public string NiceToString()
    {
        CallRetrieved(this);
        return this.FieldInfo?.NiceName() ?? this.Name;
    }

    public override bool Equals(object? obj)
    {
        return obj is SemiSymbol ss &&
            obj.GetType() == this.GetType() &&
            (ss.Key == Key || base.Equals(ss));
    }

    public override int GetHashCode()
    {
        return Key?.GetHashCode() ?? base.GetHashCode();
    }


    public virtual void SetIdAndProps(SemiSymbol other)
    {
        this.id = other.id;
        this.IsNew = false;
        this.ToStr = this.Key;
        this.Name = other.Name;
        if (this.Modified != ModifiedState.Sealed)
            this.Modified = ModifiedState.Sealed;
    }

    static SemiSymbol()
    {
        DescriptionManager.DefaultDescriptionOptions += DescriptionManager_IsSymbolContainer;
        DescriptionManager.Invalidate();
    }

    static DescriptionOptions? DescriptionManager_IsSymbolContainer(Type t)
    {
        return t.IsAbstract && t.IsSealed &&
            t.GetFields(BindingFlags.Static | BindingFlags.Public)
            .Any(a => typeof(SemiSymbol).IsAssignableFrom(a.FieldType)) ? DescriptionOptions.Members : (DescriptionOptions?)null;
    }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => this.Key ?? this.Name);

    public static void SetFromDatabase<S>(Dictionary<string, S> fromDatabase)
        where S : SemiSymbol
    {
        SemiSymbol.FromDatabase[typeof(S)] = fromDatabase.ToDictionaryEx(kvp=>kvp.Key, kvp => (SemiSymbol)kvp.Value);

        var symbols = SemiSymbol.Symbols.TryGetC(typeof(S));

        if (symbols != null)
        {
            foreach (var kvp in fromDatabase)
            {
                var s = symbols.TryGetC(kvp.Key);
                if (s != null)
                    s.SetIdAndProps(kvp.Value);
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

    internal static Dictionary<string, SemiSymbol> GetFromDatabase(Type type)
    {
        return SemiSymbol.FromDatabase.GetOrThrow(type);
    }
}
