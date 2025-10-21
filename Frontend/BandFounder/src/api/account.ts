import {API_URL} from "../config";
import {
    authorizedHeader,
    authorizedHeaders,
    getAuthToken,
    getUserId
} from "../hooks/authentication";
import {
    mantineErrorNotification,
    mantineInformationNotification,
    mantineSuccessNotification
} from "../components/common/mantineNotification";
import {Account} from "../types/Account";

export async function registerAccount(name: string, email: string, password: string) {
    const response = await fetch(`${API_URL}/accounts`, {
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
    } else if (response.status === 400 || response.status === 409) { // Relevant messages from the server
        throw new Error(responseContent);
    } else {
        throw new Error(`An error occurred during registration`);
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
        const error: any = new Error(responseContent || `HTTP ${response.status} ${response.statusText}`);
        error.status = response.status;
        error.responseText = responseContent;
        try {
            error.retryAfter = response.headers.get('Retry-After');
        } catch (e) {
            // ignore header read errors
        }
        throw error;
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

export async function getAccount(accountId: string): Promise<Account> {
    const response = await fetch(`${API_URL}/accounts/${accountId}`, {
        method: 'GET',
        headers: authorizedHeaders()
    });

    if (!response.ok) {
        throw new Error('Failed to fetch user');
    }

    return await response.json();
}

export async function getAccounts(): Promise<Account[]> {
    const response = await fetch(`${API_URL}/accounts`, {
        method: 'GET',
        headers: authorizedHeaders()
    });

    if (!response.ok) {
        mantineErrorNotification('Failed to fetch accounts');
        throw new Error('Failed to fetch accounts');
    }

    const accounts: Account[] = await response.json();
    return accounts;
}

export async function updateMyAccount(name: string | null, password: string | null, email: string | null) {
    const requestBody: { name?: string; password?: string; email?: string } = {};

    requestBody.name = name ?? undefined;
    requestBody.password = password ?? undefined;
    requestBody.email = email ?? undefined;

    const response = await fetch(`${API_URL}/accounts/me`, {
        method: 'PATCH',
        headers: authorizedHeaders(),
        body: JSON.stringify(requestBody),
    });

    if (!response.ok) {
        throw new Error(await response.text());
    }

    return response;
}

export async function deleteMyAccount(): Promise<void> {
    const response = await fetch(`${API_URL}/accounts/me`, {
        method: 'DELETE',
        headers: authorizedHeaders()
    });

    if (!response.ok) {
        throw new Error(await response.text());
    }
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

export async function addMyMusicianRole(role: string): Promise<void> {
    try {
        const response = await fetch(`${API_URL}/accounts/roles`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getAuthToken()}`,
            },
            body: JSON.stringify(role), // Ensure the role field is correctly formatted
        });

        if (!response.ok) {
            throw new Error(`Failed to add ${role} role to your account`);
        }

        if (response.status === 204) {
            mantineInformationNotification(`Your account already has role ${role} assigned`);
        } else {
            mantineSuccessNotification(`Role ${role} was added to your account`);
        }

    } catch (error) {
        mantineErrorNotification(`Failed to add ${role} role to your account`);
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

export async function getAccountByUsername(username: string): Promise<Account> {
    const response = await fetch(`${API_URL}/accounts?username=${username}`, {
        method: 'GET',
        headers: authorizedHeaders()
    });

    if (!response.ok) {
        mantineErrorNotification(`Couldn't find user ${username}`);
        console.error(await response.text());
    }

    return await response.json() as Account;
}

export async function getTopGenres(guid: string, genresToReturn: number = 10): Promise<string[]> {
    try {
        const response = await fetch(`${API_URL}/accounts/${guid}/genres`, {
            method: 'GET',
            headers: authorizedHeaders()
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        const genres: string[] = await response.json();
        return genres.slice(0, genresToReturn);
    } catch (error) {
        console.error('Error getting top genres:', error);
        return [];
    }
}

export async function uploadProfilePicture(file: File): Promise<void> {
    const formData = new FormData();
    formData.append("file", file);

    const response = await fetch(`${API_URL}/accounts/${getUserId()}/profile-picture`, {
        method: 'PUT',
        headers: authorizedHeader(),
        body: formData,
    });

    if (!response.ok) {
        throw new Error(await response.text());
    }
}

export async function getProfilePicture(accountId: string): Promise<string | null> {
    const response = await fetch(`${API_URL}/accounts/${accountId}/profile-picture`, {
        method: 'GET',
        headers: authorizedHeaders(),
    });

    if (!response.ok) {
        if (response.status === 404) {
            return null;
        }

        mantineErrorNotification('Failed to fetch profile picture');
        throw new Error('Failed to fetch profile picture');
    }

    const blob = await response.blob();
    return URL.createObjectURL(blob);
}