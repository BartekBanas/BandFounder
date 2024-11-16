import { API_URL } from "../../config";
import Cookies from "universal-cookie";

export const getCurrentUser = async () => {
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

export const getUserByName = async (name: string) => {
    try {
        const response = await fetch(`${API_URL}/accounts?username=${name}`, {
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

export const getUserById = async (id: string) => {
    try {
        const response = await fetch(`${API_URL}/accounts/${id}`, {
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