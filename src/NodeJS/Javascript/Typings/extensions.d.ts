declare function __non_webpack_require__(path: string): any;

declare module NodeJS {
    interface Global {
        module: Module;
    }
}