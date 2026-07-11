import {alpha, createTheme} from "@mui/material";
import {designTokens} from "./designTokens";

export {designTokens} from "./designTokens";

const subpixelLayer = {
    isolation: 'isolate' as const,
    transform: 'translateZ(0)',
    WebkitBackfaceVisibility: 'hidden' as const,
    backfaceVisibility: 'hidden' as const,
};

const insetRing = (color: string) => `inset 0 0 0 1px ${color}`;

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
        error: {
            main: designTokens.errorMain,
            contrastText: designTokens.textOnDark,
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
        MuiButtonBase: {
            styleOverrides: {
                root: subpixelLayer,
            },
        },
        MuiButton: {
            defaultProps: {
                disableElevation: true,
            },
            styleOverrides: {
                root: {
                    ...subpixelLayer,
                    '&.Mui-focusVisible': {
                        outline: 'none',
                    },
                },
                contained: {
                    border: 'none',
                    backgroundClip: 'padding-box',
                    boxShadow: 'none',
                    '&:hover': {
                        boxShadow: 'none',
                    },
                    '&.Mui-focusVisible': {
                        outline: `2px solid ${designTokens.accentBlue}`,
                        outlineOffset: '2px',
                    },
                },
                outlined: {
                    border: 'none',
                    backgroundClip: 'padding-box',
                    boxShadow: insetRing(designTokens.borderSubtle),
                    '&:hover': {
                        border: 'none',
                        boxShadow: insetRing(designTokens.borderStrong),
                    },
                    '&.Mui-focusVisible': {
                        border: 'none',
                        boxShadow: `${insetRing(designTokens.accentBlue)}, 0 0 0 3px ${designTokens.accentBlueGlow}`,
                    },
                },
                outlinedPrimary: {
                    border: 'none',
                    boxShadow: insetRing(alpha(designTokens.accentBlue, 0.5)),
                    '&:hover': {
                        border: 'none',
                        backgroundColor: designTokens.ctaSurfaceAccent,
                        boxShadow: insetRing(designTokens.accentBlue),
                    },
                },
            },
        },
        MuiIconButton: {
            styleOverrides: {
                root: subpixelLayer,
            },
        },
    },
});
