import * as React from 'react'
import { UserQueryEntity, UserQueryEntity_Type, UserQueryMessage } from '../Signum.Entities.UserQueries'
import { FormGroup, FormControlStatic, EntityComponent, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'


const CurrentEntityKey = "[CurrentEntity]";

export default class Role extends EntityComponent<UserQueryEntity> {

    renderEntity() {

        var queryKey = this.entity.query.key;

        return (
            <div>
                <EntityLine ctx={this.subCtx(e => e.owner) } />
                <ValueLine ctx={this.subCtx(e => e.displayName) } />
                <FormGroup ctx={this.subCtx(e => e.query)}>
                    {
                        Finder.isFindable(queryKey) ?
                            <a className="form-control-static" href={Finder.findOptionsPath(queryKey) }>{getQueryNiceName(queryKey) }</a> :
                            <span>{getQueryNiceName(queryKey) }</span>
                    }
                </FormGroup>
                <EntityLine ctx={this.subCtx(e => e.entityType) } />
                {
                    this.entity.entityType &&
                    <p className="messageEntity col-sm-offset-2">
                        {UserQueryMessage.Use0ToFilterCurrentEntity.niceToString(CurrentEntityKey) }
                    </p>
                }
                <ValueLine ctx={this.subCtx(e => e.withoutFilters) } />
                <div className="repeater-inline form-inline sf-filters-list ">
                    <EntityRepeater ctx={this.subCtx(e => e.filters)} />
                </div>
                <div className="repeater-inline form-inline sf-filters-list ">
                    <EntityRepeater ctx={this.subCtx(e => e.columns)} />
                </div>
                <div className="repeater-inline form-inline sf-filters-list ">
                    <EntityRepeater ctx={this.subCtx(e => e.orders)} />
                </div>
                <ValueLine ctx={this.subCtx(e => e.paginationMode)} />
                <ValueLine ctx={this.subCtx(e => e.elementsPerPage)} />
            </div>
        );
    }
}

