import * as React from 'react'
import { Tab, Tabs }from 'react-bootstrap'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, EntityComponent, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityFrame, EntityTabRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll }  from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EmailTemplateEntity, EmailTemplateContactEntity, EmailTemplateRecipientEntity, EmailTemplateMessageEntity, EmailTemplateViewMessage } from '../Signum.Entities.Mailing'
import { TemplateTokenMessage } from '../Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../TemplateControls'


export default class EmailTemplate extends EntityComponent<EmailTemplateEntity> {

    renderEntity() {

        var e = this.props.ctx;
        
        var ec = e.subCtx({labelColumns: { sm: 3 }});
        var sc = e.subCtx({formGroupStyle: FormGroupStyle.Basic});
     

        return (
            <div>
                <div className="row">
                    <div className="col-sm-8">
                        <ValueLine ctx={ec.subCtx(e => e.name)}  />
                        <EntityCombo ctx={ec.subCtx(e => e.systemEmail)}  />
                        <EntityLine ctx={ec.subCtx(e => e.query)} onChange={()=>this.forceUpdate()} />
                        <div className="row">
                            <div className="col-sm-4">
                                <ValueLine ctx={ec.subCtx(e => e.editableMessage)} inlineCheckbox={true} />
                            </div>
                            <div className="col-sm-4">
                                <ValueLine ctx={ec.subCtx(e => e.disableAuthorization)} inlineCheckbox={true} />
                            </div>
                            <div className="col-sm-4">
                                <ValueLine ctx={ec.subCtx(e => e.sendDifferentMessages)} inlineCheckbox={true} />
                            </div>
                        </div>
                    </div>
                    { ec.value.isNew && 
                        <div className="col-sm-4 form-vertical" style={{ marginTop:"-12px"}}>
                            <fieldset>
                                <legend>Active</legend>
                                <ValueLine ctx={sc.subCtx(e => e.active)} inlineCheckbox={true} />
                                <ValueLine ctx={sc.subCtx(e => e.startDate)}  />
                                <ValueLine ctx={sc.subCtx(e => e.endDate)}  />
                            </fieldset>
                        </div>
                    }
                    { ec.value.query && this.renderQueryPart() }
                </div>
            </div>
        );
    }

    renderQueryPart(){   
        var ec = this.props.ctx.subCtx({labelColumns: { sm: 2 }});

        return ( 
            <div>
                <EntityDetail ctx={ec.subCtx(e => e.from)} getComponent={this.renderContact}/>
                <div className="repeater-inline">
                    <EntityRepeater ctx={ec.subCtx(e => e.recipients)} getComponent={this.renderRecipient} />
                </div>
                <EntityList ctx={ec.subCtx(e => e.attachments)}  />
                <EntityLine ctx={ec.subCtx(e => e.masterTemplate)}  />
                <ValueLine ctx={ec.subCtx(e => e.isBodyHtml)}  />

                <div className="sf-email-replacements-container">
                    <EntityTabRepeater ctx={ec.subCtx(a=>a.messages)} getComponent={(ctx : TypeContext<EmailTemplateMessageEntity>)=>
                        <EmailTemplateMessage ctx={ctx} queryKey={ec.value.query && ec.value.query.key}/> } />
                </div>
            </div>
        );
    }

    renderContact = (ec: TypeContext<EmailTemplateContactEntity>) => {

        var sc = ec.subCtx({ labelColumns: { sm: 4 }});

        return (
            <div>
                <div className="row">
                    <div className="col-sm-2" style={{ textAlign: "right", padding: "8px" }}>
                        <label>{ EmailTemplateEntity.nicePropertyName(a=>a.from)} </label>
                    </div>
                    <div className="col-sm-5">
                           <ValueLine ctx={sc.subCtx(c => c.emailAddress)}  />
                    </div>
                    <div className="col-sm-5">
                          <ValueLine ctx={sc.subCtx(c => c.displayName)}  />
                    </div>
                </div>
                { this.entity.query && 
                   <QueryTokenEntityBuilder
                    ctx={ec.subCtx(a => a.token, { formGroupStyle: FormGroupStyle.None }) }
                    queryKey={this.entity.query.key}
                    subTokenOptions={ SubTokensOptions.CanElement} /> 
                }
            </div>
        );
    };

    renderRecipient = (ec: TypeContext<EmailTemplateRecipientEntity>) => {

        var sc = ec.subCtx({ labelColumns: { sm: 4 }});

        return (
            <div>
                <div className="row">
                    <div className="col-sm-2" style={{ textAlign: "right"}}>
                        <label>
                            <ValueLine ctx={sc.subCtx(c => c.kind)} formGroupStyle={FormGroupStyle.None} />
                        </label>
                    </div>
                    <div className="col-sm-5">
                        <ValueLine ctx={sc.subCtx(c => c.emailAddress)}  />
                    </div>
                    <div className="col-sm-5">
                        <ValueLine ctx={sc.subCtx(c => c.displayName)}  />
                    </div>
                </div>
                { this.entity.query && 
                   <QueryTokenEntityBuilder
                    ctx={ec.subCtx(a => a.token, { formGroupStyle: FormGroupStyle.None }) }
                    queryKey={this.entity.query.key}
                    subTokenOptions={ SubTokensOptions.CanElement} /> 
                }
            </div>
        );
    };


    renderMessage = (c: TypeContext<EmailTemplateMessageEntity>) => {
     
    }

    onInserToken = (newToken: string)=>{


    };
}

export class EmailTemplateMessage extends React.Component<{ ctx: TypeContext<EmailTemplateMessageEntity>, queryKey: string }, {currentControl: "Subject" | "Text"}>{

    render(){
        var ec = this.props.ctx.subCtx({ labelColumns: { sm: 1 }});
        return (
            <div className="sf-email-template-message">
                <EntityCombo ctx={ec.subCtx(e => e.cultureInfo)} labelText={EmailTemplateViewMessage.Language.niceToString()} />
                <TemplateControls queryKey={this.props.queryKey}  onInsert={this.onInsert} forHtml={true} /> 
                <ValueLine ctx={ec.subCtx(e => e.subject)} formGroupStyle={FormGroupStyle.None} placeholderLabels={true} labelHtmlProps={{width: "100px"}} valueHtmlProps={{className: "sf-email-inserttoken-target sf-email-template-message-subject form-control"}}  />
                <ValueLine ctx={ec.subCtx(e => e.text)}  formGroupStyle={FormGroupStyle.None} valueLineType={ValueLineType.TextArea} labelHtmlProps={{width: "100px", height:"180px"}} valueHtmlProps={{className: "sf-rich-text-editor sf-email-template-message-text"}} />
            </div>
        );
    }

    onInsert = (message: string)=> {


    }
}
