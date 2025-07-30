import * as React from 'react';
import { RouteObject } from 'react-router';
import { OperationSymbol } from '@framework/Signum.Operations';
import { PropertyRoute, PseudoType, QueryKey } from '@framework/Reflection';
import "./Help.css";
import { NamespaceHelpEntity, TypeHelpEntity, AppendixHelpEntity } from './Signum.Help';
import { CultureInfoEntity } from '@framework/Signum.Basics';
import { LineBaseController, LineBaseProps } from '@framework/Lines/LineBase';
export declare namespace HelpClient {
    function start(options: {
        routes: RouteObject[];
    }): void;
    function taskHelpIcon(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps): void;
    function replaceHtmlLinks(txt: string | null): React.JSX.Element | string;
    namespace API {
        function index(): Promise<HelpIndexTS>;
        function namespace(namespace: string): Promise<NamespaceHelp>;
        function saveNamespace(typeHelp: NamespaceHelpEntity): Promise<void>;
        function type(cleanName: string): Promise<TypeHelpEntity>;
        function saveType(typeHelp: TypeHelpEntity): Promise<void>;
        function appendix(uniqueName: string | undefined): Promise<AppendixHelpEntity>;
        function saveAppendix(appendix: AppendixHelpEntity): Promise<void>;
    }
    interface HelpIndexTS {
        culture: CultureInfoEntity;
        namespaces: Array<NamespaceItemTS>;
        appendices: Array<AppendiceItemTS>;
    }
    interface NamespaceItemTS {
        namespace: string;
        module?: string;
        title: string;
        hasEntity?: boolean;
        allowedTypes: EntityItem[];
    }
    interface EntityItem {
        cleanName: string;
        hasEntity?: boolean;
    }
    interface AppendiceItemTS {
        title: string;
        uniqueName: string;
    }
    interface NamespaceHelp {
        namespace: string;
        before?: string;
        title: string;
        description?: string;
        entity: NamespaceHelpEntity;
        allowedTypes: EntityItem[];
    }
    namespace Urls {
        function indexUrl(): string;
        function searchUrl(query: PseudoType): string;
        function typeUrl(typeName: PseudoType): string;
        function namespaceUrl(namespace: string): string;
        function appendixUrl(uniqueName: string | null): string;
        function operationUrl(typeName: PseudoType, operation: OperationSymbol | string): string;
        function idOperation(operation: OperationSymbol | string): string;
        function propertyUrl(typeName: PseudoType, route: PropertyRoute | string): string;
        function idProperty(route: PropertyRoute | string): string;
        function queryUrl(typeName: PseudoType, query: PseudoType | QueryKey): string;
        function idQuery(query: PseudoType | QueryKey): string;
    }
}
declare module '@framework/Signum.Entities' {
    interface EntityPack<T extends ModifiableEntity> {
        typeHelp?: TypeHelpEntity;
    }
}
