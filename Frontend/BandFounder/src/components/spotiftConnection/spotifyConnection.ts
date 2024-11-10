import {SpotifyAppCredentialService} from "./spotifyAppCredentialService";
import {API_URL} from "../../config";
import Cookies from "universal-cookie";
import {
    mantineErrorNotification,
    mantineInformationNotification,
    mantineSuccessNotification
} from "../common/mantineNotification";

let configLoader = new SpotifyAppCredentialService();
const BaseAppUrl = "http://localhost:3000/";
const SpotifyConnectionPageUrl = BaseAppUrl + "spotifyConnection/callback/";
const SpotifyAuthorizeUrl = "https://accounts.spotify.com/authorize";
const SpotifyFetchTokenUrl = "https://accounts.spotify.com/api/token";

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
        let body = "grant_type=authorization_code";

        body += "&code=" + code;
        body += "&redirect_uri=" + encodeURI(SpotifyConnectionPageUrl);

        body += "&client_id=" + localStorage.getItem('client_id');
        body += "&client_secret=" + localStorage.getItem('client_secret');

        try {
            const spotifyTokens = await getSpotifyTokens(body);
            await authorizeSpotifyAccountInBandfounder(spotifyTokens);
            await linkAccountWithSpotifyArtists();
        } catch (error: any) {
            throw new Error(error.message);
        }
        mantineSuccessNotification('Linking your Spotify account was successful');
    }
}

async function getSpotifyTokens(body: string): Promise<spotifyTokens> {
    const headers = new Headers();
    headers.append('Content-Type', 'application/x-www-form-urlencoded');
    headers.append('Authorization', 'Basic ' + btoa(localStorage.getItem('client_id') + ":" + localStorage.getItem('client_secret')));

    const response = await fetch(SpotifyFetchTokenUrl, {
        method: 'POST',
        headers: headers,
        body: body,
    });

    if (!response.ok) {
        const errorText = await response.text();
        mantineErrorNotification('Failed to request tokens from Spotify');
        throw new Error(errorText);
    }

    const responseContent = await response.json();
    return responseContent as spotifyTokens;
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
