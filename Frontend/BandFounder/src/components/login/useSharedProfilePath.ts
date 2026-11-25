import {useLocation} from "react-router-dom";

function isSharedProfilePath(path: unknown): path is string {
    if (typeof path !== "string") {
        return false;
    }

    const match = /^\/profile\/([^/\\?#\s]+)\/?$/i.exec(path);
    if (!match) {
        return false;
    }

    try {
        const username = decodeURIComponent(match[1]);
        return username !== "." && username !== "..";
    } catch {
        return false;
    }
}

export function useSharedProfilePath() {
    const {pathname, state} = useLocation();
    const previousProfilePath = state?.sharedProfilePath;

    if (isSharedProfilePath(pathname)) {
        return pathname;
    }

    return isSharedProfilePath(previousProfilePath) ? previousProfilePath : undefined;
}
