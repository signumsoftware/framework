
import * as React from 'react'
import { Tab, Tabs } from 'react-bootstrap'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import Typeahead  from '../../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'


export class ColorTypeaheadLine extends React.Component<{ ctx: TypeContext<string | null | undefined>; onChange?: () => void }, void>{

    render() {
        var ctx = this.props.ctx;

        return (
            <FormGroup ctx={ctx} labelText={ctx.niceName()} >
                <ColorTypeahead color={ctx.value} onChange={newColor => {
                    ctx.value = newColor;
                    if (this.props.onChange)
                        this.props.onChange();
                    this.forceUpdate();
                }} />
            </FormGroup>
        );
    }
}

export class ColorTypeahead extends React.Component<{ color: string | null | undefined, onChange: (newColor: string | null | undefined) => void }, void>{

    handleGetItems = (query: string) => {
        if (!query)
            return Promise.resolve([
                "black",
                "#00000"
            ]);

        const result = colors
            .filter(k => k.toLowerCase().contains(query.toLowerCase()))
            .orderBy(a => a.length)
            .filter((k, i) => i < 5);

        if (result.length == 0) {
            if (query.match(/^(#[0-9A-F]{3}|#[0-9A-F]{6}|#[0-9A-F]{8})$/i))
                result.push(query);
        }

        return Promise.resolve(result);
    }

    handleSelect = (item: string) => {
        this.props.onChange(item);
        this.forceUpdate();
        return item;
    }

    handleRenderItem = (item: string, query: string) => {

        return (
            <span>
                <span className="icon fa fa-square" style={{ color: item }} />
                {Typeahead.highlightedText(item, query)}
            </span>
        );
    }

    render() {
        return (
            <div style={{ position: "relative" }}>
                <Typeahead
                    value={this.props.color || ""}
                    inputAttrs={{ className: "form-control sf-entity-autocomplete" }}
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


const colors = `AliceBlue
AntiqueWhite
Aqua
Aquamarine
Azure
Beige
Bisque
Black
BlanchedAlmond
Blue
BlueViolet
Brown
BurlyWood
CadetBlue
Chartreuse
Chocolate
Coral
CornflowerBlue
Cornsilk
Crimson
Cyan
DarkBlue
DarkCyan
DarkGoldenRod
DarkGray
DarkGrey
DarkGreen
DarkKhaki
DarkMagenta
DarkOliveGreen
DarkOrange
DarkOrchid
DarkRed
DarkSalmon
DarkSeaGreen
DarkSlateBlue
DarkSlateGray
DarkSlateGrey
DarkTurquoise
DarkViolet
DeepPink
DeepSkyBlue
DimGray
DimGrey
DodgerBlue
FireBrick
FloralWhite
ForestGreen
Fuchsia
Gainsboro
GhostWhite
Gold
GoldenRod
Gray
Grey
Green
GreenYellow
HoneyDew
HotPink
IndianRed
Indigo
Ivory
Khaki
Lavender
LavenderBlush
LawnGreen
LemonChiffon
LightBlue
LightCoral
LightCyan
LightGoldenRodYellow
LightGray
LightGrey
LightGreen
LightPink
LightSalmon
LightSeaGreen
LightSkyBlue
LightSlateGray
LightSlateGrey
LightSteelBlue
LightYellow
Lime
LimeGreen
Linen
Magenta
Maroon
MediumAquaMarine
MediumBlue
MediumOrchid
MediumPurple
MediumSeaGreen
MediumSlateBlue
MediumSpringGreen
MediumTurquoise
MediumVioletRed
MidnightBlue
MintCream
MistyRose
Moccasin
NavajoWhite
Navy
OldLace
Olive
OliveDrab
Orange
OrangeRed
Orchid
PaleGoldenRod
PaleGreen
PaleTurquoise
PaleVioletRed
PapayaWhip
PeachPuff
Peru
Pink
Plum
PowderBlue
Purple
RebeccaPurple
Red
RosyBrown
RoyalBlue
SaddleBrown
Salmon
SandyBrown
SeaGreen
SeaShell
Sienna
Silver
SkyBlue
SlateBlue
SlateGray
SlateGrey
Snow
SpringGreen
SteelBlue
Tan
Teal
Thistle
Tomato
Turquoise
Violet
Wheat
White
WhiteSmoke
Yellow
YellowGreen`.split("\n");