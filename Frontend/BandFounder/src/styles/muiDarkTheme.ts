import {createTheme} from "@mui/material";
import {designTokens} from "./designTokens";

export {designTokens} from "./designTokens";

export const muiDarkTheme = createTheme({
    palette: {
        mode: 'dark',
        primary: {
            main: designTokens.accentBlue,
        },
        secondary: {
            main: designTokens.cardSurfaceElevated,
        },
        background: {
            default: designTokens.pageBackground,
            paper: designTokens.cardSurface,
        },
        text: {
            primary: designTokens.textPrimary,
            secondary: designTokens.textMuted,
        },
        success: {
            main: designTokens.successMain,
            dark: designTokens.successDark,
            contrastText: designTokens.textOnDark,
        },
        info: {
            main: designTokens.accentBlue,
        },
    },
    typography: {
        fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
        button: {
            textTransform: 'none',
        },
    },
    shape: {
        borderRadius: 12,
    },
    components: {
        MuiBackdrop: {
            styleOverrides: {
                root: {
                    backgroundColor: designTokens.overlayBackdrop,
                    backdropFilter: 'blur(6px)',
                },
                invisible: {
                    backgroundColor: 'transparent',
                    backdropFilter: 'none',
                },
            },
        },
        MuiPaper: {
            styleOverrides: {
                root: {
                    backgroundImage: 'none',
                },
            },
        },
        MuiDialog: {
            styleOverrides: {
                paper: {
                    backgroundColor: designTokens.cardSurface,
                    backgroundImage: 'none',
                },
            },
        },
    },
});
