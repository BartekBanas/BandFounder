import { SpotifyAppCredentialService } from "./spotifyAppCredentialService";
import { API_URL } from "../../config";
import Cookies from "universal-cookie";

const configLoader = new SpotifyAppCredentialService();
const BaseAppUrl = "http://localhost:3000/";
const SpotifyConnectionPageUrl = BaseAppUrl + "spotifyConnection/callback/";
const SpotifyAuthorizeUrl = "https://accounts.spotify.com/authorize";
const SpotifyFetchTokenUrl = "https://accounts.spotify.com/api/token";

export function requestAuthorization() {
    const url = `${SpotifyAuthorizeUrl}?client_id=${configLoader.clientId}&response_type=code&redirect_uri=${encodeURIComponent(SpotifyConnectionPageUrl)}&show_dialog=true&scope=user-top-read user-follow-read`;
    window.location.href = url;
}

export function accessSpotifyConnection() {
    const code = new URLSearchParams(window.location.search).get('code');
    if (code) {
        fetchAccessToken(code);
    }
}

async function fetchAccessToken(code:string) {
    const body = `grant_type=authorization_code&code=${code}&redirect_uri=${encodeURIComponent(SpotifyConnectionPageUrl)}&client_id=${configLoader.clientId}&client_secret=${configLoader.clientSecret}`;

    try {

        const response = await fetch(SpotifyFetchTokenUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'Authorization': `Basic ${btoa(configLoader.clientId + ":" + configLoader.clientSecret)}`
            },
            body: body
        });

        if (!response.ok) {
            console.error('Authorization failed:', await response.text());
            return;
        }

        const data = await response.json();
        handleAuthorizationResponse(data);
    } catch (error) {
        console.error('Error calling authorization API:', error);
    }
}

function handleAuthorizationResponse(data:any) {
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

async function submitAuthorizationRequest(accessToken:string, refreshToken:string, duration:string) {
    const jwt = new Cookies().get('auth_token');
    const spotifyCode = new Cookies().get('spotifyCode');
    console.log(123);
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