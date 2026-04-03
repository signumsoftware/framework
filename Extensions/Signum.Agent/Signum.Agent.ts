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

export const AgentSkillCodeEntity: Type<AgentSkillCodeEntity> = new Type<AgentSkillCodeEntity>("AgentSkillCode");
export interface AgentSkillCodeEntity extends Entities.Entity {
  Type: "AgentSkillCode";
  fullClassName: string;
}

export const AgentSkillEntity: Type<AgentSkillEntity> = new Type<AgentSkillEntity>("AgentSkill");
export interface AgentSkillEntity extends Entities.Entity {
  Type: "AgentSkill";
  name: string;
  skillCode: AgentSkillCodeEntity;
  active: boolean;
  useCase: AgentUseCaseSymbol | null;
  shortDescription: string | null;
  instructions: string | null;
  propertyOverrides: Entities.MList<AgentSkillPropertyOverrideEmbedded>;
  subSkills: Entities.MList<AgentSkillSubSkillEmbedded>;
}

export namespace AgentSkillOperation {
  export const Save : Operations.ExecuteSymbol<AgentSkillEntity> = registerSymbol("Operation", "AgentSkillOperation.Save");
  export const Delete : Operations.DeleteSymbol<AgentSkillEntity> = registerSymbol("Operation", "AgentSkillOperation.Delete");
  export const CreateFromUseCase : Operations.ConstructSymbol_From<AgentSkillEntity, AgentUseCaseSymbol> = registerSymbol("Operation", "AgentSkillOperation.CreateFromUseCase");
}

export const AgentSkillPropertyOverrideEmbedded: Type<AgentSkillPropertyOverrideEmbedded> = new Type<AgentSkillPropertyOverrideEmbedded>("AgentSkillPropertyOverrideEmbedded");
export interface AgentSkillPropertyOverrideEmbedded extends Entities.EmbeddedEntity {
  Type: "AgentSkillPropertyOverrideEmbedded";
  propertyName: string;
  value: string | null;
}

export const AgentSkillSubSkillEmbedded: Type<AgentSkillSubSkillEmbedded> = new Type<AgentSkillSubSkillEmbedded>("AgentSkillSubSkillEmbedded");
export interface AgentSkillSubSkillEmbedded extends Entities.EmbeddedEntity {
  Type: "AgentSkillSubSkillEmbedded";
  skill: Entities.Lite<AgentSkillEntity | AgentSkillCodeEntity>;
  activation: SkillActivation;
}

export namespace AgentUseCase {
  export const DefaultChatbot : AgentUseCaseSymbol = registerSymbol("AgentUseCase", "AgentUseCase.DefaultChatbot");
  export const Summarizer : AgentUseCaseSymbol = registerSymbol("AgentUseCase", "AgentUseCase.Summarizer");
}

export const AgentUseCaseSymbol: Type<AgentUseCaseSymbol> = new Type<AgentUseCaseSymbol>("AgentUseCase");
export interface AgentUseCaseSymbol extends Basics.Symbol {
  Type: "AgentUseCase";
}

export const ChatbotConfigurationEmbedded: Type<ChatbotConfigurationEmbedded> = new Type<ChatbotConfigurationEmbedded>("ChatbotConfigurationEmbedded");
export interface ChatbotConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "ChatbotConfigurationEmbedded";
  openAIAPIKey: string | null;
  anthropicAPIKey: string | null;
  geminiAPIKey: string | null;
  mistralAPIKey: string | null;
  githubModelsToken: string | null;
  deepSeekAPIKey: string | null;
  ollamaUrl: string | null;
}

export const ChatbotLanguageModelEntity: Type<ChatbotLanguageModelEntity> = new Type<ChatbotLanguageModelEntity>("ChatbotLanguageModel");
export interface ChatbotLanguageModelEntity extends Entities.Entity {
  Type: "ChatbotLanguageModel";
  provider: LanguageModelProviderSymbol;
  model: string;
  temperature: number | null;
  maxTokens: number | null;
  isDefault: boolean;
  pricePerInputToken: number | null;
  pricePerOutputToken: number | null;
  pricePerCachedInputToken: number | null;
  pricePerReasoningOutputToken: number | null;
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
  export const ShowSystem: MessageKey = new MessageKey("ChatbotMessage", "ShowSystem");
  export const UnableToChangeModelOrProviderOnceUsed: MessageKey = new MessageKey("ChatbotMessage", "UnableToChangeModelOrProviderOnceUsed");
  export const WhatWentWrong: MessageKey = new MessageKey("ChatbotMessage", "WhatWentWrong");
  export const ProvideFeedback: MessageKey = new MessageKey("ChatbotMessage", "ProvideFeedback");
  export const Price: MessageKey = new MessageKey("ChatbotMessage", "Price");
  export const TotalPrice: MessageKey = new MessageKey("ChatbotMessage", "TotalPrice");
  export const AnswerAbovePlease: MessageKey = new MessageKey("ChatbotMessage", "AnswerAbovePlease");
  export const MessageMustBeTheLastToDelete: MessageKey = new MessageKey("ChatbotMessage", "MessageMustBeTheLastToDelete");
  export const SessionInterruptedDoYouWantToRecover: MessageKey = new MessageKey("ChatbotMessage", "SessionInterruptedDoYouWantToRecover");
  export const Recover: MessageKey = new MessageKey("ChatbotMessage", "Recover");
}

export namespace ChatbotPermission {
  export const UseChatbot : Basics.PermissionSymbol = registerSymbol("Permission", "ChatbotPermission.UseChatbot");
}

export const ChatbotUICommand: EnumType<ChatbotUICommand> = new EnumType<ChatbotUICommand>("ChatbotUICommand");
export type ChatbotUICommand =
  "System" |
  "SessionId" |
  "SessionTitle" |
  "QuestionId" |
  "MessageId" |
  "AssistantAnswer" |
  "AssistantTool" |
  "AssistantUITool" |
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
  languageModel: Entities.Lite<ChatbotLanguageModelEntity> | null;
  inputTokens: number | null;
  cachedInputTokens: number | null;
  outputTokens: number | null;
  reasoningOutputTokens: number | null;
  duration: string /*TimeSpan*/ | null;
  userFeedback: UserFeedback | null;
  userFeedbackMessage: string | null;
}

export namespace ChatMessageOperation {
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
  totalInputTokens: number | null;
  totalOutputTokens: number | null;
  totalCachedInputTokens: number | null;
  totalReasoningOutputTokens: number | null;
  totalToolCalls: number;
}

export namespace ChatSessionOperation {
  export const Delete : Operations.DeleteSymbol<ChatSessionEntity> = registerSymbol("Operation", "ChatSessionOperation.Delete");
}

export const EmbeddingsLanguageModelEntity: Type<EmbeddingsLanguageModelEntity> = new Type<EmbeddingsLanguageModelEntity>("EmbeddingsLanguageModel");
export interface EmbeddingsLanguageModelEntity extends Entities.Entity {
  Type: "EmbeddingsLanguageModel";
  provider: LanguageModelProviderSymbol;
  model: string;
  dimensions: number | null;
  isDefault: boolean;
}

export namespace EmbeddingsLanguageModelOperation {
  export const Save : Operations.ExecuteSymbol<EmbeddingsLanguageModelEntity> = registerSymbol("Operation", "EmbeddingsLanguageModelOperation.Save");
  export const MakeDefault : Operations.ExecuteSymbol<EmbeddingsLanguageModelEntity> = registerSymbol("Operation", "EmbeddingsLanguageModelOperation.MakeDefault");
  export const Delete : Operations.DeleteSymbol<EmbeddingsLanguageModelEntity> = registerSymbol("Operation", "EmbeddingsLanguageModelOperation.Delete");
}

export namespace LanguageModelProviders {
  export const OpenAI : LanguageModelProviderSymbol = registerSymbol("LanguageModelProvider", "LanguageModelProviders.OpenAI");
  export const Gemini : LanguageModelProviderSymbol = registerSymbol("LanguageModelProvider", "LanguageModelProviders.Gemini");
  export const Anthropic : LanguageModelProviderSymbol = registerSymbol("LanguageModelProvider", "LanguageModelProviders.Anthropic");
  export const Mistral : LanguageModelProviderSymbol = registerSymbol("LanguageModelProvider", "LanguageModelProviders.Mistral");
  export const GithubModels : LanguageModelProviderSymbol = registerSymbol("LanguageModelProvider", "LanguageModelProviders.GithubModels");
  export const Ollama : LanguageModelProviderSymbol = registerSymbol("LanguageModelProvider", "LanguageModelProviders.Ollama");
  export const DeepSeek : LanguageModelProviderSymbol = registerSymbol("LanguageModelProvider", "LanguageModelProviders.DeepSeek");
}

export const LanguageModelProviderSymbol: Type<LanguageModelProviderSymbol> = new Type<LanguageModelProviderSymbol>("LanguageModelProvider");
export interface LanguageModelProviderSymbol extends Basics.Symbol {
  Type: "LanguageModelProvider";
}

export const SkillActivation: EnumType<SkillActivation> = new EnumType<SkillActivation>("SkillActivation");
export type SkillActivation =
  "Eager" |
  "Lazy";

export const ToolCallEmbedded: Type<ToolCallEmbedded> = new Type<ToolCallEmbedded>("ToolCallEmbedded");
export interface ToolCallEmbedded extends Entities.EmbeddedEntity {
  Type: "ToolCallEmbedded";
  callId: string;
  toolId: string;
  arguments: string;
  isUITool: boolean;
}

export const UserFeedback: EnumType<UserFeedback> = new EnumType<UserFeedback>("UserFeedback");
export type UserFeedback =
  "Positive" |
  "Negative";

