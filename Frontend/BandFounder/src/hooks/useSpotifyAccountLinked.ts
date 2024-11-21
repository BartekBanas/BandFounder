import {API_URL} from "../config";
import {authorizedHeaders} from "./authentication";
import {mantineErrorNotification} from "../components/common/mantineNotification";

async function useSpotifyAccountLinked(): Promise<boolean> {
    const response = await fetch(`${API_URL}/spotify/tokens`, {
        method: 'GET',
        headers: authorizedHeaders()
    });

    if (response.ok) {
        return true;
    } else if (response.status === 401) {
        return false;
    } else {
        mantineErrorNotification('Failed to verify Spotify account connection');
        throw new Error('Failed to verify Spotify account connection');
    }
}

export default useSpotifyAccountLinked;