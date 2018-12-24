import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ajaxPost, ajaxPostRaw, saveFile } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, Entity, EntityPack, toLite, ModifiableEntity, toMList } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { Type, isTypeEntity, QueryTokenString } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import * as Constructor from '@framework/Constructor'
import { WordTemplateEntity, WordTemplateOperation, SystemWordTemplateEntity, WordTemplateVisibleOn } from './Signum.Entities.Word'
import { QueryModel, MultiEntityModel } from '../Templating/Signum.Entities.Templating'
import ButtonBar from '@framework/Frames/ButtonBar';
import * as ContexualItems from '@framework/SearchControl/ContextualItems'
import { ContextualItemsContext, MenuItemBlock } from "@framework/SearchControl/ContextualItems";
import { ModelEntity } from "@framework/Signum.Entities";
import { QueryRequest } from "@framework/FindOptions";
import WordSearchMenu from "./WordSearchMenu";
import WordEntityMenu from "./WordEntityMenu";
import { ButtonsContext } from "@framework/TypeContext";
import { DropdownItem } from '@framework/Components';
import * as DynamicClientOptions from '../Dynamic/DynamicClientOptions';

export function start(options: { routes: JSX.Element[], contextual: boolean, queryButton: boolean, entityButton: boolean }) {

  DynamicClientOptions.Options.checkEvalFindOptions.push({ queryName: WordTemplateEntity });
  register(QueryModel, {
    createFromTemplate: wt => Navigator.view(QueryModel.New({ queryKey: wt.query!.key })),
    createFromEntities: (wt, lites) => {
      return Navigator.API.fetchAndForget(wt).then(template => QueryModel.New({
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
    Navigator.addSettings(new EntitySettings(QueryModel, e => import('../Templating/Templates/QueryModel')));

  register(MultiEntityModel, {
    createFromTemplate: wt => Finder.findMany({ queryName: wt.query!.key })
      .then(lites => lites && MultiEntityModel.New({ entities: toMList(lites) })),
    createFromEntities: (wt, lites) => Navigator.view(MultiEntityModel.New({ entities: toMList(lites) }))
  });

  Navigator.addSettings(new EntitySettings(WordTemplateEntity, e => import('./Templates/WordTemplate')));

  Operations.addSettings(new EntityOperationSettings(WordTemplateOperation.CreateWordReport, {
    onClick: ctx => {

      var promise: Promise<string | undefined> = ctx.entity.systemWordTemplate ? API.getConstructorType(ctx.entity.systemWordTemplate) : Promise.resolve(undefined);
      promise
        .then<Response | undefined>(ct => {
          var template = toLite(ctx.entity);

          if (!ct || isTypeEntity(ct))
            return Finder.find({ queryName: ctx.entity.query!.key })
              .then<Response | undefined>(lite => lite && API.createAndDownloadReport({ template, lite }));
          else {
            var s = settings[ct];
            var promise = (s && s.createFromTemplate ? s.createFromTemplate(ctx.entity) : Constructor.construct(ct).then(a => a && Navigator.view(a)));
            return promise.then<Response | undefined>(entity => entity && API.createAndDownloadReport({ template, entity }));
          }
        })
        .then(response => {
          if (!response)
            return;

          saveFile(response);
        }).done();
    }
  }));

  if (options.contextual)
    ContexualItems.onContextualItems.push(getWordTemplates);

  if (options.queryButton)
    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {

      if (!ctx.searchControl.props.showBarExtension || !Navigator.isViewable(WordTemplateEntity))
        return undefined;

      return <WordSearchMenu searchControl={ctx.searchControl} />;
    });

  if (options.entityButton) {
    ButtonBar.onButtonBarRender.push(getEntityWordButtons);
  }
}

export function getEntityWordButtons(ctx: ButtonsContext): Array<React.ReactElement<any> | undefined> | undefined {

  if (Navigator.isViewable(WordTemplateEntity) && ctx.pack.wordTemplates && ctx.pack.wordTemplates.length > 0)
    return [<WordEntityMenu entityPack={ctx.pack as EntityPack<Entity>} />]

  return undefined;
}

export interface WordModelSettings<T> {
  createFromTemplate?: (wt: WordTemplateEntity) => Promise<ModelEntity | undefined>;
  createFromEntities?: (wt: Lite<WordTemplateEntity>, lites: Array<Lite<Entity>>) => Promise<ModelEntity | undefined>;
  createFromQuery?: (wt: Lite<WordTemplateEntity>, req: QueryRequest) => Promise<ModelEntity | undefined>;
}

export const settings: { [typeName: string]: WordModelSettings<ModifiableEntity> } = {};

export function register<T extends ModifiableEntity>(type: Type<T>, setting: WordModelSettings<T>) {
  settings[type.typeName] = setting;
}

export function getWordTemplates(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> | undefined {

  if (!Navigator.isViewable(WordTemplateEntity) || ctx.lites.length == 0)
    return undefined;

  return API.getWordTemplates(ctx.queryDescription.queryKey, ctx.lites.length > 1 ? "Multiple" : "Single", { lite: (ctx.lites.length == 1 ? ctx.lites[0] : null) })
    .then(wts => {
      if (!wts.length)
        return undefined;

      return {
        header: WordTemplateEntity.nicePluralName(),
        menuItems: wts.map(wt =>
          <DropdownItem data-operation={wt.EntityType} onClick={() => handleMenuClick(wt, ctx)}>
            <FontAwesomeIcon icon={["far", "file-word"]} className="icon" />
            {wt.toStr}
          </DropdownItem>
        )
      } as MenuItemBlock;
    });
}

export function handleMenuClick(wt: Lite<WordTemplateEntity>, ctx: ContextualItemsContext<Entity>) {

  Navigator.API.fetchAndForget(wt)
    .then(wordTemplate => wordTemplate.systemWordTemplate ? API.getConstructorType(wordTemplate.systemWordTemplate) : Promise.resolve(undefined))
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
    .then(response => response && saveFile(response))
    .done();
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
    return ajaxPostRaw({ url: "~/api/word/createReport" }, request);
  }

  export function getConstructorType(systemWordTemplate: SystemWordTemplateEntity): Promise<string> {
    return ajaxPost<string>({ url: "~/api/word/constructorType" }, systemWordTemplate);
  }

  export function getWordTemplates(queryKey: string, visibleOn: WordTemplateVisibleOn, request: GetWordTemplatesRequest): Promise<Lite<WordTemplateEntity>[]> {
    return ajaxPost<Lite<WordTemplateEntity>[]>({ url: `~/api/word/wordTemplates?queryKey=${queryKey}&visibleOn=${visibleOn}` }, request);
  }
}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    wordTemplates?: Array<Lite<WordTemplateEntity>>;
  }
}

declare module '@framework/FindOptions' {

  export interface QueryDescription {
    wordTemplates?: Array<Lite<WordTemplateEntity>>;
  }
}
