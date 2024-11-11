import {API_URL} from "../../config";
import {removeAuthToken, removeUserId} from "../../hooks/authentication";
import {ArtistDto} from "../common/Dto";
import {mantineErrorNotification} from "../common/mantineNotification";
import {authorizedHeaders} from "../../hooks/utils";

export async function updateAccountRequest(name: string | null, password: string | null, email: string | null) {
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

export async function deleteAccountRequest() {
    const response = await fetch(`${API_URL}/accounts/me`, {
        method: 'DELETE',
        headers: authorizedHeaders()
    });

    removeAuthToken();
    removeUserId();

    return response;
}

export async function getArtists(): Promise<ArtistDto[]> {
    const response = await fetch(`${API_URL}/artists`, {
        method: 'GET',
        headers: authorizedHeaders()
    });

    if (!response.ok) {
        mantineErrorNotification('Failed to fetch artists');
        throw new Error('Failed to fetch artists');
    }

    return response.json();
}