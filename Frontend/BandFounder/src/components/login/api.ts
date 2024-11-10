import {API_URL} from "../../config";
import {mantineErrorNotification, mantineInformationNotification} from "../common/mantineNotification";
import {setAuthToken, setUserId} from "../../hooks/authentication";

type AccountDto = {
    id: string;
    name: string;
    email: string;
}

export const useLoginApi = () => {
    return async (usernameOrEmail: string, password: string) => {
        const authorizationToken = await login(usernameOrEmail, password);
        const account = await getMyAccount(authorizationToken);

        setAuthToken(authorizationToken);
        setUserId(account.id);
        mantineInformationNotification(`Welcome ${account.name}`);
    };
};

async function login(usernameOrEmail: string, password: string) {
    const response = await fetch(`${API_URL}/accounts/authenticate`, {
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

    const responseContent = await response.text();

    if (!response.ok) {
        mantineErrorNotification("Login failed");
        throw new Error(responseContent);
    }

    return responseContent;
}

async function getMyAccount(token: string): Promise<AccountDto> {
    const response = await fetch(`${API_URL}/accounts/me`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        }
    });

    if (!response.ok) {
        mantineErrorNotification('Failed to fetch account details');
        throw new Error('Failed to fetch account details');
    }

    const account: AccountDto = await response.json();
    return account;
}