import React from 'react';
import {redirectToSpotifyAuthorizationPage} from "./spotifyConnection";
import {SpotifyIcon} from "./SpotifyIcon";
import {Button} from "@mantine/core";

function SpotifyAuthorizationButton() {
    return (
        <Button
            leftSection={<SpotifyIcon />}
            size="md"
            color="green"
            onClick={redirectToSpotifyAuthorizationPage}
        >
            Link Spotify Account
        </Button>
    );
}

export default SpotifyAuthorizationButton;
