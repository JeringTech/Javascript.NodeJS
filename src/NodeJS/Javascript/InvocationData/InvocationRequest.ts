import ModuleSourceType from './ModuleSourceType';

interface InvocationRequest {
    moduleSource: string;
    moduleSourceType: ModuleSourceType;
    cacheIdentifier?: string;
    exportName?: string;
    args?: object[];
}

export default InvocationRequest;