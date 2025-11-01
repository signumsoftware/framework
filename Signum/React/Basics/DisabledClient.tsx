import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '../Navigator'
import { Finder } from '../Finder'
import { Operations, EntityOperationSettings } from '../Operations'
import { getAllTypes } from "../Reflection";
import { DisableOperation, DisabledMixin } from "../Signum.Basics";

export namespace DisabledClient {
  
  export function start(options: { routes: RouteObject[] }): void {
  
    Operations.addSettings(new EntityOperationSettings(DisableOperation.Disable, {
      contextual: {
        icon: "arrow-down",
        iconColor: "gray",
      },
      contextualFromMany: {
        icon: "arrow-down",
        iconColor: "gray",
      },
    }));
  
    Operations.addSettings(new EntityOperationSettings(DisableOperation.Enabled, {
      contextual: {
        icon: "arrow-up",
        iconColor: "var(--bs-body-color)",
      },
      contextualFromMany: {
        icon: "arrow-up",
        iconColor: "var(--bs-body-color)",
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
          ...(querySettings.hiddenColumns ?? []),
          { token: DisabledMixin.token(e => e.entity.isDisabled) }
        ];
  
        querySettings.rowAttributes = (row, sc) => {

          var disabled = sc.getRowValue(row, "Entity.IsDisabled");
          return disabled ? { style: { fontStyle: "italic", color: "gray" } } : undefined;
        };
      }
  
      {
        var entitySettings = Navigator.getSettings(ti.name);
  
        if (!entitySettings) {
          entitySettings = new EntitySettings(ti.name);
          Navigator.addSettings(entitySettings);
        }
  
        if (!entitySettings.defaultFindOptions) {
          entitySettings.defaultFindOptions = {
            queryName: ti.name,
            filterOptions: [{ token: DisabledMixin.token(e => e.entity.isDisabled), operation: "EqualTo", value: false, frozen: true }]
          };
        }
      }
    });
  }
}
