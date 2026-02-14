import Markdown from 'react-markdown';
import { Link } from 'react-router'
import { FontAwesomeIcon } from '@framework/Lines';
import remarkGfm from 'remark-gfm'


export default function ChatMarkdown(p: { content: string }){

  return <Markdown remarkPlugins={[remarkGfm]} components={{ a: renderLink, table: renderTable }}>{p.content}</Markdown>;
}

  function renderTable({ node, children, ...props }: React.PropsWithChildren<React.TableHTMLAttributes<HTMLTableElement>> & { node?: any }): React.ReactNode {
    return <table className="table table-sm table-bordered" {...props}>{children}</table>;
  }

  export function renderLink({ node, href, children, ...props }: React.PropsWithChildren<React.AnchorHTMLAttributes<HTMLAnchorElement>> & { node?: any }): React.ReactNode {
    if (href && href.startsWith("/")) 
      return <Link to={href}>{children}</Link>;
    
    return (
      <a href={href} {...props}>
        {children} <FontAwesomeIcon icon="external-link" />
      </a>
    );
  } 
