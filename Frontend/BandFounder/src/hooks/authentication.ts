import Cookies from 'js-cookie';

export function useIsAuthenticated  ()  {
    return Cookies.get('auth_token') !== undefined;
}

export function getAuthToken () {
    return Cookies.get('auth_token');
}

export function setAuthToken(token: string) {
    return Cookies.set('auth_token', token, {
        expires: new Date(Date.now() + 1000 * 60 * 60), // 1 hour
        sameSite: 'Strict',
    });
}

export function removeAuthToken () {
    return Cookies.remove('auth_token');
}

export function getUserId () {
    const userId = Cookies.get('user_id');
    if (!userId) {
        window.location.href = '/login/expiredSession';
        throw new Error('User ID not found');
    }

    return userId;
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

export function authorizedHeader(): HeadersInit {
    const token = getAuthToken();

    if (!token) {
        window.location.href = '/login/expiredSession';
        throw new Error('Authentication token not found');
    }

    return {
        'Authorization': `Bearer ${token}`,
    };
}