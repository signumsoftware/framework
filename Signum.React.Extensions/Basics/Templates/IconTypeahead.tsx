
import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '@framework/FindOptions'
import { Typeahead } from '@framework/Components'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { library } from '@fortawesome/fontawesome-svg-core'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { parseIcon } from '../../Dashboard/Admin/Dashboard';

export interface IconTypeaheadLineProps {
    ctx: TypeContext<string | null | undefined>;
    onChange?: () => void;
    extraIcons?: string[];
}

export class IconTypeaheadLine extends React.Component<IconTypeaheadLineProps>{

    handleChange = (newIcon: string | undefined | null) => {
        this.props.ctx.value = newIcon;
        if (this.props.onChange)
            this.props.onChange();
        this.forceUpdate();
    }

    render() {
        var ctx = this.props.ctx;

        return (
            <FormGroup ctx={ctx} labelText={ctx.niceName()} >
                <IconTypeahead icon={ctx.value}
                    extraIcons={this.props.extraIcons}
                    formControlClass={ctx.formControlClass}
                    onChange={this.handleChange} />
            </FormGroup>
        );
    }
}

export interface IconTypeaheadProps {
    icon: string | null | undefined;
    onChange: (newIcon: string | null | undefined) => void;
    extraIcons?: string[];
    formControlClass: string | undefined;
}

export class IconTypeahead extends React.Component<IconTypeaheadProps>{

    icons: string[]; 
    constructor(props: IconTypeaheadProps) {
        super(props);

        var lib = library as any as {
            definitions: {
                [iconPrefix: string]: {
                    [iconName: string]: any;
                }
            }
        };

        var fontAwesome = Dic.getKeys(lib.definitions).flatMap(prefix => Dic.getKeys(lib.definitions[prefix]).map(name => `${prefix} fa-${name}`));
        this.icons = ([] as string[]).concat(props.extraIcons || []).concat(fontAwesome);
    }

    handleGetItems = (query: string) => {
        if (!query)
            return Promise.resolve(([] as string[]).concat(this.props.extraIcons || []).concat(["far fa-", "fas fa-"]));

        const result = this.icons
            .filter(k => k.toLowerCase().contains(query.toLowerCase()))
            .orderBy(a => a.length)
            .filter((k, i) => i < 5);

        return Promise.resolve(result);
    }

    handleSelect = (item: string | unknown) => {
        this.props.onChange(item as string);
        this.forceUpdate();
        return item as string;
    }

    handleRenderItem = (item: unknown, query: string) => {

        var icon = parseIcon(item as string);
        
        return (
            <span>
                {icon && <FontAwesomeIcon icon={icon} className="icon" style={{ width: "12px", height: "12px" }} />}
                {Typeahead.highlightedText(item as string, query)}
            </span>
        );
    }

    render() {
        return (
            <div style={{ position: "relative" }}>
                <Typeahead
                    value={this.props.icon || ""}
                    inputAttrs={{ className: classes(this.props.formControlClass, "sf-entity-autocomplete") }}
                    getItems={this.handleGetItems}
                    onSelect={this.handleSelect}
                    onChange={this.handleSelect}
                    renderItem={this.handleRenderItem}
                    minLength={0}
                    />
            </div>
        );
    }
}