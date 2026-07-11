import {createTheme} from "@mui/material";
import {designTokens} from "./designTokens";

export {designTokens} from "./designTokens";

export const muiDarkTheme = createTheme({
    palette: {
        mode: 'light',
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
        fontFamily: '"Segoe UI", "Trebuchet MS", "Helvetica Neue", Arial, sans-serif',
        button: {
            textTransform: 'none',
        },
    },
    shape: {
        borderRadius: 14,
    },
    components: {
        MuiBackdrop: {
            styleOverrides: {
                root: {
                    backgroundColor: designTokens.overlayBackdrop,
                    backdropFilter: 'blur(6px)',
                },
            },
        },
        MuiPaper: {
            styleOverrides: {
                root: {
                    backgroundImage: 'none',
                    backgroundColor: designTokens.cardSurface,
                    border: `1px solid ${designTokens.borderSubtle}`,
                    boxShadow: designTokens.glassShadow,
                },
            },
        },
        MuiDialog: {
            styleOverrides: {
                paper: {
                    backgroundColor: designTokens.cardSurface,
                    backgroundImage: 'none',
                    borderRadius: 20,
                },
            },
        },
        MuiButton: {
            styleOverrides: {
                root: {
                    minHeight: 42,
                    borderRadius: 999,
                    fontWeight: 700,
                    boxShadow: 'inset 0 1px 0 rgba(255, 255, 255, 0.45)',
                },
                containedPrimary: {
                    background: designTokens.glossyBlueGradient,
                    boxShadow: '0 6px 14px rgba(8, 127, 203, 0.25), inset 0 1px 0 rgba(255, 255, 255, 0.48)',
                },
            },
        },
        MuiOutlinedInput: {
            styleOverrides: {
                root: {
                    backgroundColor: 'rgba(255, 255, 255, 0.72)',
                    borderRadius: 14,
                },
            },
        },
        MuiChip: {
            styleOverrides: {
                root: {
                    borderRadius: 999,
                    backgroundColor: designTokens.cardSurfaceElevated,
                    border: `1px solid ${designTokens.borderSubtle}`,
                },
            },
        },
    },
});
