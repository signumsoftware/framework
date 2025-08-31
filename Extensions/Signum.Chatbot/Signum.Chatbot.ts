//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'

export interface ToolCallEmbedded {
  _response?: ChatMessageEntity
}

export const ChatbotConfigurationEmbedded: Type<ChatbotConfigurationEmbedded> = new Type<ChatbotConfigurationEmbedded>("ChatbotConfigurationEmbedded");
export interface ChatbotConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "ChatbotConfigurationEmbedded";
  openAIAPIKey: string | null;
  anthropicAPIKey: string | null;
  geminiAPIKey: string | null;
  mistralAPIKey: string | null;
  githubModelsToken: string | null;
  ollamaUrl: string | null;
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
  export const InitialInstruction: MessageKey = new MessageKey("ChatbotMessage", "InitialInstruction");
}

export namespace ChatbotPermission {
  export const UseChatbot : Basics.PermissionSymbol = registerSymbol("Permission", "ChatbotPermission.UseChatbot");
}

export namespace ChatbotProviders {
  export const OpenAI : ChatbotProviderSymbol = registerSymbol("ChatbotProvider", "ChatbotProviders.OpenAI");
  export const Gemini : ChatbotProviderSymbol = registerSymbol("ChatbotProvider", "ChatbotProviders.Gemini");
  export const Anthropic : ChatbotProviderSymbol = registerSymbol("ChatbotProvider", "ChatbotProviders.Anthropic");
  export const Mistral : ChatbotProviderSymbol = registerSymbol("ChatbotProvider", "ChatbotProviders.Mistral");
  export const GithubModels : ChatbotProviderSymbol = registerSymbol("ChatbotProvider", "ChatbotProviders.GithubModels");
  export const Ollama : ChatbotProviderSymbol = registerSymbol("ChatbotProvider", "ChatbotProviders.Ollama");
}

export const ChatbotProviderSymbol: Type<ChatbotProviderSymbol> = new Type<ChatbotProviderSymbol>("ChatbotProvider");
export interface ChatbotProviderSymbol extends Basics.Symbol {
  Type: "ChatbotProvider";
}

export const ChatbotUICommand: EnumType<ChatbotUICommand> = new EnumType<ChatbotUICommand>("ChatbotUICommand");
export type ChatbotUICommand =
  "System" |
  "SessionId" |
  "SessionTitle" |
  "QuestionId" |
  "AnswerId" |
  "AssistantAnswer" |
  "AssistantTool" |
  "Tool" |
  "Exception";

export const ChatMessageEntity: Type<ChatMessageEntity> = new Type<ChatMessageEntity>("ChatMessage");
export interface ChatMessageEntity extends Entities.Entity {
  Type: "ChatMessage";
  chatSession: Entities.Lite<ChatSessionEntity>;
  creationDate: string /*DateTime*/;
  role: ChatMessageRole;
  content: string | null;
  toolCalls: Entities.MList<ToolCallEmbedded>;
  toolCallID: string | null;
  toolID: string | null;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
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

export const ToolCallEmbedded: Type<ToolCallEmbedded> = new Type<ToolCallEmbedded>("ToolCallEmbedded");
export interface ToolCallEmbedded extends Entities.EmbeddedEntity {
  Type: "ToolCallEmbedded";
  callId: string;
  toolId: string;
  arguments: string;
}

