import {useCookies} from "react-cookie";
import {API_URL} from "../../config";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {Account} from "../../types/Account";

export interface RegisterFormType {
    Name: string;
    Password: string;
    Email: string;
}

export const useRegisterApi = () => {
    const authCookieName = 'auth_token';
    const userIdCookieName = 'user_id';
    const [, setCookie] = useCookies([authCookieName]);
    const [, setUserIdCookie] = useCookies([userIdCookieName]);

    return async (name: string, email: string, password: string) => {
        const response = await fetch(`${API_URL}/accounts/`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
            },
            body: JSON.stringify({
                name: name,
                email: email,
                password: password
            }),
        });
        const responseContent: string = await response.text();

        const response2 = await fetch(`${API_URL}/accounts/me`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Authorization': `Bearer ${responseContent}`
            },
        });

        const account: Account = await response2.json();

        if (response.ok) {
            mantineSuccessNotification('Account created successfully');

            setCookie(authCookieName, responseContent, {
                expires: new Date(Date.now() + 1000 * 60 * 15), // 15 minutes
                sameSite: true,
            });
            setUserIdCookie(userIdCookieName, account.id,
            {
                expires: new Date(Date.now() + 1000 * 60 * 15), // 15 minutes
                sameSite: true,
            });
        } else {
            mantineErrorNotification(responseContent);
            throw new Error(responseContent);
        }
    }
}