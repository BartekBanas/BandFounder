import { API_URL } from "../../config";
import Cookies from "universal-cookie";

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

export default getUser;