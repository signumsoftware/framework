//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Security from '../../Signum/React/Signum.Security'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'


export const NoteEntity: Type<NoteEntity> = new Type<NoteEntity>("Note");
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
  export const NewNote: MessageKey = new MessageKey("NoteMessage", "NewNote");
  export const Note: MessageKey = new MessageKey("NoteMessage", "Note");
  export const _note: MessageKey = new MessageKey("NoteMessage", "_note");
  export const _notes: MessageKey = new MessageKey("NoteMessage", "_notes");
  export const CreateNote: MessageKey = new MessageKey("NoteMessage", "CreateNote");
  export const NoteCreated: MessageKey = new MessageKey("NoteMessage", "NoteCreated");
  export const Notes: MessageKey = new MessageKey("NoteMessage", "Notes");
  export const ViewNotes: MessageKey = new MessageKey("NoteMessage", "ViewNotes");
}

export module NoteOperation {
  export const CreateNoteFromEntity : Operations.ConstructSymbol_From<NoteEntity, Entities.Entity> = registerSymbol("Operation", "NoteOperation.CreateNoteFromEntity");
  export const Save : Operations.ExecuteSymbol<NoteEntity> = registerSymbol("Operation", "NoteOperation.Save");
}

export module NoteTypeOperation {
  export const Save : Operations.ExecuteSymbol<NoteTypeSymbol> = registerSymbol("Operation", "NoteTypeOperation.Save");
}

export const NoteTypeSymbol: Type<NoteTypeSymbol> = new Type<NoteTypeSymbol>("NoteType");
export interface NoteTypeSymbol extends Basics.SemiSymbol {
  Type: "NoteType";
}

