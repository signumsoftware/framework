import {
  $applyNodeReplacement,
  DecoratorNode,
  DOMConversionMap,
  DOMConversionOutput,
  DOMExportOutput,
  LexicalNode,
  NodeKey,
  SerializedLexicalNode,
  Spread,
} from 'lexical';
import * as React from 'react';

export type SerializedMentionNode = Spread<{
  mentionKey: string;
  mentionToStr: string;
  mentionTrigger: string;
}, SerializedLexicalNode>;

export class MentionNode extends DecoratorNode<React.JSX.Element> {
  __mentionKey: string;
  __mentionToStr: string;
  __mentionTrigger: string;

  static override getType(): string {
    return 'mention';
  }

  static override clone(node: MentionNode): MentionNode {
    return new MentionNode(node.__mentionKey, node.__mentionToStr, node.__mentionTrigger, node.__key);
  }

  constructor(mentionKey: string, mentionToStr: string, mentionTrigger: string, key?: NodeKey) {
    super(key);
    this.__mentionKey = mentionKey;
    this.__mentionToStr = mentionToStr;
    this.__mentionTrigger = mentionTrigger;
  }

  override createDOM(): HTMLElement {
    const span = document.createElement('span');
    span.className = 'mention';
    return span;
  }

  override updateDOM(): boolean {
    return false;
  }

  override exportDOM(): DOMExportOutput {
    const span = document.createElement('span');
    span.className = 'mention';
    span.setAttribute('data-mention-key', this.__mentionKey);
    span.setAttribute('data-mention-trigger', this.__mentionTrigger);
    span.setAttribute('contenteditable', 'false');
    span.textContent = this.__mentionTrigger + this.__mentionToStr;
    return { element: span };
  }

  static override importDOM(): DOMConversionMap {
    return {
      span: (domNode: HTMLElement) => {
        if (!domNode.hasAttribute('data-mention-key')) return null;
        return {
          priority: 1 as const,
          conversion: (element: HTMLElement): DOMConversionOutput => {
            const trigger = element.getAttribute('data-mention-trigger') ?? '@';
            const raw = element.textContent ?? '';
            const text = raw.startsWith(trigger) ? raw.slice(trigger.length) : raw;
            return {
              node: new MentionNode(
                element.getAttribute('data-mention-key')!,
                text,
                trigger,
              ),
            };
          },
        };
      },
    };
  }

  override exportJSON(): SerializedMentionNode {
    return {
      ...super.exportJSON(),
      type: 'mention',
      mentionKey: this.__mentionKey,
      mentionToStr: this.__mentionToStr,
      mentionTrigger: this.__mentionTrigger,
      version: 1,
    };
  }

  static override importJSON(serialized: SerializedMentionNode): MentionNode {
    return new MentionNode(
      serialized.mentionKey,
      serialized.mentionToStr,
      serialized.mentionTrigger ?? '@',
    );
  }

  override decorate(): React.JSX.Element {
    return (
      <span className="mention" data-mention-key={this.__mentionKey}>
        {this.__mentionTrigger}{this.__mentionToStr}
      </span>
    );
  }

  override isInline(): boolean {
    return true;
  }

  override isIsolated(): boolean {
    return false;
  }
}

export function $createMentionNode(mentionKey: string, mentionToStr: string, mentionTrigger: string): MentionNode {
  return $applyNodeReplacement(new MentionNode(mentionKey, mentionToStr, mentionTrigger));
}

export function $isMentionNode(node: LexicalNode | null | undefined): node is MentionNode {
  return node instanceof MentionNode;
}
