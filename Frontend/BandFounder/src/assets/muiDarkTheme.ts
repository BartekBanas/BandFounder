import {createTheme} from "@mui/material";

export const muiDarkTheme = createTheme({
    palette: {
        mode: 'dark',
        primary: {
            main: '#dedede',
        },
        secondary: {
            main: '#424242',
        },
        background: {
            default: '#2c2c2c',
            paper: '#1e1e1e',
        },
        text: {
            primary: '#cbcbcb',
            secondary: '#b0bec5',
        },
        success: {
            main: '#4caf50',
            dark: '#44bb4a',
            contrastText: '#2a2a2a',
        }
    },
    typography: {
        button: {
            textTransform: 'none', // Disable uppercase transformation
        },
    },
});