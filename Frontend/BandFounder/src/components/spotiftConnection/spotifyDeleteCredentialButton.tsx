import React from 'react';
import Button from '@mui/material/Button';

interface SpotifyDeleteCredentialButtonProps {
    onDelete: () => void;
}

export default function SpotifyDeleteCredentialButton({ onDelete }: SpotifyDeleteCredentialButtonProps) {
    return (
        <Button
            variant="contained"
            color="error"
            onClick={onDelete}
        >
            Delete Spotify Credential
        </Button>
    );
}