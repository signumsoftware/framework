//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'


export const ChatbotAgentEntity: Type<ChatbotAgentEntity> = new Type<ChatbotAgentEntity>("ChatbotAgent");
export interface ChatbotAgentEntity extends Entities.Entity {
  Type: "ChatbotAgent";
  key: ChatbotAgentTypeSymbol;
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

export const ChatbotAgentTypeSymbol: Type<ChatbotAgentTypeSymbol> = new Type<ChatbotAgentTypeSymbol>("ChatbotAgentType");
export interface ChatbotAgentTypeSymbol extends Basics.Symbol {
  Type: "ChatbotAgentType";
}

export namespace DefaultAgent {
  export const Introduction : ChatbotAgentTypeSymbol = registerSymbol("ChatbotAgentType", "DefaultAgent.Introduction");
  export const QuestionSumarizer : ChatbotAgentTypeSymbol = registerSymbol("ChatbotAgentType", "DefaultAgent.QuestionSumarizer");
  export const SearchControl : ChatbotAgentTypeSymbol = registerSymbol("ChatbotAgentType", "DefaultAgent.SearchControl");
}

