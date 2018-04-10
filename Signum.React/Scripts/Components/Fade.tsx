import { Transition } from 'react-transition-group';
import * as React from 'react';

const duration = 300;

const defaultStyle = {
    transition: `opacity ${duration}ms ease-in-out`,
    opacity: 0,
}

const transitionStyles: { [state: string]: React.CSSProperties } = {
    entering: { opacity: 0 },
    entered: { opacity: 1 },
};


interface FadeProps {
    in: boolean;
}

export class Fade extends React.Component<FadeProps> {
    render() {
        return (
            <Transition in={this.props.in} timeout={duration}>
                {(state: string) => (
                    <div style={{
                        ...defaultStyle,
                        ...transitionStyles[state]
                    }}>
                        {this.props.children}
                    </div>
                )}
            </Transition>
        );
    }
}