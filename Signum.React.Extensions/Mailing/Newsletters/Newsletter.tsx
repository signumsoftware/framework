import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater, EntityDetail} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ExceptionEntity }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { NewsletterEntity } from '../Signum.Entities.Mailing'
import TemplateControls from '../../Templating/TemplateControls'
import { Tabs, Tab } from '../../../../Framework/Signum.React/Scripts/Components/Tabs';

export default class Newsletter extends React.Component<{ ctx: TypeContext<NewsletterEntity> }> {

    render() {

        const nc = this.props.ctx;
        
        return (
            <div>
                        <ValueLine ctx={nc.subCtx(n => n.name)}  />
                        <ValueLine ctx={nc.subCtx(n => n.state)} readOnly={true} />

                        <ValueLine ctx={nc.subCtx(n => n.from)}  />
                        <ValueLine ctx={nc.subCtx(n => n.displayFrom)}  />

                        <EntityLine ctx={nc.subCtx(e => e.query)}  /> 

                        { nc.value.state == "Sent"?  this.renderIFrame(): this.renderEditor() }
                        
                       
                        <ValueLine ctx={nc.subCtx(n => n.subject)}  />
                        <ValueLine ctx={nc.subCtx(n => n.text)} valueLineType="TextArea" valueHtmlAttributes={{ style: {width: "100%", height: "180px"} }} />
            </div>
        );
    }


    renderIFrame(){
        return (
            <fieldset>
                <legend>Message</legend>
                <div className="sf-email-htmlbody">
                    @Html.Raw(nc.Value.Text)
                </div>
            </fieldset>
        );
    }

    renderEditor(){


    }
    
    renderBuilder(){

    }
}

