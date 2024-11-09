import React, {useEffect, useState} from "react";
import UseSpotifyConnected from "../../hooks/useSpotifyAccountLinked";
import {useNavigate} from "react-router-dom";
import {List, ListItem, ListItemText} from "@mui/material";
import SpotifyDeleteCredentialButton from "../spotiftConnection/spotifyDeleteCredentialButton";
import SpotifyAuthorizationButton from "../spotiftConnection/SpotifyAuthorizationButton";
import {deleteSpotifyCredential} from "../spotiftConnection/spotifyConnection";
import {sleep} from "../../hooks/utils";

export function SpotifyConnectionButton() {
    const [isConnectedToSpotify, setIsConnectedToSpotify] = useState<boolean>(false);
    const [loading, setLoading] = useState<boolean>(true);
    const [refresh, setRefresh] = useState<boolean>(false);
    const navigate = useNavigate();

    useEffect(() => {
        const checkSpotifyConnection = async () => {
            const isConnected = await UseSpotifyConnected();
            setIsConnectedToSpotify(isConnected);
            setLoading(false);
        };

        checkSpotifyConnection();
    }, [navigate, refresh]);

    const handleDelete = async () => {
        await deleteSpotifyCredential();
        await sleep(100);

        setRefresh(!refresh);
    };

    return (
        <div>
            <List>
                {isConnectedToSpotify ? (
                    <>
                        <ListItem>
                            <ListItemText primary="You are logged in (also on Spotify)"/>
                        </ListItem>
                        <ListItem>
                            <SpotifyDeleteCredentialButton onDelete={handleDelete}/>
                        </ListItem>
                    </>
                ) : (
                    <>
                        <ListItem>
                            <ListItemText primary="You are logged in"/>
                        </ListItem>
                        <ListItem>
                            <SpotifyAuthorizationButton/>
                        </ListItem>
                    </>
                )}
            </List>
        </div>
    );
}