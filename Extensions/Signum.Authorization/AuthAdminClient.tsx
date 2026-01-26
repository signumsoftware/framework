import * as React from 'react'
import { RouteObject } from 'react-router'
import { ModifiableEntity, EntityPack, is, SearchMessage, Lite, getToString, EntityControlMessage, liteKeyLong, Entity, isEntityPack } from '@framework/Signum.Entities';
import { ifError, softCast } from '@framework/Globals';
import { ajaxPost, ajaxGet, ajaxGetRaw, saveFile, ServiceError } from '@framework/Services';
import * as Services from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { tasks, LineBaseProps, LineBaseController } from '@framework/Lines/LineBase'
import { EntityBaseController, FormGroup, TypeContext } from '@framework/Lines'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import { PropertyRouteEntity } from '@framework/Signum.Basics'
import {
  PseudoType, getTypeInfo, OperationInfo, getQueryInfo, GraphExplorer, PropertyRoute, tryGetTypeInfo, getAllTypes, Type,
  QueryTokenString, QueryKey, getQueryKey, getTypeInfos, symbolNiceName, getSymbol, reloadTypesInDomains,
  typeAllowedInDomain, onReloadTypesActions
} from '@framework/Reflection'
import {
  PropertyAllowed, TypeAllowedBasic, AuthAdminMessage, BasicPermission,
  PermissionRulePack, TypeRulePack, OperationRulePack, PropertyRulePack, QueryRulePack, QueryAllowed, TypeConditionSymbol
} from './Rules/Signum.Authorization.Rules'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import ProfilePhoto, { SmallProfilePhoto, urlProviders } from './Templates/ProfilePhoto';
import { TypeaheadOptions } from '@framework/Components/Typeahead';
import { EntityLink, similarToken } from '@framework/Search';
import UserCircle from './Templates/UserCircle';
import { AuthMessage, RoleEntity, UserEntity, UserLiteModel, UserOperation, UserState } from './Signum.Authorization';
import { QueryDescription, SubTokensOptions, getTokenParents, isFilterCondition } from '@framework/FindOptions';
import { similarTokenToStr } from '@framework/FinderRules';
import { CollectionMessage } from '@framework/Signum.External';
import { useAPI } from '@framework/Hooks';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';
import { QuickLinkAction, QuickLinkClient } from '@framework/QuickLinkClient';

export namespace AuthAdminClient {
  
  export let types: boolean;
  export let properties: boolean;
  export let operations: boolean;
  export let queries: boolean | "queryContext";
  export let permissions: boolean;
  
  export function start(options: { routes: RouteObject[], types: boolean; properties: boolean, operations: boolean, queries: boolean | "queryContext"; permissions: boolean }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.Authorization", () => import("./Changelog"));
  
    types = options.types;
    properties = options.properties;
    operations = options.operations;
    queries = options.queries;
    permissions = options.permissions;
  
    AppContext.clearSettingsActions.push(() => urlProviders.clear());
  
    Navigator.addSettings(new EntitySettings(UserEntity, e => import('./Templates/User'), {
      renderLite: (lite, hl) => {
        if (UserLiteModel.isInstance(lite.model))
          return (
            <span className="d-inline-flex align-items-center"><SmallProfilePhoto user={lite} className="me-1" /><span>{hl.highlight(getToString(lite))}</span></span>
          );
  
        if (typeof lite.model == "string")
          return hl.highlight(getToString(lite));
  
        return lite.EntityType;
      }
    }));
  
  
    Navigator.addSettings(new EntitySettings(RoleEntity, e => import('./Templates/Role')));
    Operations.addSettings(new EntityOperationSettings(UserOperation.SetPassword, { isVisible: ctx => false }));
    Operations.addSettings(new EntityOperationSettings(UserOperation.AutoDeactivate, { hideOnCanExecute: true, isVisible: () => false }));
    Operations.addSettings(new EntityOperationSettings(UserOperation.Deactivate, { hideOnCanExecute: true }));
    Operations.addSettings(new EntityOperationSettings(UserOperation.Reactivate, { hideOnCanExecute: true }));

    AppContext.clearSettingsActions.push(() => queryAuditorTokens.clear());
  
    Finder.addSettings({
      queryName: UserEntity,
      defaultFilters: [
        {
          groupOperation: "Or",
          pinned: { label: SearchMessage.Search.niceToString(), splitValue: true, active: "WhenHasValue" },
          filters: [
            { token: "Entity.ToString", operation: "Contains" },
            { token: "Entity.Id", operation: "EqualTo" },
            { token: UserEntity.token(a => a.userName), operation: "Contains" },
          ]
        },
        {
          token: UserEntity.token(a => a.state),
          value: UserState.value("Active"),
          pinned: { label: () => AuthMessage.OnlyActive.niceToString(), column: 1, active: "Checkbox_Checked" },
        },
      ],
      entityFormatter: new Finder.EntityFormatter(({ row, searchControl: sc }) => !row.entity || !Navigator.isViewable(row.entity.EntityType, { isSearch: "main" }) ? undefined : <EntityLink lite={row.entity}
        inSearch="main"
        onNavigated={sc?.handleOnNavigated}
        getViewPromise={sc && (sc.props.getViewPromise ?? sc.props.querySettings?.getViewPromise)}
        inPlaceNavigation={sc?.props.view == "InPlace"} className="sf-line-button sf-view">
        <div title={EntityControlMessage.View.niceToString()} className="d-inline-flex align-items-center">
          <SmallProfilePhoto user={row.entity as Lite<UserEntity>} className="me-1" />
          {EntityBaseController.getViewIcon()}
        </div>
      </EntityLink>)
    });
  
    Finder.addSettings({
      queryName: RoleEntity,
      defaultFilters: [
        {
          groupOperation: "Or",
          pinned: { label: SearchMessage.Search.niceToString(), splitValue: true, active: "WhenHasValue" },
          filters: [
            { token: "Entity.Id", operation: "EqualTo" },
            { token: "Entity.ToString", operation: "Contains" },
          ]
        },
        {
          token: RoleEntity.token(a => a.entity.isTrivialMerge),
          value: false,
          pinned: { active: "NotCheckbox_Unchecked", label: () => AuthMessage.IncludeTrivialMerges.niceToString(), column: 1 }
        }
      ],
      extraButtons: scl => [AppContext.isPermissionAuthorized(BasicPermission.AdminRules) && {
        order: -1,
        button: <button type="button" className="btn btn-info"
          onClick={e => { API.downloadAuthRules(); }}>
          <FontAwesomeIcon aria-hidden={true} icon="download" /> {AuthAdminMessage.DownloadAuthRules.niceToString()}
        </button>
      }]
    });
  
    if (options.properties) {
      tasks.push(taskAuthorizeProperties);
      GraphExplorer.TypesLazilyCreated.push(PropertyRouteEntity.typeName);
      Navigator.addSettings(new EntitySettings(PropertyRulePack, e => import('./Rules/PropertyRulePackControl')));
    }
  
    if (options.types) {
      Navigator.isCreableEvent.push(navigatorIsCreable);
      Navigator.isReadonlyEvent.push(navigatorIsReadOnly);
      Navigator.isViewableEvent.push(navigatorIsViewable);
      Operations.Options.maybeReadonly = ti => ti.maxTypeAllowed == "Write" && ti.minTypeAllowed != "Write";
      Navigator.addSettings(new EntitySettings(TypeRulePack, e => import('./Rules/TypeRulePackControl')));

      QuickLinkClient.registerQuickLink(RoleEntity, new  QuickLinkAction("types", () => AuthAdminMessage.TypeRules.niceToString(), (ctx, e) => API.fetchTypeRulePack(ctx.lite.id!)
            .then(pack => Navigator.view(pack, { buttons: "close", readOnly: ctx.widgetContext?.ctx.value.isTrivialMerge == true ? true : undefined })), {
        isVisible: AppContext.isPermissionAuthorized(BasicPermission.AdminRules), icon: "shield-halved", iconColor: "red", color: "danger", group: null
      }));

      const fixTypes = () => getAllTypes().forEach(t => {
        if (t.kind == "Entity") {
          if ((t as any).typeAllowed) {
            t.minTypeAllowed = (t as any).typeAllowed;
            t.maxTypeAllowed = (t as any).typeAllowed;
            delete (t as any).typeAllowed;
          }

          Object.values(t.members).forEach(m => {

            if (!m.minPropertyAllowed) {
              if ((m as any).propertyAllowed) {
                m.minPropertyAllowed = (m as any).propertyAllowed;
                m.maxPropertyAllowed = (m as any).propertyAllowed;
                delete (t as any).typeAllowed;
              } else {
                m.minPropertyAllowed = t.minTypeAllowed;
                m.maxPropertyAllowed = t.maxTypeAllowed;
              }
            }
          });
        }
      });

      fixTypes();
      onReloadTypesActions.push(() => fixTypes());
  
      getAllTypes().filter(a => a.queryAuditors != null)
        .forEach(t => {
          Finder.getOrAddSettings(t).noResultMessage = sc => {
  
            var fo = sc.state.resultFindOptions!;
  
            var tokens = queryAuditorTokens.filter(a => fo.queryKey == a.queryKey && t.queryAuditors.contains(a.typeCondition.key));
  
            var type = getTypeInfos(sc.props.queryDescription.columns["Entity"].type).map(ti => <strong>{ti.nicePluralName}</strong>).joinCommaHtml(CollectionMessage.Or.niceToString());
            
            if (tokens.length == 0) {
              if (!fo.filterOptions.some(f => isFilterCondition(f) && f.operation == "EqualTo")) {
                var symbols = t.queryAuditors.map(a => <strong>{a}</strong>).joinCommaHtml(CollectionMessage.And.niceToString());
                return (
                  <span className="text-warning">
                    <FontAwesomeIcon icon="hand" /> {SearchMessage.NoResultsFoundBecauseTheRule0DoesNotAllowedToExplore1WithoutFilteringFirst.niceToString().formatHtml(symbols, type)}
                  </span>
                );
    }
  
              return undefined;
            } else {
              if (!fo.filterOptions.some(f => isFilterCondition(f) && f.operation == "EqualTo" && tokens.some(t => similarToken(f.token?.fullKey, t.token)))) {
                var tokenCode = tokens.map(a => <strong><QuerytokenRenderer token={a.token} queryKey={fo.queryKey} /></strong>).joinCommaHtml(CollectionMessage.Or.niceToString());
                return (
                  <span className="text-warning">
                    <FontAwesomeIcon icon="hand" /> {SearchMessage.NoResultsFoundBecauseYouAreNotAllowedToExplore0WithoutFilteringBy1First.niceToString().formatHtml(type, tokenCode)}
                  </span>
                );
              }
              return undefined;
            }
          }
        });
    }
  
    if (options.operations) {
      Navigator.addSettings(new EntitySettings(OperationRulePack, e => import('./Rules/OperationRulePackControl')));
    }
  
    if (options.queries) {
      Finder.isFindableEvent.push(queryIsFindable);
  
      Navigator.addSettings(new EntitySettings(QueryRulePack, e => import('./Rules/QueryRulePackControl')));

      if (options.queries == "queryContext")
        reloadTypesInDomains(); //fire and forget
    }
  
    if (options.permissions) {
  
      Navigator.addSettings(new EntitySettings(PermissionRulePack, e => import('./Rules/PermissionRulePackControl')));

      QuickLinkClient.registerQuickLink(RoleEntity, new QuickLinkAction("permissions", () => AuthAdminMessage.PermissionRules.niceToString(), (ctx, e) => API.fetchPermissionRulePack(ctx.lite.id!)
        .then(pack => Navigator.view(pack, { buttons: "close", readOnly: ctx.widgetContext?.ctx.value.isTrivialMerge == true ? true : undefined })),
        {
          isVisible: AppContext.isPermissionAuthorized(BasicPermission.AdminRules), icon: "shield-halved", iconColor: "orange", color: "warning", group: null
        }
      ));
    }
  
    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => AppContext.isPermissionAuthorized(BasicPermission.AdminRules),
      key: "DownloadAuthRules",
      onClick: () => { API.downloadAuthRules(); return Promise.resolve(undefined); }
    });

    TypeContext.prototype.isMemberHidden = function () {

      var m = this.propertyRoute?.member;

      if (m == null) {
        return true;
      } 

      if (m.maxPropertyAllowed == "None") {
        throw new Error("Unexpected");
      }

      if (m.minPropertyAllowed != "None") //Allways visible
        return false;

      return this.binding.getIsHidden();
    }

    TypeContext.prototype.isMemberReadOnly = function () {

      var m = this.propertyRoute?.member;

      if (m == null) {
        return true;
      }

      if (m.minPropertyAllowed == "Write") //Always writable
        return false;

      if (m.maxPropertyAllowed == "Read") //Never writable
        return true;

      return this.binding.getIsReadonly();
    }
  }
  
  const queryAuditorTokens: { queryKey: string; token: string; typeCondition: TypeConditionSymbol }[] = []; 
  
  export function registerQueryAuditorToken<T extends Entity>(queryName: Type<T> | QueryKey, token: QueryTokenString<any> | string, typeCondition: TypeConditionSymbol): void {
    queryAuditorTokens.push({ queryKey: getQueryKey(queryName), token: token.toString(), typeCondition: typeCondition });
  }
  
  export function queryIsFindable(queryKey: string, fullScreen: boolean, context?: Lite<Entity>): boolean {
    var allowed = getQueryInfo(queryKey).queryAllowed;
  
    var result = allowed == "Allow" || allowed == "EmbeddedOnly" && !fullScreen;

    if (queries == "queryContext" && context != null)
      return result && typeAllowedInDomain(queryKey, context);

    return result;
  }
  
  export function taskAuthorizeProperties(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps): void {
    if (state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {

      const member = state.ctx.propertyRoute.member;

      if (state.ctx.isMemberReadOnly()) {
        state.ctx.readOnly = true;
      }
    }
  }
  
  export function navigatorIsReadOnly(typeName: PseudoType, entityPack?: EntityPack<ModifiableEntity>, options?: Navigator.IsReadonlyOptions): boolean {

    if (options?.isEmbedded)
      return false;
  
    const ti = tryGetTypeInfo(typeName);
    if (ti == undefined)
      return true;
  
    if (entityPack?.typeAllowed)
      return entityPack.typeAllowed == "None" || entityPack.typeAllowed == "Read";
  
    return ti.maxTypeAllowed == "None" || ti.maxTypeAllowed == "Read";
  }
  
  export function navigatorIsViewable(typeName: PseudoType, entityPack?: EntityPack<ModifiableEntity> | Lite<Entity>, options?: Navigator.IsViewableOptions): boolean {

    if (options?.isEmbedded)
      return true;

    const ti = tryGetTypeInfo(typeName);

    if (ti == undefined)
      return false;

    if (isEntityPack(entityPack) && entityPack?.typeAllowed)
      return entityPack.typeAllowed != "None";

    return ti.maxTypeAllowed != "None";
  }
  
  export function navigatorIsCreable(typeName: PseudoType, options?: Navigator.IsCreableOptions): boolean {
  
    if (options?.isEmbedded)
      return true;
  
    const ti = tryGetTypeInfo(typeName);
  
    return ti != null && ti.maxTypeAllowed == "Write";
  }
  
  export namespace API {
  
    export function fetchPermissionRulePack(roleId: number | string): Promise<PermissionRulePack> {
      return ajaxGet({ url: "/api/authAdmin/permissionRules/" + roleId, cache: "no-cache" });
    }
  
    export function savePermissionRulePack(rules: PermissionRulePack): Promise<void> {
      return ajaxPost({ url: "/api/authAdmin/permissionRules" }, rules);
    }
  
  
    export function fetchTypeRulePack(roleId: number | string): Promise<TypeRulePack> {
      return ajaxGet({ url: "/api/authAdmin/typeRules/" + roleId, cache: "no-cache" });
    }
  
    export function saveTypeRulePack(rules: TypeRulePack): Promise<void> {
      return ajaxPost({ url: "/api/authAdmin/typeRules" }, rules);
    }
  
  
    export function fetchPropertyRulePack(typeName: string, roleId: number | string): Promise<PropertyRulePack> {
      return ajaxGet({ url: "/api/authAdmin/propertyRules/" + typeName + "/" + roleId, cache: "no-cache" });
    }
  
    export function savePropertyRulePack(rules: PropertyRulePack): Promise<void> {
      return ajaxPost({ url: "/api/authAdmin/propertyRules" }, rules);
    }
  
  
  
    export function fetchOperationRulePack(typeName: string, roleId: number | string): Promise<OperationRulePack> {
      return ajaxGet({ url: "/api/authAdmin/operationRules/" + typeName + "/" + roleId, cache: "no-cache" });
    }
  
    export function saveOperationRulePack(rules: OperationRulePack): Promise<void> {
      return ajaxPost({ url: "/api/authAdmin/operationRules" }, rules);
    }
  
  
  
    export function fetchQueryRulePack(typeName: string, roleId: number | string): Promise<QueryRulePack> {
      return ajaxGet({ url: "/api/authAdmin/queryRules/" + typeName + "/" + roleId, cache: "no-cache" });
    }
  
    export function saveQueryRulePack(rules: QueryRulePack): Promise<void> {
      return ajaxPost({ url: "/api/authAdmin/queryRules" }, rules);
    }
  
  
  
    export function downloadAuthRules(): void {
      ajaxGetRaw({ url: "/api/authAdmin/downloadAuthRules" })
        .then(response => saveFile(response));
    }
  
    export function trivialMergeRole(rule: Lite<RoleEntity>[]): Promise<Lite<RoleEntity>> {
      return ajaxPost({ url: "/api/authAdmin/trivialMergeRole" }, rule);
    }
  }

  
  export function QuerytokenRenderer(p: { queryKey: string, token: string, subTokenOptions?: SubTokensOptions }): React.ReactElement<any, string | React.JSXElementConstructor<any>> {
    var token = useAPI(() => Finder.parseSingleToken(p.queryKey, p.token, p.subTokenOptions ?? (SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll)), [p.queryKey, p.token, p.subTokenOptions]);
  
    return getTokenParents(token).map(a => <strong>[{a.niceName}]</strong>).joinCommaHtml(".");
  }
}


declare module '@framework/Reflection' {

  export interface TypeInfo {
    minTypeAllowed: TypeAllowedBasic;
    maxTypeAllowed: TypeAllowedBasic;
    queryAuditors: string[];
    queryAllowed: QueryAllowed;
  }

  export interface MemberInfo {
    minPropertyAllowed: PropertyAllowed;
    maxPropertyAllowed: PropertyAllowed;
    queryAllowed: QueryAllowed;
  }
}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    typeAllowed?: TypeAllowedBasic;
  }

  
}

declare module '@framework/TypeContext' {
  export interface TypeContext<T> {
    isMemberReadOnly(): boolean;
    isMemberHidden(): boolean;
  }
}
