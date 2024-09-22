import React from 'react';
import Button from '@mui/material/Button';
import {requestAuthorization} from "./spotifyConnection";

function SpotifyAuthorizationButton() {
    return (
        <Button
            variant="contained"
            color="primary"
            onClick={requestAuthorization}
        >
            Request Authorization
        </Button>
    );
}

export default SpotifyAuthorizationButton;
