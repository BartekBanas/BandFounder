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
            paper: '#131313',
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

export const cssVariables = `
    :root {
        --primary-main: ${muiDarkTheme.palette.primary.main};
        --secondary-main: ${muiDarkTheme.palette.secondary.main};
        --background-default: ${muiDarkTheme.palette.background.default};
        --background-paper: ${muiDarkTheme.palette.background.paper};
        --text-primary: ${muiDarkTheme.palette.text.primary};
        --text-secondary: ${muiDarkTheme.palette.text.secondary};
        --success-main: ${muiDarkTheme.palette.success.main};
        --success-dark: ${muiDarkTheme.palette.success.dark};
        --success-contrastText: ${muiDarkTheme.palette.success.contrastText};
    }
`;