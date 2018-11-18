import { ajaxPost, ajaxGet } from '@framework/Services';

export namespace API {
  export function typeHelp(typeName: string, mode: TypeHelpMode): Promise<TypeHelp | undefined> {
    return ajaxGet<TypeHelp | undefined>({ url: `~/api/typeHelp/${typeName}/${mode}` });
  }

  export function autocompleteEntityCleanType(request: AutocompleteEntityCleanType): Promise<string[]> {
    return ajaxPost<string[]>({ url: "~/api/typeHelp/autocompleteEntityCleanType" }, request);
  }

  export function autocompleteType(request: AutocompleteTypeRequest): Promise<string[]> {
    return ajaxPost<string[]>({ url: "~/api/typeHelp/autocompleteType" }, request);
  }
}

export type TypeHelpMode = "TypeScript" | "CSharp";

export interface TypeHelp {
  type: string;
  cleanTypeName: string;
  isEnum: boolean;
  members: TypeMemberHelp[];
}

export interface TypeMemberHelp {
  propertyString: string;
  name?: string; //Mixins, MListElements
  type: string;
  isExpression: boolean;
  isEnum: boolean;
  cleanTypeName: string | null;
  subMembers: TypeMemberHelp[];
}

export interface AutocompleteTypeRequest {
  query: string;
  limit: number;
  includeBasicTypes: boolean;
  includeEntities?: boolean;
  includeModelEntities?: boolean;
  includeEmbeddedEntities?: boolean;
  includeMList?: boolean;
  includeQueriable?: boolean;
}

export interface AutocompleteEntityCleanType {
  query: string;
  limit: number;
}


