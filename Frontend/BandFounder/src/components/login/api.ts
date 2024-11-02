import { useCookies } from "react-cookie";
import { API_URL } from "../../config";
import Swal from "sweetalert2";

const headersCreator = (jwt:string) => {
    return {
        'Authorization' : `Bearer ${jwt}`,
        'Content-Type': 'application/json',
    };
}

export const useLoginApi = () => {
    const authCookieName = 'auth_token';
    const [, setCookie] = useCookies([authCookieName]);
    const tokenName = 'auth_token';
    const[cookies] = useCookies([tokenName]);
    return async (usernameOrEmail: string, password: string) => {
        try {
            const response = await fetch(`${API_URL}/account/authenticate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*',
                },
                body: JSON.stringify({
                    usernameOrEmail: usernameOrEmail,
                    password: password,
                }),
            });

            const JWT = await response.text();
            if (!response.ok) {
                Swal.fire({
                    title: 'Error!',
                    text: JWT,
                    icon: 'error',
                    confirmButtonText: 'Confirm'
                });
                throw new Error(JWT);
            }
            setCookie(authCookieName, JWT, {
                expires: new Date(Date.now() + 1000 * 60 * 15), // 15 minutes
                sameSite: true,
            });

            console.log(`Logged in successfully ${JWT}`);
        } catch (error) {
            console.error('Login error:', error);
        }
    };
};