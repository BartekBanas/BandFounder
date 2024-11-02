import React, { useState, useEffect } from "react";
import { accessSpotifyConnection } from "../components/spotiftConnection/spotifyConnection";

export function SpotifyConnection() {
    const [dots, setDots] = useState("");

    useEffect(() => {
        const interval = setInterval(() => {
            setDots(prevDots => {
                if (prevDots.length >= 3) {
                    return "";
                }
                return prevDots + ".";
            });
        }, 500);

        accessSpotifyConnection();

        return () => clearInterval(interval);
    }, []);

    const handleConnect = () => {
        accessSpotifyConnection();
    };

    return (
        <div className="App">
            <h1>Connecting to spotify{dots}</h1>
            <button onClick={handleConnect}>Connect</button>
        </div>
    );
}