import {SpotifyAppCredentialService} from "./spotifyAppCredentialService";
import {API_URL} from "../../config";

const configLoader = new SpotifyAppCredentialService();

const BaseAppUrl = "http://localhost:3000/";
const SpotifyConnectionPageUrl = BaseAppUrl + "spotifyConnection/callback/"; // Local frontend url

const SpotifyAuthorizeUrl = "https://accounts.spotify.com/authorize"
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

export function accessSpotifyConnection() {
    let code = getSpotifyAuthorizationCode();
    if (code) {
        fetchAccessToken(code);
    }
}

function getSpotifyAuthorizationCode() {
    let code = null;
    const query = window.location.search;
    if (query.length > 0) {
        const urlParams = new URLSearchParams(query);
        code = urlParams.get('code')
    }

    console.log('Code:', code);

    return code;
}

function fetchAccessToken(code: string) {
    let body = "grant_type=authorization_code";
    body += "&code=" + code;
    body += "&redirect_uri=" + encodeURI(SpotifyConnectionPageUrl);
    body += "&client_id=" + configLoader.clientId;
    body += "&client_secret=" + configLoader.clientSecret;
    callAuthorizationApi(body)
        // .then(_ => window.location.href = BaseAppUrl);
}

async function callAuthorizationApi(body: any) {
    const headers = new Headers();
    headers.append('Content-Type', 'application/x-www-form-urlencoded');
    headers.append('Authorization', 'Basic ' + btoa(configLoader.clientId + ":" + configLoader.clientSecret));

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

function handleAuthorizationResponse(data: any): void {
    console.log('Authorization response:', data);

    if (data.access_token !== undefined) {
        localStorage.setItem("access_token", data.access_token);
        console.log("Access token stored:", data.access_token);
    } else {
        console.log("No access token in response");
    }

    if (data.refresh_token !== undefined) {
        localStorage.setItem("refresh_token", data.refresh_token);
        console.log("Refresh token stored:", data.refresh_token);
    } else {
        console.log("No refresh token in response");
    }

    submitAuthorizationRequest(data.access_token, data.refresh_token, data.expires_in)
        .then(_ => console.log('Authorization request submitted'));
}

async function submitAuthorizationRequest(accessToken: string, refreshToken: string, duration: number) {
    console.log('Submitting authorization request');
    console.log('Access token:', accessToken, 'Refresh token:', refreshToken, 'Duration:', duration);
    const response = await fetch(`${API_URL}/spotifyBroker/connect`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
        body: JSON.stringify({
            accessToken: accessToken,
            refreshToken: refreshToken,
            duration: duration
        })
    });

    const responseText = await response.text();

    if (!response.ok) {
        throw new Error(responseText);
    }
}