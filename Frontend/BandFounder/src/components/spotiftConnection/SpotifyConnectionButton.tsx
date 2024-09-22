import React from 'react';
import Button from '@mui/material/Button';
import {accessSpotifyConnection} from './spotifyConnection';

export function AuthorizationButton() {
    return (
        <Button
            variant="contained"
            color="secondary"
            onClick={accessSpotifyConnection}
        >
            Confirm Spotify Connection
        </Button>
    );
}
