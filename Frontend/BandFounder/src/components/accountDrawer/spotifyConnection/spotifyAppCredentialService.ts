import {API_URL} from "../../../config";

export class SpotifyAppCredentialService {
    private _clientId: string | null = null;

    constructor() {
        this.fetchCredentials();
    }

    get clientId(): string {
        if (!this._clientId) {
            throw new Error("Client ID is not loaded");
        }
        return this._clientId;
    }

    async fetchCredentials(): Promise<void> {
        const response = await fetch(`${API_URL}/spotifyBroker/clientId`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
        });

        const responseText = await response.text();

        if (!response.ok) {
            throw new Error(responseText);
        }

        localStorage.setItem('client_id', responseText);
        this._clientId = responseText;
    }
}