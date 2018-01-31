using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Signum.Entities.Notes
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class NoteEntity : Entity
    {
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Title { get; set; }

        [ImplementedByAll]
        [NotNullValidator]
        public Lite<Entity> Target { get; set; }

        public DateTime CreationDate { get; set; } = TimeZoneManager.Now;

        [StringLengthValidator(AllowNulls = false, Min = 1, MultiLine = true)]
        public string Text { get; set; }

        [NotNullValidator]
        public Lite<IUserEntity> CreatedBy { get; set; } = UserHolder.Current.ToLite();

        public override string ToString()
        {
            return " - ".Combine(Title, Text.FirstNonEmptyLine()).Etc(100);
        }

        public NoteTypeEntity NoteType { get; set; }
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

    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class NoteTypeEntity : SemiSymbol
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public NoteTypeEntity MakeSymbol([CallerMemberName]string memberName = null)
        {
            base.MakeSymbol(new StackFrame(1, false), memberName);
            return this;
        }

    }

    [AutoInit]
    public static class NoteTypeOperation
    {
        public static ExecuteSymbol<NoteTypeEntity> Save;
    }
}
