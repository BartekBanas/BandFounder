import { SpotifyAppCredentialService } from "./spotifyAppCredentialService";
import { API_URL } from "../../config";
import Cookies from "universal-cookie";

var configLoader = new SpotifyAppCredentialService();
const BaseAppUrl = "http://localhost:3000/";
const SpotifyConnectionPageUrl = BaseAppUrl + "spotifyConnection/callback/";
const SpotifyAuthorizeUrl = "https://accounts.spotify.com/authorize";
const SpotifyFetchTokenUrl = "https://accounts.spotify.com/api/token";

export function requestAuthorization() {
    let url = SpotifyAuthorizeUrl;
    url += "?client_id=" + configLoader.clientId;
    url += "&response_type=code";
    url += "&redirect_uri=" + encodeURI(SpotifyConnectionPageUrl);
    url += "&show_dialog=true";
    url += "&scope=user-top-read user-follow-read";
    window.location.href = url; // Show Spotify's authorization screen
}

export async function isUserSpotifyConnected(){
    const cookies = new Cookies();
    const auth_token = cookies.get('auth_token');
    try {
        const response = await fetch(`${API_URL}/spotifyBroker/tokens`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Authorization': `Bearer ${auth_token}`
            },
        });
        if (!response.ok) {
            throw new Error(await response.text());
        }
        return true;
    } catch (Exception) {
        console.error(Exception);
        return false;
    }
}

export function accessSpotifyConnection() {
    const code = new URLSearchParams(window.location.search).get('code');
    if (code) {
        fetchAccessToken(code);
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
        });
        console.log('Spotify credential deleted');
        window.location.reload();
    } catch (error) {
        console.error('Error deleting Spotify credential:', error);
    }
}
function fetchAccessToken(code: string) {
    configLoader = new SpotifyAppCredentialService();

    let body = "grant_type=authorization_code";

    body += "&code=" + code;
    body += "&redirect_uri=" + encodeURI(SpotifyConnectionPageUrl);

    body += "&client_id=" + localStorage.getItem('client_id');
    body += "&client_secret=" + localStorage.getItem('client_secret');

    callAuthorizationApi(body)
    // .then(_ => window.location.href = BaseAppUrl);
}

async function callAuthorizationApi(body: any) {
    const headers = new Headers();
    headers.append('Content-Type', 'application/x-www-form-urlencoded');
    headers.append('Authorization', 'Basic ' + btoa(localStorage.getItem('client_id') + ":" + localStorage.getItem('client_secret')));

    try {
        const response = await fetch(SpotifyFetchTokenUrl, {
            method: 'POST',
            headers: headers,
            body: body,
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.log('Authorization failed:', errorText);
            return;
        }

        const data = await response.json();
        handleAuthorizationResponse(data);
    } catch (error) {
        console.error('Error calling authorization API:', error);
    }
}

function handleAuthorizationResponse(data: any) {
    console.log('Authorization response:', data);

    if (data.access_token) {
        localStorage.setItem("access_token", data.access_token);
        console.log("Access token stored:", data.access_token);
    } else {
        console.warn("No access token in response");
    }

    if (data.refresh_token) {
        localStorage.setItem("refresh_token", data.refresh_token);
        console.log("Refresh token stored:", data.refresh_token);
    } else {
        console.warn("No refresh token in response");
    }
    submitAuthorizationRequest(data.access_token, data.refresh_token, data.expires_in);
}

async function submitAuthorizationRequest(accessToken: string, refreshToken: string, duration: string) {
    const jwt = new Cookies().get('auth_token');
    const spotifyCode = new Cookies().get('spotifyCode');
    try {
        const response = await fetch(`${API_URL}/spotifyBroker/connect`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            },
            body: JSON.stringify({
                accessToken,
                refreshToken,
                duration
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
