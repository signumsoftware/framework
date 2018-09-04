import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import { TimeSpanEmbedded, DateSpanEmbedded, DisableOperation } from './Signum.Entities.Basics'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '@framework/QuickLinks'
import { getAllTypes } from "@framework/Reflection";

export function start(options: { routes: JSX.Element[] }) {

    Operations.addSettings(new EntityOperationSettings(DisableOperation.Disable, {
        contextual: {
            icon: ["fas", "arrow-down"],
            iconColor: "gray",
        },
        contextualFromMany: {
            icon: ["fas", "arrow-down"],
            iconColor: "gray",
        },
    }));

    Operations.addSettings(new EntityOperationSettings(DisableOperation.Enabled, {
        contextual: {
            icon: ["fas", "arrow-up"],
            iconColor: "black",
        },
        contextualFromMany: {
            icon: ["fas", "arrow-up"],
            iconColor: "black",
        },
    }));

    var typesToOverride = getAllTypes().filter(a => a.queryDefined && a.kind == "Entity" && a.members["[DisabledMixin].IsDisabled"]);

    typesToOverride.forEach(ti => {

        {
            var querySettings = Finder.getSettings(ti.name);

            if (!querySettings) {
                querySettings = { queryName: ti.name };
                Finder.addSettings(querySettings);
            }

            querySettings.hiddenColumns = [
                { token: "Entity.IsDisabled" }
            ];

            querySettings.rowAttributes = (row, columns) => {

                var index = columns.indexOf("Entity.IsDisabled");
                return row.columns[index] ? { style: { fontStyle: "italic", color: "gray" } } : undefined;
            };
        }

        {
            var entitySettings = Navigator.getSettings(ti.name);

            if (!entitySettings) {
                entitySettings = new EntitySettings(ti.name);
                Navigator.addSettings(entitySettings);
            }

            if (!entitySettings.findOptions) {
                entitySettings.findOptions = {
                    queryName: ti.name,
                    filterOptions: [{ token: "Entity.IsDisabled", operation: "EqualTo", value: false, frozen: true }]
                };
            }
        }
    });
}
