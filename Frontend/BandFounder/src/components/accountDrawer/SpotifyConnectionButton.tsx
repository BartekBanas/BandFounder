import React, {useEffect, useState} from "react";
import UseSpotifyConnected from "../../hooks/useSpotifyAccountLinked";
import {useNavigate} from "react-router-dom";
import {List, ListItem, ListItemText} from "@mui/material";
import DisconnectSpotifyAccountButton from "../spotiftConnection/DisconnectSpotifyAccountButton";
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
            {isConnectedToSpotify ? (
                <>
                    <DisconnectSpotifyAccountButton onDelete={handleDelete}/>
                </>
            ) : (
                <>
                    <SpotifyAuthorizationButton/>
                </>
            )}
        </div>
    );
}