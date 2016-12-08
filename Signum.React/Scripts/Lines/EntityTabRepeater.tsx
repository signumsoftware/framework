import * as React from 'react'
import { Link } from 'react-router'
import { Tab, Tabs } from 'react-bootstrap'
import { classes, Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'
import { RenderEntity } from './RenderEntity'

export interface EntityTabRepeaterProps extends EntityListBaseProps {
	createAsLink?: boolean;
}

export class EntityTabRepeater extends EntityListBase<EntityTabRepeaterProps, EntityTabRepeaterProps> {

	calculateDefaultState(state: EntityTabRepeaterProps) {
		super.calculateDefaultState(state);
		state.viewOnCreate = false;
	}

	renderInternal() {

		const buttons = (
			<span className="pull-right">
				{this.renderCreateButton(false) }
				{this.renderFindButton(false) }
			</span>
		);

		var ctx = this.state.ctx!;

	   const readOnly = this.state.ctx.readOnly;

		return (
			<fieldset className={classes("SF-repeater-field SF-control-container", ctx.errorClass) }
                {...this.baseHtmlProps() } {...this.state.formGroupHtmlProps}>
				<legend>
					<div>
						<span>{this.state.labelText}</span>
						{React.Children.count(buttons) ? buttons : undefined}
					</div>
				</legend>
				<Tabs id={ctx.compose("tabs")}>
					{
						mlistItemContext(ctx).map((mlec, i) =>
							<Tab className="sf-repeater-element" eventKey={i} key={i} {...EntityListBase.entityHtmlProps(mlec.value) }
								title={
									<div>
										{ getToString(mlec.value) }
										&nbsp;
										{ this.state.remove && !readOnly &&
											<span className={classes("sf-line-button", "sf-create") }
											onClick={e => this.handleRemoveElementClick(e, i) }
											title={EntityControlMessage.Remove.niceToString() }>
											<span className="glyphicon glyphicon-remove"/>
											</span>
										}
									</div> as any
								}>
								<RenderEntity ctx={mlec} getComponent={this.props.getComponent}/>
							</Tab>
						)
						
					}
					<Tab eventKey={"x"} disabled></Tab> {/*Temporal hack*/}
				</Tabs>
			</fieldset>
		);
	}
}

