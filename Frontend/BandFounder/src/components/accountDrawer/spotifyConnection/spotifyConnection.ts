import {API_URL} from "../../../config";
import {
    mantineErrorNotification,
    mantineInformationNotification,
    mantineSuccessNotification
} from "../../common/mantineNotification";
import {authorizedHeaders} from "../../../hooks/authentication";

const BaseAppUrl = "http://localhost:3000/";
const SpotifyConnectionPageUrl = BaseAppUrl + "spotifyConnection/callback/";
const SpotifyAuthorizeUrl = "https://accounts.spotify.com/authorize";

export async function redirectToSpotifyAuthorizationPage() {
    try {
        const spotifyClientId = await fetchSpotifyAppClientId();

        let url = SpotifyAuthorizeUrl;
        url += "?client_id=" + spotifyClientId;
        url += "&response_type=code";
        url += "&redirect_uri=" + encodeURI(SpotifyConnectionPageUrl);
        url += "&show_dialog=true";
        url += "&scope=user-top-read user-follow-read";
        window.location.href = url; // Show Spotify's authorization screen
    } catch (error) {
        console.error('Error fetching Spotify app client ID:', error);
    }
}

export async function linkAccountWithSpotifyFromCode(): Promise<void> {
    const code = new URLSearchParams(window.location.search).get('code');
    if (code) {
        try {
            await requestSpotifyAccountLinkFromCode(code);
            mantineSuccessNotification('Linking your Spotify account was successful');
        } catch (error: any) {
            mantineErrorNotification('Failed to link Spotify account');
            throw new Error(error.message);
        }
    }
}

async function requestSpotifyAccountLinkFromCode(code: string): Promise<void> {
    const response = await fetch(`${API_URL}/spotify/tokens`, {
        method: 'POST',
        headers: authorizedHeaders(),
        body: JSON.stringify({
            code: code,
            base_app_url: encodeURI(SpotifyConnectionPageUrl)
        }),
    });

    if (response.status === 409) {
        mantineInformationNotification('Your account is already connected to a Spotify account');
        throw new Error(await response.text());
    } else if (!response.ok) {
        mantineErrorNotification('An error occurred while linking your Spotify account');
        throw new Error(await response.text());
    }
}

export async function fetchSpotifyAppClientId(): Promise<string> {
    const response = await fetch(`${API_URL}/spotify/app/clientId`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
    });

    const responseText = await response.text();

    if (!response.ok) {
        mantineErrorNotification('Failed to fetch Spotify app client ID');
        throw new Error(responseText);
    }

    return responseText;
}

export async function deleteSpotifyCredential() {
    try {
        fetch(`${API_URL}/accounts/clearProfile`, {
            method: 'POST',
            headers: authorizedHeaders()
        }).then(r => {
            mantineSuccessNotification('Spotify account successfully disconnected');
        });
    } catch (error) {
        mantineErrorNotification('Failed to disconnect your Spotify account');
        console.error('Error deleting Spotify credential:', error);
    }
}