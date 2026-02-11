import * as React from 'react'
import { RouteObject } from 'react-router'
import { Dropdown } from 'react-bootstrap'
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { RecipientEmbedded, RemoteEmailFolderModel, RemoteEmailMessageMessage, RemoteEmailMessageModel, RemoteEmailMessageQuery } from './Signum.Mailing.MicrosoftGraph.RemoteEmails';
import { EntityOperationSettings, Operations } from '@framework/Operations'
import { EmailMessageEntity } from '../../Signum.Mailing/Signum.Mailing'
import {
  Entity,
  Lite, ModifiableEntity, OperationMessage, SearchMessage, newMListElement } from '@framework/Signum.Entities'
import { EntityBaseController, EntityCombo, EntityLine, LiteAutocompleteConfig } from '@framework/Lines'
import { ajaxGet, ajaxGetRaw, ajaxPost, ajaxPostRaw } from '@framework/Services'
import { UserEntity, UserLiteModel } from '../../Signum.Authorization/Signum.Authorization'
import MessageModal from '@framework/Modals/MessageModal'
import { QueryToken, ResultRow, SubTokensOptions, isFilterCondition } from '@framework/FindOptions'
import { FilterOptionParsed, SearchControlHandler, SearchControlLoaded } from '@framework/Search'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import RemoteEmailPopover from './RemoteEmailPopover'
import { FolderLine } from './FolderLine'
import { ModelConverterSymbol } from '../../Signum.Templating/Signum.Templating'
import { getQueryKey,
  getTypeInfo } from '@framework/Reflection'
import { LinkButton } from '@framework/Basics/LinkButton'
import { MultiMessageProgressModal } from './MultiMessageProgressModal';
import * as ContextualItems from '@framework/SearchControl/ContextualItems';
import SelectorModal from '@framework/SelectorModal';
import { ButtonBar, ButtonBarManager } from '@framework/Frames/ButtonBar';
import { ButtonBarElement,
  ButtonsContext } from '@framework/TypeContext';
import { classes } from '@framework/Globals';

export namespace RemoteEmailsClient {
  
  
  export function start(options: { routes: RouteObject[] }): void {
    Navigator.addSettings(new EntitySettings(RemoteEmailMessageModel, e => import('./RemoteEmailMessage'), {
      renderSubTitle: r => <span>
        {getTypeInfo(r.Type).niceName}
        <span className="sf-hide-id ms-1"> {r.id}</span>
      </span>
    }));
  
    Navigator.addSettings(new EntitySettings(RecipientEmbedded, undefined, { isViewable: "Never" }));
  
    Finder.quickFilterRules.push({
      name: "EmailAddress",
      applicable: (qt: QueryToken, value: unknown, sc: SearchControlLoaded) => qt.filterType == "Embedded" && qt.type.name == RecipientEmbedded.typeName,
      execute: async (qt: QueryToken, value: unknown, sc: SearchControlLoaded) => {
        var token = await sc.parseSingleFilterToken(qt.fullKey + ".EmailAddress");
  
        return sc.addQuickFilter(token, "EqualTo", (value as RecipientEmbedded | undefined)?.emailAddress);
      }
    });
  
    Finder.quickFilterRules.push({
      name: "RemoteEmailFolder",
      applicable: (qt: QueryToken, value: unknown, sc: SearchControlLoaded) => qt.filterType == "Model" && qt.type.name == RemoteEmailFolderModel.typeName,
      execute: async (qt: QueryToken, value: unknown, sc: SearchControlLoaded) => {
        return sc.addQuickFilter(qt, "EqualTo", value);
      }
    });
  
    Finder.Encoder.encodeModel[RemoteEmailFolderModel.typeName] = (model: RemoteEmailFolderModel) => {
      return model.folderId;
    };
    Finder.Decoder.decodeModel[RemoteEmailFolderModel.typeName] = (folderId: string | RemoteEmailFolderModel | null) => {
      if (!folderId)
        return null;
  
      if (typeof folderId == "string")
        return RemoteEmailFolderModel.New({ folderId: folderId, displayName: folderId });
  
      if (RemoteEmailFolderModel.isInstance(folderId))
        return folderId;
  
      throw new Error("Unexpected " + folderId); 
    };  
  
    Finder.filterValueFormatRules.push({
      name: "User",
      applicable: (f: FilterOptionParsed, ffc: Finder.FilterFormatterContext) => isFilterCondition(f) && f.token?.fullKey == "User" && f.operation == "EqualTo",
      renderValue: (f: FilterOptionParsed, ffc: Finder.FilterFormatterContext) => {
  
        return <EntityLine ctx={ffc.ctx} type={f.token!.type} create={false} onChange={() => ffc.handleValueChange(f)} label={ffc.label} mandatory={ffc.mandatory} />;
      }
    });
  
    Finder.filterValueFormatRules.push({
      name: "EmailFolder",
      applicable: (f: FilterOptionParsed, ffc: Finder.FilterFormatterContext) => isFilterCondition(f) && f.token?.type.name == RemoteEmailFolderModel.typeName,
      renderValue: (f: FilterOptionParsed, ffc: Finder.FilterFormatterContext) => {
        var user = ffc.filterOptions.firstOrNull(a => isFilterCondition(a) && a.token?.fullKey == "User" && a.operation == "EqualTo");
        return <FolderLine ctx={ffc.ctx} mandatory={ffc.mandatory} label={ffc.label} user={user?.value as Lite<UserEntity>} onChange={() => ffc.handleValueChange(f)} />
      }
    });
  
    Finder.addSettings({
      queryName: RemoteEmailMessageQuery.RemoteEmailMessages,
      allowCreate: false,
      markRowsColumn: "Id",
      defaultFilters: [
        { token: "User", value: AppContext.currentUser, pinned: { active: "Always" } },
      ],
      hiddenColumns: [
        { token: "User" },
        { token: "Id" },
        { token: "HasAttachments" },
        { token: "IsRead" }
      ],
      onDoubleClick: (e, row, col, sc) => {
        openMessage(row, sc!);
      },
      entityFormatter: new Finder.EntityFormatter(ctx => {
        return (
          <LinkButton title={SearchMessage.View.niceToString()} onClick={async e => openMessage(ctx.row, ctx.searchControl!)}>
            {EntityBaseController.getViewIcon()}
          </LinkButton>
        );
  
      }),
      formatters: {
        "Subject": new Finder.CellFormatter((val, cfc) => {
          var hasAttachments = cfc.searchControl?.getRowValue(cfc.row, "HasAttachments") ? <FontAwesomeIcon icon="paperclip" className="me-1" /> : null;
          var isRead = cfc.searchControl?.getRowValue(cfc.row, "IsRead") as boolean;
          var user = cfc.searchControl?.getRowValue(cfc.row, "User") as Lite<UserEntity>;
          var id = cfc.searchControl?.getRowValue(cfc.row, "Id") as string;
  
          var popIcon = <RemoteEmailPopover subject={val} isRead={isRead} user={user} remoteEmailId={id} />
  
          if (isRead)
            return <span className="try-no-wrap">{popIcon} {hasAttachments} {(val as string)?.etc(100)}</span>;
          else
            return <strong className="try-no-wrap">{popIcon} {hasAttachments} {(val as string)?.etc(100)}</strong>;
        }, true)
      }
    });

    ContextualItems.onContextualItems.push(getMessageContextualItems);
    ButtonBarManager.onButtonBarRender.push(getMessageButtons);
  }

  export function getMessageButtons(ctx: ButtonsContext): Array<ButtonBarElement | undefined> | undefined {
    if (!RemoteEmailMessageModel.isInstance(ctx.pack.entity))
      return undefined;

    var message = ctx.pack.entity;

    var oid = (message.user?.model as UserLiteModel)?.oID!;
    if (oid == null)
      throw new Error("User has no OID");

    async function finishSuccess() {
      Operations.notifySuccess();

      var remote = await API.getRemoteEmail(oid, message.id!);

      ctx.frame.onReload(await Navigator.toEntityPack(remote));
    }
    

    return [
      {
        button: <button className={classes("btn ", "btn-info")} 
          title={RemoteEmailMessageMessage.Move.niceToString()}
          onClick={async () => {
            var folder = await selectFolder(oid);
            if (folder == null)
              return;

            await API.movingEmails(oid!, [message.id!], folder?.folderId);

            Operations.notifySuccess();
          }}>
          <FontAwesomeIcon aria-hidden={true} icon="folder-tree" /> {RemoteEmailMessageMessage.Move.niceToString()}
        </button>,
      },
      {
        button: <button className={classes("btn ", "btn-success")} 
          title={RemoteEmailMessageMessage.AddCategory.niceToString()}
          onClick={async () => {
            var categories = await API.getRemoteCategories(oid);

            var category = await SelectorModal.chooseElement(categories, {
              title: RemoteEmailMessageMessage.AddCategory.niceToString(),
            });

            if (category == null)
              return null;
          
            await API.changeCategoriesEmails(oid, { messageIds: [message.id!], categoriesToAdd: [category], categoriesToRemove: [] });

            finishSuccess();

          }}>
          <FontAwesomeIcon aria-hidden={true} icon="tags" /> {RemoteEmailMessageMessage.AddCategory.niceToString()}
        </button>,
      },
      {
        button: <button className={classes("btn ", "btn-warning")} 
          title={RemoteEmailMessageMessage.RemoveCategory.niceToString()}
          onClick={async () => {
            var category = await SelectorModal.chooseElement(message.categories.map(a => a.element), {
              title: RemoteEmailMessageMessage.RemoveCategory.niceToString(),
              forceShow: true
            });

            if (category == null)
              return null;
              
            await API.changeCategoriesEmails(oid, { messageIds: [message.id!], categoriesToAdd: [], categoriesToRemove: [category] });

            finishSuccess();
          }}>
          <FontAwesomeIcon aria-hidden={true} icon="tags" /> {RemoteEmailMessageMessage.RemoveCategory.niceToString()}
        </button>,
      },
      {
        button: <button className={classes("btn ", "btn-danger")} 
          title={RemoteEmailMessageMessage.Delete.niceToString()}
          onClick={async () => {
            
            if (!await confirmDelete(1))
              return;

            await API.deleteEmails(oid, [message.id!])

            Operations.notifySuccess();

            ctx.frame.onClose(undefined);
          }}>
          <FontAwesomeIcon aria-hidden={true} icon="trash" /> {RemoteEmailMessageMessage.Delete.niceToString()}
        </button>,
      },
    ];
  }

  function confirmDelete(numberOfMessages: number): Promise<boolean> {
    return MessageModal.show({
      title: RemoteEmailMessageMessage.Delete.niceToString(),
      message: RemoteEmailMessageMessage.PleaseConfirmYouWouldLikeToDelete0FromOutlook.niceToString()
        .formatHtml(<span><strong>{numberOfMessages}</strong> {numberOfMessages == 1 ? RemoteEmailMessageMessage.Message.niceToString() : RemoteEmailMessageMessage.Messages.niceToString()}</span>),
      buttons: "yes_no",
      icon: "warning",
      style: "warning",
    }).then(result => { return result == "yes"; });
  }

  async function selectFolder(oid: string): Promise<RemoteEmailFolderModel | undefined> {
    var folders = await API.getRemoteFolders(oid);

    var folder = await SelectorModal.chooseElement(folders, {
      title: RemoteEmailMessageMessage.Move.niceToString(),
      message: RemoteEmailMessageMessage.SelectAFolder.niceToString(),
      buttonDisplay: a => a.displayName,
    });

    return folder;
  }

  export async function getMessageContextualItems(ctx: ContextualItems.ContextualItemsContext<Entity>): Promise<ContextualItems.MenuItemBlock | undefined> {
    if (ctx.queryDescription.queryKey != getQueryKey(RemoteEmailMessageQuery.RemoteEmailMessages))
      return undefined;
   
    if (!(ctx.container instanceof SearchControlLoaded))
      return undefined;

    const sc = ctx.container as SearchControlLoaded;

    var messageIds = sc.state.selectedRows?.map(r => sc.getRowValue<string>(r, "Id")).notNull() ?? [];

    if (messageIds.length == 0)
      return undefined;

    const user = sc.state.resultFindOptions?.filterOptions.firstOrNull(fo => isFilterCondition(fo) && fo.token?.fullKey == "User" && fo.operation == "EqualTo")?.value as Lite<UserEntity>;
    if (user == null)
      throw new Error("No User found in filters");

    const oid = (user.model as UserLiteModel)?.oID;
    if (oid == null)
      throw new Error("User has no OID"); 

    return ({
      header: RemoteEmailMessageMessage.Messages.niceToString(),
      menuItems: [
       
        {
          fullText: RemoteEmailMessageMessage.Move.niceToString(),
          menu: <Dropdown.Item onClick={async () => {
         
            var folder = await selectFolder(oid);
            if (folder == null)
              return null;
          
            var result = await API.movingEmails(oid, messageIds, folder.folderId);

            sc.markRows(result.errors);
          }}>
            <FontAwesomeIcon aria-hidden={true} icon={"folder-tree"} className="icon" color="blue" />
            {RemoteEmailMessageMessage.Move.niceToString()}
          </Dropdown.Item>
        },
        {
          fullText: RemoteEmailMessageMessage.AddCategory.niceToString(),
          menu: <Dropdown.Item onClick={async () => {
            var categories = await API.getRemoteCategories(oid);

            var category = await SelectorModal.chooseElement(categories, {
              title: RemoteEmailMessageMessage.AddCategory.niceToString(),
            });

            if (category == null)
              return null;
          
            var result = await API.changeCategoriesEmails(oid, { messageIds, categoriesToAdd: [category], categoriesToRemove: [] });

            sc.markRows(result.errors);
          }}>
            <FontAwesomeIcon aria-hidden={true} icon={"tag"} className="icon" color="green" />
            {RemoteEmailMessageMessage.AddCategory.niceToString()}
          </Dropdown.Item>
        },
        {
          fullText: RemoteEmailMessageMessage.RemoveCategory.niceToString(),
          menu: <Dropdown.Item onClick={async () => {
            var categories = await API.getRemoteCategories(oid);

            var category = await SelectorModal.chooseElement(categories, {
              title: RemoteEmailMessageMessage.RemoveCategory.niceToString(),
            });

            if (category == null)
              return null;
          
            var result = await API.changeCategoriesEmails(oid, { messageIds, categoriesToAdd: [], categoriesToRemove: [category] });

            sc.markRows(result.errors);
          }}>
            <FontAwesomeIcon aria-hidden={true} icon={"tag"} className="icon" color="orange" />
            {RemoteEmailMessageMessage.RemoveCategory.niceToString()}
          </Dropdown.Item>
        },
        {
          fullText: RemoteEmailMessageMessage.Delete.niceToString(),
          menu: <Dropdown.Item onClick={async () => {
            if (!await confirmDelete(messageIds.length))
              return;

            var result = await API.deleteEmails(oid, messageIds);

            sc.markRows(result.errors);
          }} >
            <FontAwesomeIcon aria-hidden={true} icon={"trash"} className="icon" color="red" />
            {RemoteEmailMessageMessage.Delete.niceToString()}
          </Dropdown.Item>
        },
      ]
    });
  }
  
  async function openMessage(row: ResultRow, sc: SearchControlLoaded) {
    var user = sc.getRowValue<Lite<UserEntity>>(row, "User");
    var messageId = sc.getRowValue<string>(row, "Id");
    if (messageId == null)
      throw new Error("No User found");
  
    var oid = (user?.model as UserLiteModel).oID;
    if (oid == null)
      throw new Error("User has no OID");
  
    if (messageId == null)
      throw new Error("No message Id found");
  
    var message = await API.getRemoteEmail(oid, messageId);
  
    await Navigator.view(message);

    await sc.doSearchPage1();
  }
  
  export namespace API {
    export function getRemoteEmail(userOID: string, messageId: string): Promise<RemoteEmailMessageModel> {
      return ajaxGet({ url: `/api/remoteEmail/${userOID}/message/${messageId}` });
    }
  
    export function getRemoteFolders(oid: string): Promise<Array<RemoteEmailFolderModel>> {
      return ajaxGet({ url: `/api/remoteEmailFolders/${oid}` });
    }

    export function getRemoteCategories(oid: string): Promise<Array<string>> {
      return ajaxGet({ url: `/api/remoteEmailCategories/${oid}` });
    }
  
    export function getRemoteAttachment(userOID: string, messageId: string, attachmentId: string): Promise<Response> {
      return ajaxGetRaw({ url: `/api/remoteEmail/${userOID}/message/${messageId}/attachment/${attachmentId}` });
    }
  
    export function getRemoteAttachmentUrl(userOID: string, messageId: string, attachmentId: string): string {
      return `/api/remoteEmail/${userOID}/message/${messageId}/attachment/${attachmentId}`
    }

    export function deleteEmails(userOID: string, messages: string[]): Promise<Operations.API.ErrorReport> {
      var abortController = new AbortController();
      return MultiMessageProgressModal.show(messages, RemoteEmailMessageMessage.Deleting.niceToString(), abortController,
        () => ajaxPostRaw({ url: `/api/remoteEmail/${userOID}/delete` }, messages));
    }

    export function movingEmails(userOID: string, messages: string[], folderId: string): Promise<Operations.API.ErrorReport> {
      var abortController = new AbortController();
      return MultiMessageProgressModal.show(messages, RemoteEmailMessageMessage.Moving.niceToString(), abortController,
        () => ajaxPostRaw({ url: `/api/remoteEmail/${userOID}/moveTo/${folderId}` }, messages));
    }

    export function changeCategoriesEmails(userOID: string, request: ChangeCategoriesRequest): Promise<Operations.API.ErrorReport> {
      var abortController = new AbortController();
      return MultiMessageProgressModal.show(request.messageIds, RemoteEmailMessageMessage.ChangingCategories.niceToString(), abortController,
        () => ajaxPostRaw({ url: `/api/remoteEmail/${userOID}/changeCategories` }, request));
    }
  }
}

export interface ChangeCategoriesRequest {
  messageIds: string[];
  categoriesToAdd: string[];
  categoriesToRemove: string[];
}

export interface EmailResult {
  id: string;
  error?: string;
}
