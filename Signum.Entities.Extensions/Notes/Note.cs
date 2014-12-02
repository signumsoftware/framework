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
        [SqlDbType(Size = 100)]
        string title;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Title
        {
            get { return title; }
            set { SetToStr(ref title, value); }
        }

        [ImplementedByAll]
        Lite<Entity> target;
        [NotNullValidator]
        public Lite<Entity> Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }

        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { Set(ref creationDate, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, Min = 1)]
        public string Text
        {
            get { return text; }
            set { SetToStr(ref text, value); }
        }

        Lite<IUserEntity> createdBy = UserHolder.Current.ToLite();
        [NotNullValidator]
        public Lite<IUserEntity> CreatedBy
        {
            get { return createdBy; }
            set { Set(ref createdBy, value); }
        }

        public override string ToString()
        {
            return " - ".Combine(title, text.FirstNonEmptyLine()).Etc(100);
        }

        NoteTypeEntity noteType;
        public NoteTypeEntity NoteType
        {
            get { return noteType; }
            set { Set(ref noteType, value); }
        }
    }

    public static class NoteOperation
    {
        public static readonly ConstructSymbol<NoteEntity>.From<Entity> CreateNoteFromEntity = OperationSymbol.Construct<NoteEntity>.From<Entity>();
        public static readonly ExecuteSymbol<NoteEntity> Save = OperationSymbol.Execute<NoteEntity>();
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

    public static class NoteTypeOperation
    {
        public static readonly ExecuteSymbol<NoteTypeEntity> Save = OperationSymbol.Execute<NoteTypeEntity>();
    }
}
