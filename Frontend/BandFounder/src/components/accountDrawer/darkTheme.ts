import {createTheme} from "@mui/material";

export const darkTheme = createTheme({
    palette: {
        mode: 'dark',
        primary: {
            main: '#dedede',
        },
        secondary: {
            main: '#424242',
        },
        background: {
            default: '#282828',
            paper: '#7a7a7a',
        },
        text: {
            primary: '#cbcbcb',
            secondary: '#b0bec5',
        }
    },
    typography: {
        button: {
            textTransform: 'none', // Disable uppercase transformation
        },
    },
});