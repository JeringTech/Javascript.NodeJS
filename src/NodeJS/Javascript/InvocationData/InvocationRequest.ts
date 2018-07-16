import ModuleSourceType from './ModuleSourceType';

interface InvocationRequest {
    moduleSource: string;
    moduleSourceType: ModuleSourceType;
    newCacheIdentifier?: string;
    exportName?: string;
    args?: object[];
}

export default InvocationRequest;