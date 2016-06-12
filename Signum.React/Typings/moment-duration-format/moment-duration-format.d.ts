declare module moment {
    interface Duration {
        format(template?: string | Function, precision?: number, settings?: Object): string;
    }
}