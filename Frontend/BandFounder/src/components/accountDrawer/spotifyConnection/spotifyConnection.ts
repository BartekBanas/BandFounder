import {SpotifyAppCredentialService} from "./spotifyAppCredentialService";
import {API_URL} from "../../../config";
import Cookies from "universal-cookie";
import {
    mantineErrorNotification,
    mantineInformationNotification,
    mantineSuccessNotification
} from "../../common/mantineNotification";
import {authorizedHeaders} from "../../../hooks/authentication";

let configLoader = new SpotifyAppCredentialService();
const BaseAppUrl = "http://localhost:3000/";
const SpotifyConnectionPageUrl = BaseAppUrl + "spotifyConnection/callback/";
const SpotifyAuthorizeUrl = "https://accounts.spotify.com/authorize";

export type spotifyTokens = {
    access_token: string
    refresh_token: string
    expires_in: number
}

export function redirectToSpotifyAuthorizationPage() {
    let url = SpotifyAuthorizeUrl;
    url += "?client_id=" + configLoader.clientId;
    url += "&response_type=code";
    url += "&redirect_uri=" + encodeURI(SpotifyConnectionPageUrl);
    url += "&show_dialog=true";
    url += "&scope=user-top-read user-follow-read";
    window.location.href = url; // Show Spotify's authorization screen
}

export async function linkAccountWithSpotifyFromCode(): Promise<void> {
    const code = new URLSearchParams(window.location.search).get('code');
    if (code) {
        try {
            await sendCodeToBackend(code);
            mantineSuccessNotification('Linking your Spotify account was successful');
        } catch (error: any) {
            mantineErrorNotification('Failed to link Spotify account');
            throw new Error(error.message);
        }
    }
}

async function sendCodeToBackend(code: string): Promise<void> {
    const response = await fetch(`${API_URL}/spotifyBroker/connect`, {
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

async function authorizeSpotifyAccountInBandfounder(tokens: spotifyTokens) {
    const jwt = new Cookies().get('auth_token');

    // mantineErrorNotification('Poopy');
    // throw new Error();

    const response = await fetch(`${API_URL}/spotifyBroker/connect`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${jwt}`
        },
        body: JSON.stringify({
            accessToken: tokens.access_token,
            refreshToken: tokens.refresh_token,
            duration: tokens.expires_in
        })
    });

    if (response.status === 409) {
        mantineInformationNotification('Your account is already connected to a Spotify account');
        throw new Error(await response.text());
    } else if (!response.ok) {
        mantineErrorNotification('An error occurred while linking your Spotify account');
        throw new Error(await response.text());
    }

    console.log('Authorization request submitted');
}

async function linkAccountWithSpotifyArtists() {
    const jwt = new Cookies().get('auth_token');
    const response = await fetch(`${API_URL}/spotifyBroker/artists`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${jwt}`
        }
    });

    if (!response.ok) {
        mantineErrorNotification('An error occurred while linking your favourite Spotify artists');
        throw new Error(await response.text());
    }
}

export async function deleteSpotifyCredential() {
    const jwt = new Cookies().get('auth_token');
    try {
        fetch(`${API_URL}/accounts/clearProfile`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        }).then(r => {
            mantineSuccessNotification('Spotify account successfully disconnected');
        });
    } catch (error) {
        mantineErrorNotification('Failed to disconnect your Spotify account');
        console.error('Error deleting Spotify credential:', error);
    }
}
