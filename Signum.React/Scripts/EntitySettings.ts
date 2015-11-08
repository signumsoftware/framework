import { Type, IType, EntityKind } from 'Framework/Signum.React/Scripts/Reflection';

export abstract class EntitySettingsBase {
    public type: IType;

    public avoidPopup: boolean;

    abstract onIsCreable(isSearch: boolean): boolean;
    abstract onIsFindable() : boolean;
    abstract onIsViewable(partialViewName: string) : boolean;
    abstract onIsNavigable(partialViewName: string, isSearch: boolean): boolean;
    abstract onIsReadonly(): boolean;

    constructor(type: IType) {
        this.type = type;
    }
}

export class EntitySettings<T> extends EntitySettingsBase {

    public type: Type<T>;

    isCreable: EntityWhen;
    isFindable: boolean;
    isViewable: boolean;
    isNavigable: EntityWhen;
    isReadOnly: boolean;

    partialViewName: (entity: T) => string;

    constructor(type: Type<T>) {
        super(type);

        switch (type.typeInfo().entityKind) {
            case EntityKind.SystemString:
                this.isCreable = EntityWhen.Never;
                this.isFindable = true;
                this.isViewable = false;
                this.isNavigable = EntityWhen.Never;
                this.isReadOnly = true;
                break;

            case EntityKind.System:
                this.isCreable = EntityWhen.Never;
                this.isFindable = true;
                this.isViewable = true;
                this.isNavigable = EntityWhen.Always;
                this.isReadOnly = true;
                break;

            case EntityKind.Relational:
                this.isCreable = EntityWhen.Never;
                this.isFindable = false;
                this.isViewable = false;
                this.isNavigable = EntityWhen.Never;
                this.isReadOnly = true;
                break;

            case EntityKind.String:
                this.isCreable = EntityWhen.IsSearch;
                this.isFindable = true;
                this.isViewable = false;
                this.isNavigable = EntityWhen.IsSearch;
                break;

            case EntityKind.Shared:
                this.isCreable = EntityWhen.Always;
                this.isFindable = true;
                this.isViewable = true;
                this.isNavigable = EntityWhen.Always;
                break;

            case EntityKind.Main:
                this.isCreable = EntityWhen.IsSearch;
                this.isFindable = true;
                this.isViewable = true;
                this.isNavigable = EntityWhen.Always;
                break;

            case EntityKind.Part:
                this.isCreable = EntityWhen.IsLine;
                this.isFindable = false;
                this.isViewable = true;
                this.isNavigable = EntityWhen.Always;
                break;

            case EntityKind.SharedPart:
                this.isCreable = EntityWhen.IsLine;
                this.isFindable = true;
                this.isViewable = true;
                this.isNavigable = EntityWhen.Always;
                break;

            default:
                break;

        }
    }

    onIsCreable(isSearch: boolean): boolean {
        return hasFlag(this.isCreable, isSearch ? EntityWhen.IsSearch : EntityWhen.IsLine);
    }


    onIsFindable(): boolean {
        return this.isFindable;
    }

    onIsViewable(partialViewName: string): boolean {
        if (!this.partialViewName && !partialViewName)
            return false;

        return this.isViewable;
    }

    onIsNavigable(partialViewName: string, isSearch: boolean): boolean {

        if (!this.partialViewName && !partialViewName)
            return false;

        return hasFlag(this.isNavigable, isSearch ? EntityWhen.IsSearch : EntityWhen.IsLine);
    }

    onIsReadonly(): boolean {
        return this.isReadOnly;
    }
}

export class EmbeddedEntitySettings<T> extends EntitySettingsBase {
    public type: Type<T>;

    partialViewName: (entity: T) => string;

    isCreable: boolean;
    isViewable: boolean;
    isReadOnly: boolean;

    constructor(type: Type<T>) {
        super(type);
    }

    onIsCreable(isSearch: boolean) {
        if (isSearch)
            throw new Error("EmbeddedEntitySettigs are not compatible with isSearch");

        return this.isCreable;
    }

    onIsFindable(): boolean {
        return false;
    }

    onIsViewable(partialViewName: string): boolean {
        if (!partialViewName && !partialViewName)
            return false; 

        return this.isViewable;
    }

    onIsNavigable(partialViewName: string, isSearch: boolean): boolean {
        return false;
    }

    onIsReadonly(): boolean {
        return this.isReadOnly;
    }
}


export enum EntityWhen {
    Always = 3,
    IsSearch = 2,
    IsLine = 1,
    Never = 0,
}