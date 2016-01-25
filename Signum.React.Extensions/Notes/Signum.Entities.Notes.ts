//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection' 

import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities' 

export const NoteEntity_Type = new Type<NoteEntity>("Note");
export interface NoteEntity extends Entities.Entity {
    title?: string;
    target?: Entities.Lite<Entities.Entity>;
    creationDate?: string;
    text?: string;
    createdBy?: Entities.Lite<Entities.Basics.IUserEntity>;
    noteType?: NoteTypeEntity;
}

export module NoteMessage {
    export const NewNote = new MessageKey("NoteMessage", "NewNote");
    export const Note = new MessageKey("NoteMessage", "Note");
    export const _note = new MessageKey("NoteMessage", "_note");
    export const _notes = new MessageKey("NoteMessage", "_notes");
    export const CreateNote = new MessageKey("NoteMessage", "CreateNote");
    export const NoteCreated = new MessageKey("NoteMessage", "NoteCreated");
    export const Notes = new MessageKey("NoteMessage", "Notes");
    export const ViewNotes = new MessageKey("NoteMessage", "ViewNotes");
}

export module NoteOperation {
    export const CreateNoteFromEntity : Entities.ConstructSymbol_From<NoteEntity, Entities.Entity> = registerSymbol({ Type: "Operation", key: "NoteOperation.CreateNoteFromEntity" });
    export const Save : Entities.ExecuteSymbol<NoteEntity> = registerSymbol({ Type: "Operation", key: "NoteOperation.Save" });
}

export const NoteTypeEntity_Type = new Type<NoteTypeEntity>("NoteType");
export interface NoteTypeEntity extends Entities.Basics.SemiSymbol {
}

export module NoteTypeOperation {
    export const Save : Entities.ExecuteSymbol<NoteTypeEntity> = registerSymbol({ Type: "Operation", key: "NoteTypeOperation.Save" });
}

