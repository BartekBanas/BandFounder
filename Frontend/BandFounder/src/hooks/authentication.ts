import Cookies from 'js-cookie';

export function useIsAuthenticated  ()  {
    return Cookies.get('auth_token') !== undefined;
}

export function getAuthToken () {
    return Cookies.get('auth_token');
}

export function setAuthToken (token: string) {
    return Cookies.set('auth_token', token);
}

export function removeAuthToken () {
    return Cookies.remove('auth_token');
}

export function getUserId () {
    return Cookies.get('user_id');
}

export function setUserId (id: string) {
    return Cookies.set('user_id', id);
}

export function removeUserId () {
    return Cookies.remove('user_id');
}