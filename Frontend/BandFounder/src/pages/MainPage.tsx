import SpotifyAuthorizationButton from "../components/spotiftConnection/SpotifyAuthorizationButton";
import React from "react";
import {AuthorizationButton} from "../components/spotiftConnection/SpotifyConnectionButton";

export function MainPage() {
    return (
        <div className="App-header">
            <h1>You are logged in faggot</h1>
            <SpotifyAuthorizationButton/>
            <AuthorizationButton/>
        </div>
    );
}