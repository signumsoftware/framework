import * as React from 'react'

interface ErrorBoundaryProps {
  deps?: unknown[];
  children: React.ReactNode;
}

interface ErrorBoundaryState {
  error?: Error;
  info?: React.ErrorInfo;
}

export class ErrorBoundary extends React.Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = {};
  }

  componentDidCatch(error: Error, info: React.ErrorInfo): void {
    this.setState({ error, info });
  }

  UNSAFE_componentWillReceiveProps(newProps: ErrorBoundaryProps): void {
    if (!depsEquals(newProps.deps, this.props.deps) && (this.state.error || this.state.info))
      this.setState({ error: undefined, info: undefined });
  }

  render(): React.ReactElement {
    if (this.state.error || this.state.info) {
      return (
        <div className="alert alert-danger" role="alert">
          <h2>Error in rendering</h2>
          {this.state.error && <pre><code>{this.state.error.stack}</code></pre>}

          <h4>Component Stack</h4>
          {this.state.info && <pre><code>{this.state.info.componentStack}</code></pre>}
        </div>
      );
    }
    return this.props.children as React.ReactElement ?? null;
  }
}

function depsEquals(prev: unknown[] | undefined, next: unknown[] | undefined) {
  if (prev == next)
    return true;

  if (prev === undefined || next === undefined)
    return false;

  if (prev.length !== next.length)
    return false;

  for (var i = 0; i < prev.length; i++) {
    if (prev[i] !== next[i])
      return false;
  }

  return true;
}
