import Cookies from 'universal-cookie';
import { API_URL } from "../config";

const useSpotifyConnected = async () => {
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
};

export default useSpotifyConnected;