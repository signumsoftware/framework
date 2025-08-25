//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'


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

export const ChatbotConfigurationEmbedded: Type<ChatbotConfigurationEmbedded> = new Type<ChatbotConfigurationEmbedded>("ChatbotConfigurationEmbedded");
export interface ChatbotConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "ChatbotConfigurationEmbedded";
  openAIAPIKey: string | null;
  anthropicAPIKey: string | null;
  deepSeekAPIKey: string | null;
  geminiAPIKey: string | null;
  grokAPIKey: string | null;
  mistralAPIKey: string | null;
}

export const ChatbotLanguageModelEntity: Type<ChatbotLanguageModelEntity> = new Type<ChatbotLanguageModelEntity>("ChatbotLanguageModel");
export interface ChatbotLanguageModelEntity extends Entities.Entity {
  Type: "ChatbotLanguageModel";
  provider: ChatbotProviderSymbol;
  model: string;
  temperature: number | null;
  maxTokens: number | null;
  isDefault: boolean;
}

export namespace ChatbotLanguageModelOperation {
  export const Save : Operations.ExecuteSymbol<ChatbotLanguageModelEntity> = registerSymbol("Operation", "ChatbotLanguageModelOperation.Save");
  export const MakeDefault : Operations.ExecuteSymbol<ChatbotLanguageModelEntity> = registerSymbol("Operation", "ChatbotLanguageModelOperation.MakeDefault");
  export const Delete : Operations.DeleteSymbol<ChatbotLanguageModelEntity> = registerSymbol("Operation", "ChatbotLanguageModelOperation.Delete");
}

export namespace ChatbotMessage {
  export const OpenSession: MessageKey = new MessageKey("ChatbotMessage", "OpenSession");
  export const NewSession: MessageKey = new MessageKey("ChatbotMessage", "NewSession");
  export const Send: MessageKey = new MessageKey("ChatbotMessage", "Send");
  export const TypeAMessage: MessageKey = new MessageKey("ChatbotMessage", "TypeAMessage");
  export const UsingInternalTool: MessageKey = new MessageKey("ChatbotMessage", "UsingInternalTool");
  export const ReceivingInstructions: MessageKey = new MessageKey("ChatbotMessage", "ReceivingInstructions");
}

export namespace ChatbotProviders {
  export const OpenAI : ChatbotProviderSymbol = registerSymbol("ChatbotProvider", "ChatbotProviders.OpenAI");
  export const Mistral : ChatbotProviderSymbol = registerSymbol("ChatbotProvider", "ChatbotProviders.Mistral");
  export const Anthropic : ChatbotProviderSymbol = registerSymbol("ChatbotProvider", "ChatbotProviders.Anthropic");
  export const DeepSeek : ChatbotProviderSymbol = registerSymbol("ChatbotProvider", "ChatbotProviders.DeepSeek");
  export const Grok : ChatbotProviderSymbol = registerSymbol("ChatbotProvider", "ChatbotProviders.Grok");
}

export const ChatbotProviderSymbol: Type<ChatbotProviderSymbol> = new Type<ChatbotProviderSymbol>("ChatbotProvider");
export interface ChatbotProviderSymbol extends Basics.Symbol {
  Type: "ChatbotProvider";
}

export const ChatMessageEntity: Type<ChatMessageEntity> = new Type<ChatMessageEntity>("ChatMessage");
export interface ChatMessageEntity extends Entities.Entity {
  Type: "ChatMessage";
  chatSession: Entities.Lite<ChatSessionEntity>;
  creationDate: string /*DateTime*/;
  role: ChatMessageRole;
  isToolCall: boolean;
  message: string;
  toolID: string | null;
}

export namespace ChatMessageOperation {
  export const Save : Operations.ExecuteSymbol<ChatMessageEntity> = registerSymbol("Operation", "ChatMessageOperation.Save");
  export const Delete : Operations.DeleteSymbol<ChatMessageEntity> = registerSymbol("Operation", "ChatMessageOperation.Delete");
}

export const ChatMessageRole: EnumType<ChatMessageRole> = new EnumType<ChatMessageRole>("ChatMessageRole");
export type ChatMessageRole =
  "System" |
  "User" |
  "Assistant" |
  "Tool";

export const ChatSessionEntity: Type<ChatSessionEntity> = new Type<ChatSessionEntity>("ChatSession");
export interface ChatSessionEntity extends Entities.Entity {
  Type: "ChatSession";
  title: string | null;
  languageModel: Entities.Lite<ChatbotLanguageModelEntity>;
  user: Entities.Lite<Authorization.UserEntity>;
  startDate: string /*DateTime*/;
}

export namespace ChatSessionOperation {
  export const Delete : Operations.DeleteSymbol<ChatSessionEntity> = registerSymbol("Operation", "ChatSessionOperation.Delete");
}

export namespace DefaultAgent {
  export const Introduction : ChatbotAgentCodeSymbol = registerSymbol("ChatbotAgentCode", "DefaultAgent.Introduction");
  export const QuestionSumarizer : ChatbotAgentCodeSymbol = registerSymbol("ChatbotAgentCode", "DefaultAgent.QuestionSumarizer");
  export const SearchControl : ChatbotAgentCodeSymbol = registerSymbol("ChatbotAgentCode", "DefaultAgent.SearchControl");
}

