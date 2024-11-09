import {useCookies} from "react-cookie";
import {API_URL} from "../../config";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";

export const useRegisterApi = () => {
    const authCookieName = 'auth_token';
    const [, setCookie] = useCookies([authCookieName]);

    return async (name: string, email: string, password: string) => {
        const response = await fetch(`${API_URL}/account/`, {
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

        if (response.ok) {
            mantineSuccessNotification('Account created successfully');

            setCookie(authCookieName, responseContent, {
                expires: new Date(Date.now() + 1000 * 60 * 15), // 15 minutes
                sameSite: true,
            });
        } else {
            mantineErrorNotification(responseContent);
            throw new Error(responseContent);
        }
    }
}