import Cookies from 'js-cookie';

export function useIsAuthenticated  ()  {
    return Cookies.get('auth_token') !== undefined;
}

export function getAuthToken () {
    return Cookies.get('auth_token');
}

export function setAuthToken(token: string) {
    return Cookies.set('auth_token', token, {
        expires: new Date(Date.now() + 1000 * 60 * 15), // 15 minutes
        sameSite: 'Strict',
    });
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

export function authorizedHeaders(): HeadersInit {
    const token = getAuthToken();

    if (!token) {
        window.location.href = '/login/expiredSession';
        throw new Error('Authentication token not found');
    }

    return {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
    };
}