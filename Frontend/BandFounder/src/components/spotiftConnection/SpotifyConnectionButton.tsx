import React from 'react';
import Button from '@mui/material/Button';
import {linkAccountWithSpotifyFromCode} from './spotifyConnection';

export function AuthorizationButton() {
    return (
        <Button
            variant="contained"
            color="secondary"
            onClick={linkAccountWithSpotifyFromCode}
        >
            Confirm Spotify Connection
        </Button>
    );
}
