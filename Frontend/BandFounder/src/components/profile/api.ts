// src/components/profile/api.ts
import Cookies from "universal-cookie";
import { API_URL } from "../../config";
import { Account } from "../../types/Account";

const MAX_GENRES_RETURNED = 10;

export const getGUID = async (username: string) => {
    try {
        const response = await fetch(`${API_URL}/account?username=${username}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${new Cookies().get('auth_token')}`
            }
        });
        if (!response.ok) {
            throw new Error(await response.text());
        }
        const account: Account = await response.json();
        return account.id;
    } catch (error) {
        console.error('Error getting GUID:', error);
    }
};

export const getAccount = async (guid: string) => {
    try {
        const response = await fetch(`${API_URL}/account/${guid}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${new Cookies().get('auth_token')}`
            }
        });
        if (!response.ok) {
            throw new Error(await response.text());
        }
        // console.log('response:', response);
        return await response.json();
    } catch (error) {
        console.error('Error getting account:', error);
    }
};

export const getTopArtists = async (guid: string) => {
    try{
        const response = await fetch(`${API_URL}/account/${guid}/artists/top`, {
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
    }
    catch(error){
        console.error('Error getting top artists:', error);
    }
}

export const getTopGenres = async (guid: string): Promise<string[]> => {
    try {
        const response = await fetch(`${API_URL}/account/${guid}/genres`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${new Cookies().get('auth_token')}`
            }
        });
        if (!response.ok) {
            throw new Error(await response.text());
        }
        const genres: string[] = await response.json();
        return genres.slice(0, MAX_GENRES_RETURNED);
    } catch (error) {
        console.error('Error getting top genres:', error);
        return [];
    }
}