import * as React from 'react';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
export declare function start(...params: OmniboxProvider<OmniboxResult>[]): void;
export declare const providers: {
    [resultTypeName: string]: OmniboxProvider<OmniboxResult>;
};
export declare function clearProviders(): void;
export declare function registerProvider(prov: OmniboxProvider<OmniboxResult>): void;
export interface HelpOmniboxResult extends OmniboxResult {
    text: string;
    referencedTypeName: string;
}
export declare function renderItem(result: OmniboxResult): React.ReactChild;
export declare function navigateTo(result: OmniboxResult): Promise<string | undefined> | undefined;
export declare function toString(result: OmniboxResult): string;
export declare namespace API {
    function getResults(query: string, signal: AbortSignal): Promise<OmniboxResult[]>;
}
export declare abstract class OmniboxProvider<T extends OmniboxResult> {
    abstract getProviderName(): string;
    abstract renderItem(result: T): React.ReactNode[];
    abstract navigateTo(result: T): Promise<string | undefined> | undefined;
    abstract toString(result: T): string;
    abstract icon(): React.ReactNode;
    renderMatch(match: OmniboxMatch, array: React.ReactNode[]): void;
    coloredSpan(text: string, colorName: string): React.ReactChild;
    coloredIcon(icon: IconProp, color: string): React.ReactChild;
}
export interface OmniboxResult {
    resultTypeName: string;
    distance: number;
}
export interface OmniboxMatch {
    distance: number;
    text: string;
    boldMask: string;
}
//# sourceMappingURL=OmniboxClient.d.ts.map