import * as React from "react";
import PopperJS, {
    Placement,
    Data,
    Modifiers,
    ReferenceObject,
} from 'popper.js';

export const ManagerContext = React.createContext<ManagerContextType>({ getReferenceRef: undefined, referenceNode: undefined });

export interface ManagerContextType {
    getReferenceRef?: (elem: HTMLElement | null) => void;
    referenceNode?: HTMLElement;
}


export class Manager extends React.Component<{}, { context: ManagerContextType;}> {
    constructor(props: {}) {
        super(props);
        this.state = {
            context: {
                getReferenceRef: this.getReferenceRef,
                referenceNode: undefined,
            },
        };
    }

    getReferenceRef = (referenceNode: HTMLElement | null) =>
        this.setState(({ context }) => ({
            context: { getReferenceRef: context.getReferenceRef, referenceNode } as ManagerContextType,
        }));

    render() {
        return (
            <ManagerContext.Provider value={this.state.context}>
                {this.props.children}
            </ManagerContext.Provider>
        );
    }
}


type ReferenceElement = ReferenceObject | HTMLElement | null;
type StyleOffsets = { top: number, left: number };
type StylePosition = { position: 'absolute' | 'fixed' };

export interface PopperArrowProps {
    ref: (elem: HTMLElement | null) => void;
    style: StyleOffsets,
};
export interface PopperChildrenProps {
    ref: (elem: HTMLElement | null) => void,
    style: StyleOffsets & StylePosition;
    placement: Placement;
    outOfBoundaries: boolean | null;
    scheduleUpdate: () => void;
    arrowProps: PopperArrowProps;
};

export interface PopperProps {
    children: (props: PopperChildrenProps) => React.ReactNode;
    eventsEnabled?: boolean;
    innerRef?: (elem: HTMLElement | null) => void;
    modifiers?: Modifiers;
    placement?: Placement;
    positionFixed?: boolean;
    referenceElement?: ReferenceElement;
};

export interface PopperState {
    popperNode?: HTMLElement | null;
    arrowNode?: HTMLElement | null;
    popperInstance?: PopperJS;
    data?: Data;
};

const initialStyle = {
    position: 'absolute',
    top: 0,
    left: 0,
    opacity: 0,
    pointerEvents: 'none',
};

const initialArrowStyle = {};

export class InnerPopper extends React.Component<PopperProps, PopperState> {
    static defaultProps = {
        placement: 'bottom',
        eventsEnabled: true,
        referenceElement: undefined,
        positionFixed: false,
    };

    constructor(props: PopperProps) {
        super(props);
        this.state = {
            popperNode: undefined,
            arrowNode: undefined,
            popperInstance: undefined,
            data: undefined,
        };
    }

    setPopperNode = (popperNode: HTMLElement | null) => {
        this.props.innerRef && this.props.innerRef(popperNode);
        this.setState({ popperNode });
    }

    setArrowNode = (arrowNode: HTMLElement | null) => this.setState({ arrowNode });

    updateStateModifier = {
        enabled: true,
        order: 900,
        fn: (data: PopperJS.Data | undefined) => {
            this.setState({ data });
            return data;
        },
    };

    getOptions = () => ({
        placement: this.props.placement,
        eventsEnabled: this.props.eventsEnabled,
        positionFixed: this.props.positionFixed,
        modifiers: {
            ...this.props.modifiers,
            arrow: {
                enabled: !!this.state.arrowNode,
                element: this.state.arrowNode,
            },
            applyStyle: { enabled: false },
            updateStateModifier: this.updateStateModifier,
        },
    } as PopperJS.PopperOptions);

    getPopperStyle = () => {
        debugger;
        return !this.state.popperNode || !this.state.data ? initialStyle :
            {
                position: this.state.data.offsets.popper.position,
                ...this.state.data.styles,
            }
    };

    getPopperPlacement = () => !this.state.data ? undefined : this.state.data.placement;

    getArrowStyle = () => {
        debugger;
        return !this.state.arrowNode || !this.state.data ? initialArrowStyle : (this.state.data as any).arrowStyles;
    }

    getOutOfBoundariesState = () => this.state.data ? this.state.data.hide : undefined;

    initPopperInstance = () => {
        const { referenceElement } = this.props;
        const { popperNode, popperInstance } = this.state;
        if (referenceElement && popperNode && !popperInstance) {
            const popperInstance = new PopperJS(referenceElement, popperNode, this.getOptions());
            this.setState({ popperInstance });
            return true;
        }
        return false;
    };

    destroyPopperInstance = (callback: () => boolean) => {
        if (this.state.popperInstance) {
            this.state.popperInstance.destroy();
        }
        this.setState({ popperInstance: undefined }, callback);
    };

    updatePopperInstance = () => {
        if (this.state.popperInstance) {
            this.destroyPopperInstance(() => this.initPopperInstance());
        }
    };

    scheduleUpdate = () => {
        if (this.state.popperInstance) {
            this.state.popperInstance.scheduleUpdate();
        }
    };

    componentDidUpdate(prevProps: PopperProps, prevState: PopperState) {
        // If needed, initialize the Popper.js instance
        // it will return `true` if it initialized a new instance, or `false` otherwise
        // if it returns `false`, we make sure Popper props haven't changed, and update
        // the Popper.js instance if needed
        if (!this.initPopperInstance()) {
            // If the Popper.js options have changed, update the instance (destroy + create)
            if (
                this.props.placement !== prevProps.placement ||
                this.props.eventsEnabled !== prevProps.eventsEnabled ||
                this.state.arrowNode !== prevState.arrowNode ||
                this.state.popperNode !== prevState.popperNode ||
                this.props.referenceElement !== prevProps.referenceElement ||
                this.props.positionFixed !== prevProps.positionFixed
            ) {
                this.updatePopperInstance();
            }
        }
    }

    componentWillUnmount() {
        if (this.state.popperInstance) {
            this.state.popperInstance.destroy();
        }
    }

    render() {
        return this.props.children({
            ref: this.setPopperNode,
            style: this.getPopperStyle(),
            placement: this.getPopperPlacement(),
            outOfBoundaries: this.getOutOfBoundariesState(),
            scheduleUpdate: this.scheduleUpdate,
            arrowProps: {
                ref: this.setArrowNode,
                style: this.getArrowStyle(),
            },
        } as PopperChildrenProps);
    }
}

const placements = PopperJS.placements;
export { placements };

export function Popper(props: PopperProps) {
    return (
        <ManagerContext.Consumer>
            {({ referenceNode }) => (
                <InnerPopper referenceElement={referenceNode} {...props} />
            )}
        </ManagerContext.Consumer>
    );
}



interface InnerReferenceProps {
    getReferenceRef?: (element: HTMLElement | null) => void,
}

class InnerReference extends React.Component<ReferenceProps & InnerReferenceProps> {
    refHandler = (node: HTMLElement | null) => {
        this.props.innerRef && this.props.innerRef(node);
        this.props.getReferenceRef && this.props.getReferenceRef(node);
    }

    render() {
        return this.props.children({ ref: this.refHandler });
    }
}

export interface ReferenceChildrenProps {
    ref: (elem: HTMLElement | null) => void
};

export interface ReferenceProps {
    children: (props: ReferenceChildrenProps) => React.ReactNode,
    innerRef?: (element: HTMLElement | null) => void,
}

export function Reference(props: ReferenceProps) {
    return (
        <ManagerContext.Consumer>
            {({ getReferenceRef }) => <InnerReference getReferenceRef={getReferenceRef} {...props} />}
        </ManagerContext.Consumer>
    );
}
