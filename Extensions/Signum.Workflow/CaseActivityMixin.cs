
namespace Signum.Workflow;

public class CaseActivityMixin : MixinEntity
{
    CaseActivityMixin(ModifiableEntity mainEntity, MixinEntity next)
        : base(mainEntity, next)
    {

        this.CaseActivity = WorkflowActivityInfo.Current.CaseActivity?.ToLite();
    }

    public Lite<CaseActivityEntity>? CaseActivity { get; set; }

    //protected override void CopyFrom(MixinEntity mixin, object[] args)
    //{
    //    this.CaseActivity = ((CaseActivityMixin)mixin).CaseActivity;
    //}
}
