import React from "react";
import {AuthorizationButton} from "../components/spotiftConnection/SpotifyConnectionButton";

export function SpotifyConnection() {
    return (
        <div className="App">
            <header className="App-header">
                <AuthorizationButton/>
            </header>
        </div>
    );
}