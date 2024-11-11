import {getAuthToken} from "./authentication";

export function sleep(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

export function authorizedHeaders() {
    const token = getAuthToken();
    return {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
    };
}