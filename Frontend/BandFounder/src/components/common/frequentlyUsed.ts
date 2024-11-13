import { API_URL } from "../../config";
import Cookies from "universal-cookie";
import {Account} from "../../types/Account";

const getUser = async (guid: string) => {
    try {
        const response = await fetch(`${API_URL}/accounts/${guid}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${new Cookies().get('auth_token')}`
            }
        });

        return await response.json();
    } catch (error) {
        console.log(error);
    }
}

export const getCurrentUser = async ()  => {
    try {
        const response = await fetch(`${API_URL}/accounts/me`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${new Cookies().get('auth_token')}`
            }
        });
        return response.json();
    } catch (error) {
        console.log(error);
    }
}

export default getUser;
