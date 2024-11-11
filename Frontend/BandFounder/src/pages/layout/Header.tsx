import React, {FC} from 'react';
import '../styles/Header.css';
import {removeAuthToken, removeUserId} from "../../hooks/authentication";
import {
    AppBar,
    Box,
    Button,
    createTheme,
    CssBaseline,
    IconButton,
    ThemeProvider,
    Toolbar,
    Typography
} from '@mui/material';
import {UtilityDrawer} from "../../components/accountDrawer/UtilityDrawer";
import TemporaryDrawer from "../../components/accountDrawer/AccountDrawer";

export const Header: FC = () => {
    const handleLogout = () => {
        removeAuthToken();
        removeUserId();
        window.location.reload();
    }

    const theme = createTheme({
        palette: {
            primary: {
                main: '#c55858',
            },
            secondary: {
                main: '#424242',
            },
            background: {
                default: '#383838',
            },
            text: {
                primary: '#983b3b',
            },
        },
    });

    return (
        <ThemeProvider theme={theme}>
            <CssBaseline /> {/* Resetuje domyślne style przeglądarki */}
            <Box sx={{ flexGrow: 1 }}>
                <AppBar position="static" color="transparent">
                    <Toolbar>
                        <IconButton
                            size="large"
                            edge="start"
                            color="inherit"
                            aria-label="menu"
                            sx={{ mr: 2 }}
                        >
                            <TemporaryDrawer />
                        </IconButton>
                        <Typography variant="h6" component="div" sx={{ flexGrow: 1, color: 'text.primary' }}>
                            News
                        </Typography>
                        <Button color="primary" size="large" onClick={handleLogout}>
                            Logout
                        </Button>
                    </Toolbar>
                </AppBar>
            </Box>
        </ThemeProvider>
    );
};

export {};