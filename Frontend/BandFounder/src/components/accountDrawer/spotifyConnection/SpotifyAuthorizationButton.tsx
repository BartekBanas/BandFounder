import React from 'react';
import { Button } from '@mui/material';
import {redirectToSpotifyAuthorizationPage} from "./spotifyConnection";
import {SpotifyIcon} from "./SpotifyIcon";
import {muiDarkTheme} from "../../../assets/muiDarkTheme";

function SpotifyAuthorizationButton() {
    return (
        <Button
            variant="contained"
            sx={{
                backgroundColor: muiDarkTheme.palette.success.dark,
                color: muiDarkTheme.palette.success.contrastText,
                '&:hover': {
                    backgroundColor: muiDarkTheme.palette.success.dark,
                },
            }}
            onClick={redirectToSpotifyAuthorizationPage}
            startIcon={<SpotifyIcon />}
        >
            Link Spotify Account
        </Button>
    );
}

export default SpotifyAuthorizationButton;
