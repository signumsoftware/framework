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

namespace Signum.Entities.Notes
{
    [Serializable, EntityKind(EntityKind.Main)]
    public class NoteDN : IdentifiableEntity
    {
        [SqlDbType(Size = 100)]
        string title;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Title
        {
            get { return title; }
            set { SetToStr(ref title, value, () => Title); }
        }

        [ImplementedByAll]
        Lite<IdentifiableEntity> target;
        [NotNullValidator]
        public Lite<IdentifiableEntity> Target
        {
            get { return target; }
            set { Set(ref target, value, () => Target); }
        }

        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { Set(ref creationDate, value, () => CreationDate); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, Min = 1)]
        public string Text
        {
            get { return text; }
            set { SetToStr(ref text, value, () => Text); }
        }

        Lite<UserDN> createdBy = UserDN.Current.ToLite();
        [NotNullValidator]
        public Lite<UserDN> CreatedBy
        {
            get { return createdBy; }
            set { Set(ref createdBy, value, () => CreatedBy); }
        }

        public override string ToString()
        {
            return " - ".Combine(title, text.EtcLines(100)).Etc(100);
        }

        [ImplementedBy()]
        Lite<IIdentifiable> aditionalData;
        public Lite<IIdentifiable> AditionalData
        {
            get { return aditionalData; }
            set { Set(ref aditionalData, value, () => AditionalData); }
        }
    }

    public enum NoteOperation
    {
        Save,
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
}
