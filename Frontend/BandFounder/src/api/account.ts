import {API_URL} from "../config";
import {authorizedHeaders, getUserId, removeAuthToken, removeUserId} from "../hooks/authentication";
import {mantineErrorNotification} from "../components/common/mantineNotification";
import {Account} from "../types/Account";

export async function registerAccount(name: string, email: string, password: string) {
    const response = await fetch(`${API_URL}/accounts/`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            name: name,
            email: email,
            password: password
        }),
    });

    const responseContent: string = await response.text();

    if (response.ok) {
        return responseContent; // Authentication JWT
    } else {
        mantineErrorNotification(responseContent);
        throw new Error(responseContent);
    }
}

export async function login(usernameOrEmail: string, password: string): Promise<string> {
    const response = await fetch(`${API_URL}/accounts/authenticate`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            usernameOrEmail: usernameOrEmail,
            password: password,
        }),
    });

    const responseContent = await response.text();

    if (!response.ok) {
        mantineErrorNotification("Login failed");
        throw new Error(responseContent);
    }

    return responseContent; // Authentication JWT
}

export async function getMyAccount(): Promise<Account> {
    const response = await fetch(`${API_URL}/accounts/me`, {
        method: 'GET',
        headers: authorizedHeaders()
    });

    if (!response.ok) {
        mantineErrorNotification('Failed to fetch account details');
        throw new Error('Failed to fetch account details');
    }

    const account: Account = await response.json();
    return account;
}

export async function updateMyAccount(name: string | null, password: string | null, email: string | null) {
    const requestBody: { name?: string; password?: string; email?: string } = {};

    requestBody.name = name ?? undefined;
    requestBody.password = password ?? undefined;
    requestBody.email = email ?? undefined;

    const response = await fetch(`${API_URL}/accounts/me`, {
        method: 'PUT',
        headers: authorizedHeaders(),
        body: JSON.stringify(requestBody),
    });

    if (!response.ok) {
        throw new Error(await response.text());
    }

    return response;
}

export async function deleteMyAccount() {
    const response = await fetch(`${API_URL}/accounts/me`, {
        method: 'DELETE',
        headers: authorizedHeaders()
    });

    removeAuthToken();
    removeUserId();

    return response;
}

export async function getMyMusicianRoles(): Promise<string[]> {
    const response = await fetch(`${API_URL}/accounts/${getUserId()}/roles`, {
        method: 'GET',
        headers: authorizedHeaders()
    });

    if (!response.ok) {
        mantineErrorNotification('Failed to fetch own roles');
        throw new Error('Failed to fetch own roles');
    }

    return response.json();
}

export async function deleteMyMusicianRole(role: string): Promise<void> {
    const response = await fetch(`${API_URL}/accounts/roles`, {
        method: 'DELETE',
        headers: authorizedHeaders(),
        body: JSON.stringify(role)
    });

    if (!response.ok) {
        mantineErrorNotification(`Failed to delete role ${role}`);
        throw new Error(`Failed to delete role ${role}`);
    }
}

export async function getUser(guid: string): Promise<Account> {
    try {
        const response = await fetch(`${API_URL}/accounts/${guid}`, {
            method: 'GET',
            headers: authorizedHeaders()
        });

        if (!response.ok) {
            if (response.status === 404) {
                mantineErrorNotification('User not found');
                throw new Error('User not found');
            } else if (response.status === 401) {
                mantineErrorNotification('Unauthorized (401)');
                throw new Error('Unauthorized');
            } else {
                mantineErrorNotification(`Failed to fetch user: ${response.status}`);
                throw new Error(`Failed to fetch user: ${response.status}`);
            }
        }

        const data: Account = await response.json();
        return data;
    } catch (error) {
        console.error(error);
        throw error;
    }
}