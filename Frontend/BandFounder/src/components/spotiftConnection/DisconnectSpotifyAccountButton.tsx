import React from 'react';
import {Button} from "@mantine/core";
import {SpotifyIcon} from "./SpotifyIcon";

interface SpotifyDeleteCredentialButtonProps {
    onDelete: () => void;
}

export default function DisconnectSpotifyAccountButton({ onDelete }: SpotifyDeleteCredentialButtonProps) {
    return (
        <Button
            leftSection={<SpotifyIcon />}
            size="md"
            color="red"
            onClick={onDelete}
        >
            Disconnect with Spotify
        </Button>
    );
}