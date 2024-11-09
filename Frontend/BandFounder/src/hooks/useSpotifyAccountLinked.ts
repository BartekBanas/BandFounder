import Cookies from 'universal-cookie';
import {API_URL} from "../config";

const useSpotifyAccountLinked = async (): Promise<boolean> => {
    const cookies = new Cookies();
    const authToken = cookies.get('auth_token');

    const response = await fetch(`${API_URL}/spotifyBroker/tokens`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*',
            'Authorization': `Bearer ${authToken}`
        },
    });

    if (response.ok) {
        return true;
    } else if (response.status === 401) {
        return false;
    } else {
        throw new Error(await response.text());
    }
};

export default useSpotifyAccountLinked;