import React from 'react';
import Button from '@mui/material/Button';
import {redirectToSpotifyAuthorizationPage} from "./spotifyConnection";

function SpotifyAuthorizationButton() {
    return (
        <Button
            variant="contained"
            color="primary"
            onClick={redirectToSpotifyAuthorizationPage}
        >
            Request Authorization
        </Button>
    );
}

export default SpotifyAuthorizationButton;
