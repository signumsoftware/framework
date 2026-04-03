using Signum.Basics;
using System.ComponentModel;

namespace Signum.Notes;

[EntityKind(EntityKind.Main, EntityData.Transactional)]
public class NoteEntity : Entity
{
    [StringLengthValidator(Max = 100)]
    public string? Title { get; set; }

    [ImplementedByAll]
    public Lite<Entity> Target { get; set; }

    public DateTime CreationDate { get; set; } = Clock.Now;

    [StringLengthValidator(Min = 1, MultiLine = true)]
    public string Text { get; set; }

    
    public Lite<IUserEntity> CreatedBy { get; set; } = UserHolder.Current.User;

    public override string ToString()
    {
        return " - ".Combine(Title, Text.FirstNonEmptyLine()).Etc(100);
    }

    public NoteTypeSymbol? NoteType { get; set; }
}

[AutoInit]
public static class NoteOperation
{
    public static ConstructSymbol<NoteEntity>.From<Entity> CreateNoteFromEntity;
    public static ExecuteSymbol<NoteEntity> Save;
}

public enum NoteMessage
{
    [Description("New Note")]
    NewNote,
    [Description("Note:")]
    Note,
    [Description("note")]
    _note,
    [Description("notes")]
    _notes,
    CreateNote,
    NoteCreated,
    Notes,
    ViewNotes
}

[EntityKind(EntityKind.String, EntityData.Master, IsLowPopulation = true)]
public class NoteTypeSymbol : SemiSymbol
{
    public NoteTypeSymbol()
    {
    }

    public NoteTypeSymbol(Type declaringType, string fieldName) : base(declaringType, fieldName)
    {
    }
}

[AutoInit]
public static class NoteTypeOperation
{
    public static ExecuteSymbol<NoteTypeSymbol> Save;
}
