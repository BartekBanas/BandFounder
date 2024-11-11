import React, {useEffect, useState} from "react";
import {useNavigate} from "react-router-dom";
import "../components/SpotifyDrawer/SpotifyLogoButton.css";
import UseSpotifyConnected from "../hooks/useSpotifyAccountLinked";
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../components/common/RingLoader";

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

    const theme = createTheme({
        components: {
            Loader: Loader.extend({
                defaultProps: {
                    loaders: { ...Loader.defaultLoaders, ring: RingLoader },
                    type: 'ring',
                },
            }),
        },
    });

    if (true) {
        return <div className="App-header">
            <MantineThemeProvider theme={theme}>
                <Loader size={200} />
            </MantineThemeProvider>
        </div>;
    }

    return (
        <div className="App-header">

        </div>
    );
}