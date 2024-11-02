import { useCookies } from "react-cookie";
import { API_URL } from "../../config";
import Swal from "sweetalert2";

export const useRegisterApi = () => {
    const authCookieName = 'auth_token';
    const [, setCookie] = useCookies([authCookieName]);

    return async(name: string, email: string, password: string) => {
        try{
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

            const JWT : string = await response.text();

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

        }
        catch(error){
            console.log(`Error: ${error}`);
        }
    }
}