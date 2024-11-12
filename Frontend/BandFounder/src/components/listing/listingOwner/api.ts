import Cookies from "universal-cookie";
import {API_URL} from "../../../config";

export const getListing = async (listingId: string) => {
    try{
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/listings/${listingId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.json();
    }
    catch (e) {
        console.error('Error getting listing:', e);
    }
}

export const getListings = async () => {
    try{
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/listings/me`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.json();
    }
    catch (e) {
        console.error('Error getting listings:', e);
    }
}

export const getGenres = async () => {
    try{
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/genres`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.json();
    }
    catch (e) {
        console.error('Error getting genres:', e);
    }
}

export const getMusicianRoles = async () => {
    try{
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/roles`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.json();
    }
    catch (e) {
        console.error('Error getting musician roles:', e);
    }
}

export const updateListing = async (listing: any, id:string) => {
    try{
        const jwt = new Cookies().get('auth_token');
        if(listing.type === 'Band'){
            listing.type = '0';
        }
        else {
            listing.type = '1';
        }
        console.log('listing:', listing);
        const response = await fetch(`${API_URL}/listings/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            },
            body: JSON.stringify(listing)
        });
        return await response.json();
    }
    catch (e) {
        console.error('Error updating listing:', e);
    }
}