import { MessageKey, Type, EnumType } from '../../Signum/React/Reflection';
import * as Entities from '../../Signum/React/Signum.Entities';
import * as Basics from '../../Signum/React/Signum.Basics';
import * as Operations from '../../Signum/React/Signum.Operations';
import * as Authorization from '../Signum.Authorization/Signum.Authorization';
export declare const AgentUseCaseSymbol: Type<AgentUseCaseSymbol>;
export interface AgentUseCaseSymbol extends Basics.Symbol {
    Type: "AgentUseCase";
}
export declare namespace AgentUseCase {
    const DefaultChatbot: AgentUseCaseSymbol;
    const Summarizer: AgentUseCaseSymbol;
}
export declare const AgentSkillEntity: Type<AgentSkillEntity>;
export interface AgentSkillEntity extends Entities.Entity {
    Type: "AgentSkill";
    name: string;
    skillCode: string;
    active: boolean;
    useCase: AgentUseCaseSymbol | null;
    shortDescription: string | null;
    instructions: string | null;
    propertyOverrides: Entities.MList<AgentSkillPropertyOverrideEmbedded>;
    subSkills: Entities.MList<AgentSkillSubSkillEmbedded>;
}
export declare namespace AgentSkillOperation {
    const Save: Operations.ExecuteSymbol<AgentSkillEntity>;
    const Delete: Operations.DeleteSymbol<AgentSkillEntity>;
}
export declare const AgentSkillPropertyOverrideEmbedded: Type<AgentSkillPropertyOverrideEmbedded>;
export interface AgentSkillPropertyOverrideEmbedded extends Entities.EmbeddedEntity {
    Type: "AgentSkillPropertyOverrideEmbedded";
    propertyName: string;
    value: string | null;
}
export declare const AgentSkillSubSkillEmbedded: Type<AgentSkillSubSkillEmbedded>;
export interface AgentSkillSubSkillEmbedded extends Entities.EmbeddedEntity {
    Type: "AgentSkillSubSkillEmbedded";
    skill: Entities.Lite<AgentSkillEntity>;
    activation: SkillActivation;
}
export declare const SkillActivation: EnumType<SkillActivation>;
export type SkillActivation = "Eager" | "Lazy";
export interface SkillPropertyMeta {
    propertyName: string;
    attributeName: string;
    valueHint: string | null;
    propertyType: string;
}
export interface SkillCodeDefaults {
    defaultShortDescription: string;
    defaultInstructions: string;
}
export interface ToolCallEmbedded {
    _response?: ChatMessageEntity;
}
export declare const ChatbotConfigurationEmbedded: Type<ChatbotConfigurationEmbedded>;
export interface ChatbotConfigurationEmbedded extends Entities.EmbeddedEntity {
    Type: "ChatbotConfigurationEmbedded";
    openAIAPIKey: string | null;
    anthropicAPIKey: string | null;
    geminiAPIKey: string | null;
    mistralAPIKey: string | null;
    githubModelsToken: string | null;
    ollamaUrl: string | null;
}
export declare const ChatbotLanguageModelEntity: Type<ChatbotLanguageModelEntity>;
export interface ChatbotLanguageModelEntity extends Entities.Entity {
    Type: "ChatbotLanguageModel";
    provider: LanguageModelProviderSymbol;
    model: string;
    temperature: number | null;
    maxTokens: number | null;
    isDefault: boolean;
}
export declare namespace ChatbotLanguageModelOperation {
    const Save: Operations.ExecuteSymbol<ChatbotLanguageModelEntity>;
    const MakeDefault: Operations.ExecuteSymbol<ChatbotLanguageModelEntity>;
    const Delete: Operations.DeleteSymbol<ChatbotLanguageModelEntity>;
}
export declare namespace ChatbotMessage {
    const OpenSession: MessageKey;
    const NewSession: MessageKey;
    const Send: MessageKey;
    const TypeAMessage: MessageKey;
    const InitialInstruction: MessageKey;
}
export declare namespace ChatbotPermission {
    const UseChatbot: Basics.PermissionSymbol;
}
export declare const ChatbotUICommand: EnumType<ChatbotUICommand>;
export type ChatbotUICommand = "System" | "SessionId" | "SessionTitle" | "QuestionId" | "AnswerId" | "AssistantAnswer" | "AssistantTool" | "Tool" | "Exception";
export declare const ChatMessageEntity: Type<ChatMessageEntity>;
export interface ChatMessageEntity extends Entities.Entity {
    Type: "ChatMessage";
    chatSession: Entities.Lite<ChatSessionEntity>;
    creationDate: string;
    role: ChatMessageRole;
    content: string | null;
    toolCalls: Entities.MList<ToolCallEmbedded>;
    toolCallID: string | null;
    toolID: string | null;
    exception: Entities.Lite<Basics.ExceptionEntity> | null;
}
export declare namespace ChatMessageOperation {
    const Save: Operations.ExecuteSymbol<ChatMessageEntity>;
    const Delete: Operations.DeleteSymbol<ChatMessageEntity>;
}
export declare const ChatMessageRole: EnumType<ChatMessageRole>;
export type ChatMessageRole = "System" | "User" | "Assistant" | "Tool";
export declare const ChatSessionEntity: Type<ChatSessionEntity>;
export interface ChatSessionEntity extends Entities.Entity {
    Type: "ChatSession";
    title: string | null;
    languageModel: Entities.Lite<ChatbotLanguageModelEntity>;
    user: Entities.Lite<Authorization.UserEntity>;
    startDate: string;
}
export declare namespace ChatSessionOperation {
    const Delete: Operations.DeleteSymbol<ChatSessionEntity>;
}
export declare const EmbeddingsLanguageModelEntity: Type<EmbeddingsLanguageModelEntity>;
export interface EmbeddingsLanguageModelEntity extends Entities.Entity {
    Type: "EmbeddingsLanguageModel";
    provider: LanguageModelProviderSymbol;
    model: string;
    dimensions: number | null;
    isDefault: boolean;
}
export declare namespace EmbeddingsLanguageModelOperation {
    const Save: Operations.ExecuteSymbol<EmbeddingsLanguageModelEntity>;
    const MakeDefault: Operations.ExecuteSymbol<EmbeddingsLanguageModelEntity>;
    const Delete: Operations.DeleteSymbol<EmbeddingsLanguageModelEntity>;
}
export declare namespace LanguageModelProviders {
    const OpenAI: LanguageModelProviderSymbol;
    const Gemini: LanguageModelProviderSymbol;
    const Anthropic: LanguageModelProviderSymbol;
    const Mistral: LanguageModelProviderSymbol;
    const GithubModels: LanguageModelProviderSymbol;
    const Ollama: LanguageModelProviderSymbol;
}
export declare const LanguageModelProviderSymbol: Type<LanguageModelProviderSymbol>;
export interface LanguageModelProviderSymbol extends Basics.Symbol {
    Type: "LanguageModelProvider";
}
export declare const ToolCallEmbedded: Type<ToolCallEmbedded>;
export interface ToolCallEmbedded extends Entities.EmbeddedEntity {
    Type: "ToolCallEmbedded";
    callId: string;
    toolId: string;
    arguments: string;
}
//# sourceMappingURL=Signum.Agent.d.ts.map