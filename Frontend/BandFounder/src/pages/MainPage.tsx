import SpotifyAuthorizationButton from "../components/spotiftConnection/SpotifyAuthorizationButton";
import React from "react";
import {AuthorizationButton} from "../components/spotiftConnection/SpotifyConnectionButton";
import {SpotifyDeleteCredentialButton} from "../components/spotiftConnection/spotifyDeleteCredentialButton";

export function MainPage() {
    return (
        <div className="App-header">
            <h1>You are logged in faggot</h1>
            <SpotifyAuthorizationButton/>
            <AuthorizationButton/>
            <SpotifyDeleteCredentialButton/>
        </div>
    );
}