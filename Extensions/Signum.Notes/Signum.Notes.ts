//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Security from '../../Signum/React/Signum.Security'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'


export const NoteEntity = new Type<NoteEntity>("Note");
export interface NoteEntity extends Entities.Entity {
  Type: "Note";
  title: string | null;
  target: Entities.Lite<Entities.Entity>;
  creationDate: string /*DateTime*/;
  text: string;
  createdBy: Entities.Lite<Security.IUserEntity>;
  noteType: NoteTypeSymbol | null;
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
  export const CreateNoteFromEntity : Operations.ConstructSymbol_From<NoteEntity, Entities.Entity> = registerSymbol("Operation", "NoteOperation.CreateNoteFromEntity");
  export const Save : Operations.ExecuteSymbol<NoteEntity> = registerSymbol("Operation", "NoteOperation.Save");
}

export module NoteTypeOperation {
  export const Save : Operations.ExecuteSymbol<NoteTypeSymbol> = registerSymbol("Operation", "NoteTypeOperation.Save");
}

export const NoteTypeSymbol = new Type<NoteTypeSymbol>("NoteType");
export interface NoteTypeSymbol extends Basics.SemiSymbol {
  Type: "NoteType";
}

