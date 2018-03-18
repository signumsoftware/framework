import * as React from 'react'

interface ErrorBoundaryProps {
}

interface ErrorBoundaryState {
    error?: Error;
    info?: React.ErrorInfo;
}

export class ErrorBoundary extends React.Component<ErrorBoundaryProps, ErrorBoundaryState> {
    constructor(props: ErrorBoundaryProps) {
        super(props);
        this.state = { };
    }

    componentDidCatch(error: Error, info: React.ErrorInfo) {
        this.setState({ error, info });
    }

    render() {
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
        return this.props.children || null;
    }
}