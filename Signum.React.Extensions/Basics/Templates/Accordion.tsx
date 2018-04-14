import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import CollapsableCard, { CollapsableCardProps } from './CollapsableCard';

export interface AccordionProps {
    defaultCardId?: number | string;
}

export interface AccordionState {
    cardId?: number | string;
}

export default class AccordionPanel extends React.Component<AccordionProps, AccordionState> {

    constructor(props: any) {
        super(props);
        this.state = { cardId: this.props.defaultCardId };
    }

    handleSelect = (open: boolean, cardId: string | number) => {
        if (open)
            this.setState({ cardId: undefined });
        else
            this.setState({ cardId });
    }

    render() {
        return (
            <div>
                {
                    React.Children.map(this.props.children, e => {
                        let p = e as React.ReactElement<CollapsableCardProps>;
                        if (p.type != CollapsableCard)
                            throw new Error("Childrens of AccordionPanel should be CollapsableCard");
                        
                        if (p.props.cardId == null)
                            throw new Error("Childrens of AccordionPanel should have the cardId prop set");

                        return React.cloneElement(p,
                            {
                                key: p.props.cardId,
                                isOpen: this.state.cardId == p.props.cardId,
                                toggle: (open: boolean) => this.handleSelect(open, p.props.cardId!)
                            } as Partial<CollapsableCardProps>);
                    })
                }
            </div>
        );
    }
}

