import React, {FC, useEffect, useState} from 'react';
import {API_URL} from '../../config';
import {useDisclosure} from "@mantine/hooks";
import {authorizedHeaders, getUserId} from "../../hooks/authentication";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {Autocomplete, Box, Button, Modal, Stack, TextField, Typography} from '@mui/material';
import {muiDarkTheme} from "../../assets/muiDarkTheme";
import {getArtists} from "../../api/metadata";

export const AddArtistModal: FC = () => {
    const [opened, {close, open}] = useDisclosure(false);
    const [artists, setArtists] = useState<string[]>([]);
    const [myArtists, setMyArtists] = useState<string[]>([]);
    const [selectedArtistName, setSelectedArtistName] = useState<string | null>(null);

    useEffect(() => {
        fetchArtists();
        fetchMyArtists();
    }, []);

    const fetchArtists = async (): Promise<void> => {
        try {
            const response = await getArtists();
            setArtists(response.map((artist) => artist.name));
        } catch (error) {
            console.error('Error fetching artists:', error);
        }
    }

    const fetchMyArtists = async (): Promise<void> => {
        try {
            console.log(`My auth token: ${authorizedHeaders()}`);

            const response = await fetch(`${API_URL}/accounts/${getUserId()}/artists`, {
                method: 'GET',
                headers: authorizedHeaders()
            });

            if (!response.ok) {
                mantineErrorNotification('Failed to fetch own artists');
                throw new Error('Failed to fetch own artists');
            }

            const myArtistsResponse: string[] = await response.json();
            setMyArtists(myArtistsResponse);
        } catch (error) {
            console.error('Error fetching own artists:', error);
        }
    }

    const handleAddArtist = async () => {
        if (!selectedArtistName || selectedArtistName.trim() === '') {
            mantineErrorNotification("Please enter an artist's name before adding.");
            return;
        }

        try {
            const response = await fetch(`${API_URL}/accounts/${getUserId()}/artists`, {
                method: 'POST',
                headers: authorizedHeaders(),
                body: JSON.stringify(selectedArtistName)
            });

            if (!response.ok) {
                throw new Error(`Failed to add ${selectedArtistName} to your account`);
            }

            mantineSuccessNotification(`Artist ${selectedArtistName} was added to your account`);
        } catch (error) {
            mantineErrorNotification(`Failed to add ${selectedArtistName} to your account`);
        }

        fetchMyArtists();

        close();
    };

    const filteredAccounts: string[] = artists.filter(artist => !myArtists.includes(artist));

    return (
        <>
            <Button variant="contained" color="success" size="large" onClick={open}>
                Add an artist
            </Button>

            <Modal open={opened} onClose={close}>
                <Box
                    sx={{
                        position: 'absolute',
                        top: '50%',
                        left: '50%',
                        transform: 'translate(-50%, -50%)',
                        width: 400,
                        bgcolor: muiDarkTheme.palette.background.default,
                        borderRadius: 2,
                        boxShadow: 24,
                        p: 4,
                        outline: 'none',
                    }}
                >
                    <Typography variant="h6" align="center" sx={{mb: 3}}>
                        Add a new artist
                    </Typography>

                    <Stack spacing={3} alignItems="center">
                        <Autocomplete
                            options={filteredAccounts}
                            freeSolo
                            onInputChange={(event, value) => setSelectedArtistName(value)}
                            renderInput={(params) => (
                                <TextField {...params} label="Artist's name" variant="outlined" fullWidth/>
                            )}
                            sx={{width: '90%'}}
                        />

                        <Button color="success" variant="contained" onClick={handleAddArtist}>
                            Add artist
                        </Button>
                    </Stack>
                </Box>
            </Modal>
        </>
    );
};