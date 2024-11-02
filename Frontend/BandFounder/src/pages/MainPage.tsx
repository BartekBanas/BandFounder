import SpotifyAuthorizationButton from "../components/spotiftConnection/SpotifyAuthorizationButton";
import React, { useEffect, useState } from "react";
import SpotifyDeleteCredentialButton from "../components/spotiftConnection/spotifyDeleteCredentialButton";
import { isUserSpotifyConnected } from "../components/spotiftConnection/spotifyConnection";
import { useNavigate } from "react-router-dom";

export function MainPage() {
    const [isConnectedToSpotify, setIsConnectedToSpotify] = useState<boolean>(false);
    const [loading, setLoading] = useState<boolean>(true);
    const navigate = useNavigate();

    useEffect(() => {
        const checkSpotifyConnection = async () => {
            const isConnected = await isUserSpotifyConnected();
            setIsConnectedToSpotify(isConnected);
            setLoading(false);
        };

        checkSpotifyConnection();
    }, [navigate]);

    if (loading) {
        return <div className="App-header"><h1>Loading...</h1></div>;
    }

    return (
        <div className="App-header">
            {isConnectedToSpotify ? (
                <>
                    <h1>You are logged in (also on Spotify)</h1>
                    <SpotifyDeleteCredentialButton />
                </>
            ) : (
                <>
                    <h1>You are logged in</h1>
                    <SpotifyAuthorizationButton />
                </>
            )}
        </div>
    );
}