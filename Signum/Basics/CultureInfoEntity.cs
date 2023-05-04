
using System.Globalization;

namespace Signum.Basics;

[EntityKind(EntityKind.String, EntityData.Master), AllowUnathenticated]
public class CultureInfoEntity : Entity
{
    public CultureInfoEntity() { }

    public CultureInfoEntity(CultureInfo ci)
    {
        Name = ci.Name;
        NativeName = ci.NativeName;
        EnglishName = ci.EnglishName;
    }

    [UniqueIndex]
    [StringLengthValidator(Min = 2, Max = 10)]
    public string Name { get; set; }

    [StringLengthValidator(Max = 200), NotNullValidator(DisabledInModelBinder = true)]
    public string NativeName { get; private set; }

    [StringLengthValidator(Max = 200), NotNullValidator(DisabledInModelBinder = true)]
    public string EnglishName { get; private set; }

    protected internal override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Name) && Name.HasText())
        {
            try
            {
                CultureInfo.GetCultureInfo(this.Name);
            }
            catch (CultureNotFoundException)
            {
                return "'{0}' is not a valid culture name".FormatWith(Name);
            }
        }

        return base.PropertyValidation(pi);
    }

    [AutoExpressionField]
    public bool IsNeutral => As.Expression(() => !Name.Contains("-"));

    protected internal override void PreSaving(PreSavingContext ctx)
    {
        try
        {
            var ci = CultureInfo.GetCultureInfo(Name);

            //To be more resilient with diferent versions of windows
            if (this.IsGraphModified || EnglishName == null)
                EnglishName = ci.EnglishName;
            if (this.IsGraphModified || NativeName == null)
                NativeName = ci.NativeName;
        }
        catch (CultureNotFoundException)
        {
        }

        base.PreSaving(ctx);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => EnglishName);
}

[AutoInit]
public static class CultureInfoOperation
{
    public static ExecuteSymbol<CultureInfoEntity> Save;
    public static DeleteSymbol<CultureInfoEntity> Delete;
}
