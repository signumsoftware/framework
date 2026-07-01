namespace Signum.Basics;

/// <summary>
/// A place to anchor an application tour. Lives in the framework (not in the optional Signum.Tour
/// extension) so any module can declare triggers and register them without referencing Signum.Tour.
/// The Signum.Tour extension consumes <see cref="TourTriggerLogic"/> when it is started; if the
/// application does not start it, the registrations are simply ignored.
/// </summary>
[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class TourTriggerSymbol : Symbol
{
    private TourTriggerSymbol() { }

    public TourTriggerSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

public static class TourTriggerLogic
{
    static HashSet<TourTriggerSymbol> tourTriggers = new HashSet<TourTriggerSymbol>();

    static Dictionary<TourTriggerSymbol, Type> triggerTypes = new Dictionary<TourTriggerSymbol, Type>();

    public static IEnumerable<TourTriggerSymbol> RegisteredTourTriggers => tourTriggers;

    public static void RegisterTourTriggers(params TourTriggerSymbol[] triggers)
    {
        foreach (var t in triggers)
        {
            if (t == null)
                throw AutoInitAttribute.ArgumentNullException(typeof(TourTriggerSymbol), nameof(triggers));

            tourTriggers.Add(t);
        }
    }

    /// <summary>
    /// Registers a tour trigger symbol associated with an entity type, so the tour editor can
    /// offer that type's properties as Property CSS steps (like a Lite&lt;TypeEntity&gt; trigger does).
    /// </summary>
    public static void RegisterTriggerType(TourTriggerSymbol trigger, Type type)
    {
        if (trigger == null)
            throw AutoInitAttribute.ArgumentNullException(typeof(TourTriggerSymbol), nameof(trigger));

        tourTriggers.Add(trigger);
        triggerTypes.Add(trigger, type);
    }

    public static Type? GetTriggerType(TourTriggerSymbol trigger) => triggerTypes.TryGetC(trigger);
}
