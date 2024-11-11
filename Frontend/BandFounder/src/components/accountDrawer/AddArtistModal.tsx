import React, {FC, useEffect, useState} from 'react';
import {Autocomplete, Button, Flex, Group, Modal, Space} from '@mantine/core';
import {API_URL} from '../../config';
import {authorizedHeaders} from "../../hooks/utils";
import {useDisclosure} from "@mantine/hooks";
import {getUserId} from "../../hooks/authentication";
import {getArtists} from "./api";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";

export const AddArtistModal: FC = () => {
    const [opened, {close, open}] = useDisclosure(false);
    const [artists, setArtists] = useState<string[]>([]);
    const [myArtists, setMyArtists] = useState<string[]>([]);
    const [selectedArtistName, SetSelectedArtistName] = useState<string | null>(null);

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

        close();
    };

    const filteredAccounts: string[] = artists.filter(artist => !myArtists.includes(artist));

    return (
        <>
            <Modal opened={opened} onClose={close} size="auto" title="Add a new artist">
                <Flex
                    justify="center"
                    align="center"
                    direction="column"
                >
                    <Autocomplete
                        size="md"
                        maw={320}
                        mx="auto"
                        placeholder="Artist's name"
                        data={filteredAccounts}
                        onChange={(value) => {
                            SetSelectedArtistName(value)
                        }}
                    />

                    <Space h="xl"/>

                    <Button color="green" onClick={handleAddArtist}>
                        Add artist
                    </Button>
                </Flex>
            </Modal>
            <Group>
                <Button color="green" size="md" onClick={open}>Add an artist</Button>
            </Group>
        </>
    );
};