import {SpotifyAppCredentialService} from "./spotifyAppCredentialService";
import {API_URL} from "../../config";
import Cookies from "universal-cookie";

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

        const spotifyTokens = await getSpotifyTokens(body);
        await authorizeSpotifyAccountInBandfounder(spotifyTokens);
        await linkAccountWithSpotifyArtists();
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
        console.log('Requesting tokens from Spotify failed: ', errorText);
        throw new Error(errorText);
    }

    const responseContent = await response.json();
    return responseContent as spotifyTokens;
}

async function authorizeSpotifyAccountInBandfounder(tokens: spotifyTokens) {
    const jwt = new Cookies().get('auth_token');

    try {
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

        if (!response.ok) {
            throw new Error(await response.text());
        }

        console.log('Authorization request submitted');
    } catch (error) {
        console.error('Error submitting authorization request:', error);
    }
}

async function linkAccountWithSpotifyArtists() {
    const jwt = new Cookies().get('auth_token');
    try {
        const response = await fetch(`${API_URL}/spotifyBroker/artists`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }
    } catch (error) {
        console.error('Error linking account with its Spotify artists:', error);
    }
}

export function deleteSpotifyCredential() {
    const jwt = new Cookies().get('auth_token');
    try {
        fetch(`${API_URL}/account/clearProfile`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        }).then(r => {
            console.log('Spotify credential deleted');
            window.location.reload();
        });
    } catch (error) {
        console.error('Error deleting Spotify credential:', error);
    }
}
