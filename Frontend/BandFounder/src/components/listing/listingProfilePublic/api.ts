import Cookies from "universal-cookie";
import {API_URL} from "../../../config";
import {getAccountByUsername} from "../../../api/account";

export const getListing = async (listingId: string) => {
    try {
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/listings/${listingId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.json();
    } catch (e) {
        console.error('Error getting listing:', e);
    }
}

export const getListings = async (username: string) => {
    try {
        const jwt = new Cookies().get('auth_token');
        const targetUser = await getAccountByUsername(username);
        const response = await fetch(`${API_URL}/accounts/${targetUser.id}/listings`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.json();
    } catch (e) {
        console.error('Error getting listings:', e);
    }
}