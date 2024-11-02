import SpotifyAuthorizationButton from "../components/spotiftConnection/SpotifyAuthorizationButton";
import React, { useEffect, useState } from "react";
import { AuthorizationButton } from "../components/spotiftConnection/SpotifyConnectionButton";
import SpotifyDeleteCredentialButton from "../components/spotiftConnection/spotifyDeleteCredentialButton";
import useSpotifyConnected from "../hooks/useSpotifyConnected";

export function MainPage() {
    const [isConnectedToSpotify, setIsConnectedToSpotify] = useState(false);
    const checkConnection = useSpotifyConnected; // No direct call here

    useEffect(() => {
        const interval = setInterval(() => {
            async function fetchConnectionStatus() {
                const firstCheck = await checkConnection(); // First call
                setIsConnectedToSpotify(firstCheck);

                if (!firstCheck) {
                    const secondCheck = await checkConnection(); // Second call if first is false
                    setIsConnectedToSpotify(secondCheck);
                }
            }
            fetchConnectionStatus();
        }, 500);
    }, []);

    return (
        <div className="App-header">
            {isConnectedToSpotify ? (
                <>
                    <h1>You are logged in faggot (also on spotify)</h1>
                    <SpotifyDeleteCredentialButton />
                </>
            ) : (
                <>
                    <h1>You are logged in faggot</h1>
                    <SpotifyAuthorizationButton/>
                </>
            )}
        </div>
    );
}