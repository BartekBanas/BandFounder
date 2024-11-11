import React, {FC} from 'react';
import '../styles/Header.css';
import {removeAuthToken, removeUserId} from "../../hooks/authentication";
import {
    AppBar,
    Box,
    Button,
    CssBaseline,
    IconButton,
    ThemeProvider,
    Toolbar,
    Typography
} from '@mui/material';
import {UtilityDrawer} from "../../components/accountDrawer/UtilityDrawer";
import {darkTheme} from "../../components/accountDrawer/darkTheme";

export const Header: FC = () => {
    const handleLogout = () => {
        removeAuthToken();
        removeUserId();
        window.location.reload();
    }

    return (
        <ThemeProvider theme={darkTheme}>
            <CssBaseline/>
            <Box sx={{flexGrow: 1}}>
                <AppBar position="static" color="transparent">
                    <Toolbar>
                        <IconButton
                            size="large"
                            edge="start"
                            color="inherit"
                            aria-label="menu"
                            sx={{mr: 2}}
                        >
                            <UtilityDrawer/>
                        </IconButton>
                        <Typography variant="h6" component="div" sx={{flexGrow: 1, color: 'text.primary'}}>
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