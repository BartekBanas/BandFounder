import { Button } from '@mui/material';
import React from 'react';
import {SpotifyIcon} from "./SpotifyIcon";
import {muiDarkTheme} from "../../../assets/muiDarkTheme";

interface SpotifyDeleteCredentialButtonProps {
    onDelete: () => void;
}

export default function DisconnectSpotifyAccountButton({ onDelete }: SpotifyDeleteCredentialButtonProps) {
    return (
        <Button
            variant="contained"
            sx={{
                backgroundColor: muiDarkTheme.palette.error.main,
                color: muiDarkTheme.palette.error.contrastText,
                '&:hover': {
                    backgroundColor: muiDarkTheme.palette.error.dark,
                },
            }}
            startIcon={<SpotifyIcon />}
            onClick={onDelete}
        >
            Disconnect with Spotify
        </Button>
    );
}