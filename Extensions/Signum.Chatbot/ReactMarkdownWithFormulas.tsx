import React from 'react';
import ReactMarkdown from "react-markdown";
import rehypeKatex from 'rehype-katex';
import remarkMath from 'remark-math';
import remarkGfm from 'remark-gfm'
import 'katex/dist/katex.min.css';
import { ChatbotClient } from './ChatbotClient';

const remarkPlugins = [remarkGfm as any, remarkMath as any];
const rehypePlugins = [rehypeKatex as any];

export default function ReactMarkdownWithFormulas(p: { children: string | null | undefined }): React.JSX.Element {
  return <ReactMarkdown remarkPlugins={remarkPlugins}
    rehypePlugins={rehypePlugins}
    children={p.children!}
    components={{ a: ChatbotClient.renderLink }} />
}

