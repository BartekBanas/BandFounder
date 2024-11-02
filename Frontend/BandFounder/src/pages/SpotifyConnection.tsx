import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { accessSpotifyConnection } from "../components/spotiftConnection/spotifyConnection";

export function SpotifyConnection() {
    const [dots, setDots] = useState("");
    const navigate = useNavigate();

    useEffect(() => {
        const interval = setInterval(() => {
            setDots(prevDots => {
                if (prevDots.length >= 3) {
                    return "";
                }
                return prevDots + ".";
            });
        }, 1000);

        const connectToSpotify = async () => {
            try {
                await accessSpotifyConnection();
                navigate("/home"); // Navigate to /home after accessing Spotify connection
            } catch (error) {
                console.log(error);
            }
        };

        connectToSpotify();

        return () => clearInterval(interval);
    }, [navigate]);

    const handleConnect = async () => {
        try {
            await accessSpotifyConnection();
            navigate("/home"); // Navigate to /home on button click
        } catch (error) {
            console.log(error);
        }
    };

    return (
        <div className="App">
            <h1>Connecting to spotify{dots}</h1>
            <button onClick={handleConnect}>Connect</button>
        </div>
    );
}