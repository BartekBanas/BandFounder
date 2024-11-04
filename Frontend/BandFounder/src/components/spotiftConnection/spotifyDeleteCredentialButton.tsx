// src/components/spotiftConnection/spotifyDeleteCredentialButton.tsx
import React from 'react';
import Button from '@mui/material/Button';
import { deleteSpotifyCredential } from './spotifyConnection';

export default function SpotifyDeleteCredentialButton() {
    return (
        <Button
            variant="contained"
            color="error"
            onClick={deleteSpotifyCredential}
        >
            Delete Spotify Credential
        </Button>
    );
};
