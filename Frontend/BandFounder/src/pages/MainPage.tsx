import React, {useEffect, useState} from "react";
import {useNavigate} from "react-router-dom";
import SpotifyDeleteCredentialButton from "../components/spotiftConnection/spotifyDeleteCredentialButton";
import SpotifyLogoButton from "../components/SpotifyDrawer/SpotifyLogoButton";
import {Drawer, List, ListItem, ListItemText} from "@mui/material";
import SpotifyAuthorizationButton from "../components/spotiftConnection/SpotifyAuthorizationButton";
import "../components/SpotifyDrawer/SpotifyLogoButton.css";
import UseSpotifyConnected from "../hooks/useSpotifyAccountLinked";

export function MainPage() {
    const [isConnectedToSpotify, setIsConnectedToSpotify] = useState<boolean>(false);
    const [loading, setLoading] = useState<boolean>(true);
    const [drawerOpen, setDrawerOpen] = useState<boolean>(false);
    const navigate = useNavigate();

    useEffect(() => {
        const checkSpotifyConnection = async () => {
            const isConnected = await UseSpotifyConnected();
            setIsConnectedToSpotify(isConnected);
            setLoading(false);
        };

        checkSpotifyConnection();
    }, [navigate]);

    const toggleDrawer = (open: boolean) => (event: React.KeyboardEvent | React.MouseEvent) => {
        if (event.type === 'keydown' && ((event as React.KeyboardEvent).key === 'Tab' || (event as React.KeyboardEvent).key === 'Shift')) {
            return;
        }
        setDrawerOpen(open);
    };

    if (loading) {
        return <div className="App-header"><h1>Loading...</h1></div>;
    }

    return (
        <div className="App-header">
            <SpotifyLogoButton
                onClick={toggleDrawer(true)}
                className={drawerOpen ? "drawer-open" : ""}
            />
            <Drawer
                anchor="left"
                open={drawerOpen}
                onClose={toggleDrawer(false)}
                sx={{width: '140px'}}
            >
                <List>
                    {isConnectedToSpotify ? (
                        <>
                            <ListItem>
                                <ListItemText primary="You are logged in (also on Spotify)"/>
                            </ListItem>
                            <ListItem>
                                <SpotifyDeleteCredentialButton/>
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
            </Drawer>
        </div>
    );
}