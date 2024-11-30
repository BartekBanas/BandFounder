import {API_URL} from "../../../config";
import {mantineErrorNotification, mantineSuccessNotification} from "../../common/mantineNotification";
import {authorizedHeaders} from "../../../hooks/authentication";
import {fetchSpotifyAppClientId, requestSpotifyAccountLinkFromCode} from "../../../api/spotify";

const BaseAppUrl = "https://bandfounder.com/";
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
            await requestSpotifyAccountLinkFromCode(SpotifyConnectionPageUrl, code);
            mantineSuccessNotification('Linking your Spotify account was successful');
        } catch (error: any) {
            mantineErrorNotification('Failed to link Spotify account');
            throw new Error(error.message);
        }
    }
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