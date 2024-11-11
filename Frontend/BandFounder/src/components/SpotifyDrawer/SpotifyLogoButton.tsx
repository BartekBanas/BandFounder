// src/components/SpotifyDrawer/SpotifyLogoButton.tsx
import React from "react";
import { Button } from "@mui/material";
import { redirectToSpotifyAuthorizationPage } from "../accountDrawer/spotifyConnection/spotifyConnection";
import "./SpotifyLogoButton.css";

interface SpotifyLogoButtonProps {
    onClick?: React.MouseEventHandler<HTMLButtonElement>;
    className?: string;
}

const SpotifyLogoButton: React.FC<SpotifyLogoButtonProps> = ({ onClick, className }) => {
    return (
        <Button className={`spotify-logo-button ${className}`} onClick={onClick || redirectToSpotifyAuthorizationPage}>
            <span className="visually-hidden">Connect to Spotify</span>
        </Button>
    );
};

export default SpotifyLogoButton;