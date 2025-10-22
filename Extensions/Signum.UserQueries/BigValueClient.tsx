import { Lite, Entity } from "@framework/Signum.Entities";
import { PanelPartContentProps } from "../Signum.Dashboard/DashboardClient";
import { BigValuePartEntity } from "./Signum.UserQueries";
import { Type } from "@framework/Reflection";

interface CustomMessageContext<T extends Entity> {
  content: BigValuePartEntity;
  entity?: Lite<T>;
  value?: any;
}

interface CustomBigValue<T extends Entity> {
  customMessage?: (c: CustomMessageContext<T>) => React.ReactNode;
  customValue?: (c: CustomMessageContext<T>) => React.ReactNode;
}
export namespace BigValueClient {
  export const customBigValues: Record<string, Record<string, CustomBigValue<Entity>>> = {};

  export function registerCustomBigValue<T extends Entity>(entityType: Type<T> | undefined, messageName: string, customBigValue: CustomBigValue<T>): void {
    (customBigValues[entityType?.typeName ?? "global"] ??= {})[messageName] = customBigValue as CustomBigValue<Entity>;
  }

  export function getKeys(entityType: string | undefined): string[] {
    return Object.keys(customBigValues[entityType ?? "global"] ?? {});
  }

  export function renderCustomBigValue(messageName: string, ctx: CustomMessageContext<Entity>): { message?: React.ReactNode, value?: React.ReactNode } {
    var cm = customBigValues[ctx.entity?.EntityType ?? "global"][messageName];
    if (cm == null)
      return {
        message: <span className="text-danger">No CustomMessage {messageName} registered for {ctx.entity?.EntityType ?? "global"}</span >
      };

    return {
      message: cm.customMessage?.(ctx),
      value: cm.customValue?.(ctx)
    };
  }
}

