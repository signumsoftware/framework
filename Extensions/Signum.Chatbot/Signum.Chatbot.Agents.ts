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

export const ChatbotAgentEntity: Type<ChatbotAgentEntity> = new Type<ChatbotAgentEntity>("ChatbotAgent");
export interface ChatbotAgentEntity extends Entities.Entity {
  Type: "ChatbotAgent";
  code: ChatbotAgentCodeSymbol;
  shortDescription: string;
  chatbotPrompts: Entities.MList<ChatbotAgentPromptEmbedded>;
}

export namespace ChatbotAgentOperation {
  export const Save : Operations.ExecuteSymbol<ChatbotAgentEntity> = registerSymbol("Operation", "ChatbotAgentOperation.Save");
  export const Delete : Operations.DeleteSymbol<ChatbotAgentEntity> = registerSymbol("Operation", "ChatbotAgentOperation.Delete");
}

export const ChatbotAgentPromptEmbedded: Type<ChatbotAgentPromptEmbedded> = new Type<ChatbotAgentPromptEmbedded>("ChatbotAgentPromptEmbedded");
export interface ChatbotAgentPromptEmbedded extends Entities.EmbeddedEntity {
  Type: "ChatbotAgentPromptEmbedded";
  promptName: string | null;
  content: string;
}

export namespace DefaultAgent {
  export const Introduction : ChatbotAgentCodeSymbol = registerSymbol("ChatbotAgentCode", "DefaultAgent.Introduction");
  export const QuestionSumarizer : ChatbotAgentCodeSymbol = registerSymbol("ChatbotAgentCode", "DefaultAgent.QuestionSumarizer");
  export const Sience : ChatbotAgentCodeSymbol = registerSymbol("ChatbotAgentCode", "DefaultAgent.Sience");
  export const SearchControl : ChatbotAgentCodeSymbol = registerSymbol("ChatbotAgentCode", "DefaultAgent.SearchControl");
}

