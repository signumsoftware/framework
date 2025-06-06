//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Mailing from '../Signum.Mailing/Signum.Mailing'


export const MicrosoftGraphEmailServiceEntity: Type<MicrosoftGraphEmailServiceEntity> = new Type<MicrosoftGraphEmailServiceEntity>("MicrosoftGraphEmailService");
export interface MicrosoftGraphEmailServiceEntity extends Mailing.EmailServiceEntity {
  Type: "MicrosoftGraphEmailService";
  useActiveDirectoryConfiguration: boolean;
  azure_ApplicationID: string /*Guid*/ | null;
  azure_DirectoryID: string /*Guid*/ | null;
  azure_ClientSecret: string | null;
}

