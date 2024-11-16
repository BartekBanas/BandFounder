import {ArtistDto} from "../components/common/Dto";
import {API_URL} from "../config";
import {mantineErrorNotification} from "../components/common/mantineNotification";
import {authorizedHeaders} from "../hooks/authentication";

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

export async function getMusicianRoles(): Promise<string[]> {
    const response = await fetch(`${API_URL}/roles`, {
        method: 'GET',
    });

    if (!response.ok) {
        mantineErrorNotification('Failed to fetch musician roles');
        throw new Error('Failed to fetch musician roles');
    }

    return response.json();
}