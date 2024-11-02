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

        try {
            accessSpotifyConnection();
            navigate("/home"); // Navigate to /home after accessing Spotify connection
        } catch (Exception) {
            console.log(Exception);
        }

        return () => clearInterval(interval);
    }, [navigate]);

    const handleConnect = () => {
        accessSpotifyConnection();
        navigate("/home"); // Navigate to /home on button click
    };

    return (
        <div className="App">
            <h1>Connecting to spotify{dots}</h1>
            <button onClick={handleConnect}>Connect</button>
        </div>
    );
}