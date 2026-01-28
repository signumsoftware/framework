import * as React from 'react'
import { RouteObject } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ajaxPost, ajaxPostRaw, saveFile } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { Lite, Entity, EntityPack, toLite, ModifiableEntity, toMList, getToString } from '@framework/Signum.Entities'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import { Type, isTypeEntity, QueryTokenString } from '@framework/Reflection'
import { Constructor } from '@framework/Constructor'
import { WordTemplateEntity, WordTemplateOperation, WordModelEntity, WordTemplateVisibleOn } from './Signum.Word'
import { QueryModel, MultiEntityModel } from '../Signum.Templating/Signum.Templating'
import { ButtonBarManager } from '@framework/Frames/ButtonBar';
import * as ContexualItems from '@framework/SearchControl/ContextualItems'
import { ContextualItemsContext, MenuItemBlock, ContextualMenuItem } from "@framework/SearchControl/ContextualItems";
import { ModelEntity } from "@framework/Signum.Entities";
import { QueryRequest } from "@framework/FindOptions";
import WordSearchMenu from "./WordSearchMenu";
import WordEntityMenu from "./WordEntityMenu";
import { ButtonsContext, ButtonBarElement } from "@framework/TypeContext";
import { Dropdown } from 'react-bootstrap';
import { EvalClient } from '../Signum.Eval/EvalClient';
import { SearchControlLoaded } from '@framework/Search';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';

export namespace WordClient {
  
  export function start(options: { routes: RouteObject[], contextual: boolean, queryButton: boolean, entityButton: boolean }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.Word", () => import("./Changelog"));
  
    EvalClient.Options.checkEvalFindOptions.push({ queryName: WordTemplateEntity });
    register(QueryModel, {
      createFromTemplate: wt => Navigator.view(QueryModel.New({ queryKey: wt.query!.key })),
      createFromEntities: (wt, lites) => {
        return Navigator.API.fetch(wt).then(template => QueryModel.New({
          queryKey: template.query!.key,
          filters: [{ token: QueryTokenString.entity().toString(), operation: "IsIn", value: lites }],
          orders: [],
          pagination: { mode: "All" },
        }));
      },
      createFromQuery: (wt, req) => {
        return Promise.resolve(QueryModel.New({
          queryKey: req.queryKey,
          filters: req.filters,
          orders: req.orders,
          pagination: req.pagination,
        }));
      }
    });
  
    if (!Navigator.getSettings(QueryModel))
      Navigator.addSettings(new EntitySettings(QueryModel, e => import('../Signum.Templating/Templates/QueryModel')));
  
    register(MultiEntityModel, {
      createFromTemplate: wt => Finder.findMany({ queryName: wt.query!.key })
        .then(lites => lites && MultiEntityModel.New({ entities: toMList(lites) })),
      createFromEntities: (wt, lites) => Navigator.view(MultiEntityModel.New({ entities: toMList(lites) }))
    });
  
    Navigator.addSettings(new EntitySettings(WordTemplateEntity, e => import('./Templates/WordTemplate')));
  
    Operations.addSettings(new EntityOperationSettings(WordTemplateOperation.CreateWordReport, {
      onClick: ctx => {
  
        var promise: Promise<string | undefined> = ctx.entity.model ? API.getConstructorType(ctx.entity.model) : Promise.resolve(undefined);
        return promise
          .then<Response | undefined>(ct => {
            var template = toLite(ctx.entity);
  
            if (!ct || isTypeEntity(ct))
              return Finder.find({ queryName: ctx.entity.query!.key })
                .then<Response | undefined>(lite => lite && API.createAndDownloadReport({ template, lite }));
            else {
              var s = settings[ct];
              var promise = (s?.createFromTemplate ? s.createFromTemplate(ctx.entity) : Constructor.constructPack(ct).then(a => a && Navigator.view(a)));
              return promise.then<Response | undefined>(entity => entity && API.createAndDownloadReport({ template, entity }));
            }
          })
          .then(response => {
            if (!response)
              return;
  
            return saveFile(response);
          });
      }
    }));
  
    if (options.contextual)
      ContexualItems.onContextualItems.push(getWordTemplates);
  
    if (options.queryButton)
      Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
  
        if (!ctx.searchControl.props.showBarExtension ||
          !(ctx.searchControl.props.showBarExtensionOption?.showWordReport ?? ctx.searchControl.props.largeToolbarButtons) ||
          !Navigator.isViewable(WordTemplateEntity))
          return undefined;
  
        return { button: <WordSearchMenu searchControl={ctx.searchControl} /> };
      });
  
    if (options.entityButton) {
      ButtonBarManager.onButtonBarRender.push(getEntityWordButtons);
    }
  }
  
  export function getEntityWordButtons(ctx: ButtonsContext): Array<ButtonBarElement | undefined> | undefined {
  
    if (Navigator.isViewable(WordTemplateEntity) && ctx.pack.wordTemplates && ctx.pack.wordTemplates.length > 0)
      return [{ button: <WordEntityMenu entityPack={ctx.pack as EntityPack<Entity>} />, order: 1000 }];
  
    return undefined;
  }
  
  export interface WordModelSettings<T> {
    createFromTemplate?: (wt: WordTemplateEntity) => Promise<ModelEntity | undefined>;
    createFromEntities?: (wt: Lite<WordTemplateEntity>, lites: Array<Lite<Entity>>) => Promise<ModelEntity | undefined>;
    createFromQuery?: (wt: Lite<WordTemplateEntity>, req: QueryRequest) => Promise<ModelEntity | undefined>;
  }
  
  export const settings: { [typeName: string]: WordModelSettings<ModifiableEntity> } = {};
  
  export function register<T extends ModifiableEntity>(type: Type<T>, setting: WordModelSettings<T>): void {
    settings[type.typeName] = setting;
  }
  
  export function getWordTemplates(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> | undefined {
  
    if (ctx.container instanceof SearchControlLoaded && ctx.container.state.resultFindOptions?.systemTime)
      return undefined;
  
    if (!Navigator.isViewable(WordTemplateEntity) || ctx.lites.length == 0)
      return undefined;
  
    return API.getWordTemplates(ctx.queryDescription.queryKey, ctx.lites.length > 1 ? "Multiple" : "Single", { lite: (ctx.lites.length == 1 ? ctx.lites[0] : null) })
      .then(wts => {
        if (!wts.length)
          return undefined;

        return {
          header: WordTemplateEntity.nicePluralName(),
          menuItems: wts.map(wt =>
          ({
            fullText: getToString(wt),
            menu: <Dropdown.Item data-operation={wt.EntityType} onClick={() => handleMenuClick(wt, ctx)} >
              <FontAwesomeIcon aria-hidden={true} icon={"file-word"} className="icon" />
              {getToString(wt)}
            </Dropdown.Item >
          } as ContextualMenuItem)
          )
        } as MenuItemBlock;
      });
  }
  
  export function handleMenuClick(wt: Lite<WordTemplateEntity>, ctx: ContextualItemsContext<Entity>): void {
  
    Navigator.API.fetch(wt)
      .then(wordTemplate => wordTemplate.model ? API.getConstructorType(wordTemplate.model) : Promise.resolve(undefined))
      .then(ct => {
        if (!ct || ctx.lites.length == 1 && ctx.lites.single().EntityType == ct)
          return API.createAndDownloadReport({ template: wt, lite: ctx.lites.single() });
  
        var s = settings[ct];
        if (!s)
          throw new Error("No 'WordModelSettings' defined for '" + ct + "'");
  
        if (!s.createFromEntities)
          throw new Error("No 'createFromEntities' defined in the WordModelSettings of '" + ct + "'");
  
        return s.createFromEntities(wt, ctx.lites)
          .then<Response | undefined>(m => m && API.createAndDownloadReport({ template: wt, entity: m }));
      })
      .then(response => response && saveFile(response));
  }
  
  export namespace API {
  
    export interface CreateWordReportRequest {
      template: Lite<WordTemplateEntity>;
      lite?: Lite<Entity>;
      entity?: ModifiableEntity;
    }
  
    export interface GetWordTemplatesRequest {
      lite: Lite<Entity> | null;
    }
  
    export function createAndDownloadReport(request: CreateWordReportRequest): Promise<Response> {
      return ajaxPostRaw({ url: "/api/word/createReport" }, request);
    }
  
    export function getConstructorType(wordModel: WordModelEntity): Promise<string> {
      return ajaxPost({ url: "/api/word/constructorType" }, wordModel);
    }
  
    export function getWordTemplates(queryKey: string, visibleOn: WordTemplateVisibleOn, request: GetWordTemplatesRequest): Promise<Lite<WordTemplateEntity>[]> {
      return ajaxPost({ url: `/api/word/wordTemplates?queryKey=${queryKey}&visibleOn=${visibleOn}` }, request);
    }
  }
}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    wordTemplates?: Array<Lite<WordTemplateEntity>> | "error";
  }
}

declare module '@framework/FindOptions' {

  export interface QueryDescription {
    wordTemplates?: Array<Lite<WordTemplateEntity>> | "error";
  }
}

declare module '@framework/SearchControl/SearchControlLoaded' {

  export interface ShowBarExtensionOption {
    showWordReport?: boolean;
  }
}


