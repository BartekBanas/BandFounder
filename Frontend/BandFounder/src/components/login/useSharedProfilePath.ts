import {useLocation} from "react-router-dom";

function isSharedProfilePath(path: unknown): path is string {
    return typeof path === "string" && /^\/profile\/[a-z0-9_-]+\/?$/i.test(path);
}

export function useSharedProfilePath() {
    const {pathname, state} = useLocation();
    const previousProfilePath = state?.sharedProfilePath;

    if (isSharedProfilePath(pathname)) {
        return pathname;
    }

    return isSharedProfilePath(previousProfilePath) ? previousProfilePath : undefined;
}
