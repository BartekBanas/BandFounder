import {getAuthToken} from "./authentication";

export function sleep(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

export const authorizedHeaders = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${getAuthToken()}`
}