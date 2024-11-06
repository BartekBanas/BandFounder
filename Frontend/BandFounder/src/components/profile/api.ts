// src/components/profile/api.ts
import Cookies from "universal-cookie";
import {API_URL} from "../../config";

export const getProfile = async () => {
    try {
        const response = await fetch(`${API_URL}/account`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${new Cookies().get('auth_token')}`
            }
        });
        if (!response.ok) {
            throw new Error(await response.text());
        }
        return await response.json();
    } catch (error) {
        console.error('Error getting profile:', error);
    }
}

export const getTopArtists = async () => {
    try{
        const response = await fetch(`${API_URL}/spotifyBroker/artists/top`)
    }
    catch(Error){
        console.error('Error getting top artists:', Error);
    }
}