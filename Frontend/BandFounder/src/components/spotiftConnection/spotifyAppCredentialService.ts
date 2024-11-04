import {API_URL} from "../../config";

export interface Credentials {
    clientId: string;
    clientSecret: string;
}

export class SpotifyAppCredentialService {
    private _clientId: string | null = null;
    private _clientSecret: string | null = null;

    constructor() {
        this.fetchCredentials().then(_ => console.log("Credentials loaded\n" + "client_id: " + this.clientId + "\nclient_secret: " + this.clientSecret));
    }

    get clientId(): string {
        if (!this._clientId) {
            throw new Error("Client ID is not loaded");
        }
        return this._clientId;
    }

    get clientSecret(): string {
        if (!this._clientSecret) {
            throw new Error("Client Secret is not loaded");
        }
        return this._clientSecret;
    }

    async fetchCredentials(): Promise<void> {
        console.log("Base API: " + API_URL);

        const response = await fetch(`${API_URL}/spotifyBroker/credentials`, {
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

        const credentials: Credentials = JSON.parse(responseText);
        localStorage.setItem('client_id', credentials.clientId);
        localStorage.setItem('client_secret', credentials.clientSecret);
        this._clientId = credentials.clientId;
        this._clientSecret = credentials.clientSecret;
    }
}