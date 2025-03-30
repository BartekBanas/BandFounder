import {useEffect, useState} from "react";
import UseSpotifyConnected from "../../../hooks/useSpotifyAccountLinked";
import {useNavigate} from "react-router-dom";
import {deleteSpotifyCredential, redirectToSpotifyAuthorizationPage} from "./spotifyConnection";
import {sleep} from "../../common/utils";
import {muiDarkTheme} from "../../../styles/muiDarkTheme";
import {SpotifyIcon} from "./SpotifyIcon";
import {Button} from "@mui/material";

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
        <>
            {isConnectedToSpotify ? (
                <Button
                    variant="contained"
                    sx={{
                        backgroundColor: muiDarkTheme.palette.error.main,
                        color: muiDarkTheme.palette.error.contrastText,
                        '&:hover': {
                            backgroundColor: muiDarkTheme.palette.error.dark,
                        },
                    }}
                    startIcon={<SpotifyIcon/>}
                    onClick={handleDelete}
                >
                    Disconnect with Spotify
                </Button>
            ) : (
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
                    startIcon={<SpotifyIcon/>}
                >
                    Link Spotify Account
                </Button>
            )}
        </>
    );
}