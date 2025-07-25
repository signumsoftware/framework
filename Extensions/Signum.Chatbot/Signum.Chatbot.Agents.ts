//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'


export const ChatbotAgentCodeSymbol: Type<ChatbotAgentCodeSymbol> = new Type<ChatbotAgentCodeSymbol>("ChatbotAgentCode");
export interface ChatbotAgentCodeSymbol extends Basics.Symbol {
  Type: "ChatbotAgentCode";
}

export const ChatbotAgentDescriptionsEmbedded: Type<ChatbotAgentDescriptionsEmbedded> = new Type<ChatbotAgentDescriptionsEmbedded>("ChatbotAgentDescriptionsEmbedded");
export interface ChatbotAgentDescriptionsEmbedded extends Entities.EmbeddedEntity {
  Type: "ChatbotAgentDescriptionsEmbedded";
  promptName: string | null;
  content: string;
}

export const ChatbotAgentEntity: Type<ChatbotAgentEntity> = new Type<ChatbotAgentEntity>("ChatbotAgent");
export interface ChatbotAgentEntity extends Entities.Entity {
  Type: "ChatbotAgent";
  code: ChatbotAgentCodeSymbol;
  shortDescription: string;
  descriptions: Entities.MList<ChatbotAgentDescriptionsEmbedded>;
}

export namespace ChatbotAgentMessage {
  export const Default: MessageKey = new MessageKey("ChatbotAgentMessage", "Default");
}

export namespace ChatbotAgentOperation {
  export const Save : Operations.ExecuteSymbol<ChatbotAgentEntity> = registerSymbol("Operation", "ChatbotAgentOperation.Save");
  export const Delete : Operations.DeleteSymbol<ChatbotAgentEntity> = registerSymbol("Operation", "ChatbotAgentOperation.Delete");
}

export namespace DefaultAgent {
  export const Introduction : ChatbotAgentCodeSymbol = registerSymbol("ChatbotAgentCode", "DefaultAgent.Introduction");
  export const QuestionSumarizer : ChatbotAgentCodeSymbol = registerSymbol("ChatbotAgentCode", "DefaultAgent.QuestionSumarizer");
  export const SearchControl : ChatbotAgentCodeSymbol = registerSymbol("ChatbotAgentCode", "DefaultAgent.SearchControl");
}

