using System.Data;

namespace Signum.Cache;

public class SemiCachedController<T> where T : Entity
{
    CachedTableBase cachedTable;

    public SemiCachedController(CachedTableBase cachedTable)
    {
        this.cachedTable = cachedTable;

        CacheLogic.semiControllers.GetOrCreate(typeof(T)).Add(cachedTable);

        var ee = Schema.Current.EntityEvents<T>();
        ee.Saving += ident =>
        {
            if (ident.IsGraphModified && !ident.IsNew)
            {
                cachedTable.LoadAll();

                if (cachedTable.Contains(ident.Id))
                    DisableAndInvalidate();
            }
        };
        //ee.PreUnsafeDelete += query => DisableAndInvalidate();
        ee.PreUnsafeUpdate += (update, entityQuery) => { DisableAndInvalidateMassive(entityQuery); return null; };
        ee.PreUnsafeInsert += (query, constructor, entityQuery) =>
        {
            if (constructor.Body.Type.IsInstantiationOf(typeof(MListElement<,>)))
                DisableAndInvalidateMassive(entityQuery);

            return constructor;
        };
        ee.PreUnsafeMListDelete += (mlistQuery, entityQuery) => { DisableAndInvalidateMassive(entityQuery); return null; };
        ee.PreBulkInsert += inMListTable =>
        {
            if (inMListTable)
                DisableAndInvalidateMassive(null);
        };
    }

    public int? MassiveInvalidationCheckLimit = 500;

    void DisableAndInvalidateMassive(IQueryable<T>? elements)
    {
        var asssumeAsInvalidation = CacheLogic.assumeMassiveChangesAsInvalidations.Value?.TryGetS(typeof(T));

        if (asssumeAsInvalidation == false)
        {

        }
        else if(asssumeAsInvalidation == true)
        {
            DisableAndInvalidate();
        }
        else if (asssumeAsInvalidation == null) //Default
        {
            if (MassiveInvalidationCheckLimit != null && elements != null)
            {
                var ids = elements.Select(a => a.Id).Distinct().Take(MassiveInvalidationCheckLimit.Value).ToList();
                if (ids.Count == MassiveInvalidationCheckLimit.Value)
                    throw new InvalidOperationException($"MassiveInvalidationCheckLimit reached when trying to determine if the massive operation will affect the semi-cached instances of {typeof(T).TypeName()}.");

                cachedTable.LoadAll();

                if (ids.Any(cachedTable.Contains))
                    DisableAndInvalidate();
                else
                    return;
            }
            else
            {
                throw new InvalidOperationException($"Impossible to determine if the massive operation will affect the semi-cached instances of {typeof(T).TypeName()}. Execute CacheLogic.AssumeMassiveChangesAsInvalidations to desanbiguate.");
            }
        }
    }

    void DisableAndInvalidate()
    {
        CacheLogic.DisableAllConnectedTypesInTransaction(this.cachedTable.controller.Type);

        Transaction.PostRealCommit -= Transaction_PostRealCommit;
        Transaction.PostRealCommit += Transaction_PostRealCommit;
    }

    void Transaction_PostRealCommit(Dictionary<string, object> obj)
    {
        cachedTable.ResetAll(forceReset: false);
        CacheLogic.NotifyInvalidateAllConnectedTypes(this.cachedTable.controller.Type);
    }
}
