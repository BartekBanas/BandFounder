import {API_URL} from "../../config";
import Cookies from "universal-cookie";

export async function updateAccountRequest(name: string | null, password: string | null, email: string | null) {
    const requestBody: { name?: string; password?: string; email?: string } = {};

    requestBody.name = name ?? undefined;
    requestBody.password = password ?? undefined;
    requestBody.email = email ?? undefined;

    const token = new Cookies().get('auth_token');

    const response = await fetch(`${API_URL}/accounts/me`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(requestBody),
    });

    return response;
}

export async function deleteAccountRequest() {
    const token = new Cookies().get('auth_token');

    const response = await fetch(`${API_URL}/accounts/me`, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        }
    });

    removeAuthToken();

    return response;
}

export function removeAuthToken() {
    new Cookies().remove('auth_token');
}