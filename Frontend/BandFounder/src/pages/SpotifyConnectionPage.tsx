import {Center, Loader} from "@mantine/core";
import React, {useEffect} from "react";
import {useNavigate} from "react-router-dom";
import {linkAccountWithSpotifyFromCode} from "../components/spotiftConnection/spotifyConnection";

export function SpotifyConnectionPage() {
    const navigate = useNavigate();

    useEffect(() => {
        const connectAccountWithSpotify = async () => {
            try {
                await linkAccountWithSpotifyFromCode();
                navigate("/home"); // Navigate to /home after linking account to Spotify
            } catch (error) {
                console.log(error);
            }
        };

        connectAccountWithSpotify();
    }, [navigate]);

    const handleConnect = async () => {
        try {
            await linkAccountWithSpotifyFromCode();
            navigate("/home");
        } catch (error) {
            console.log(error);
        }
    };

    return (
        <div className="App">
            <Center style={{minHeight: '100vh'}}>
                <Loader size={100}/>
            </Center>
        </div>
    );
}