import SpotifyAuthorizationButton from "../components/spotiftConnection/SpotifyAuthorizationButton";
import React from "react";

export function MainPage() {
    return (
        <div className="App-header">
            <SpotifyAuthorizationButton/>
        </div>
    );
}