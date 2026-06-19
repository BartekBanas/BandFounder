import {createTheme} from "@mui/material";

export const designTokens = {
    accentBlue: '#3b82f6',
    accentBlueGlow: 'rgba(59, 130, 246, 0.35)',
    cardSurface: '#1e2433',
    cardSurfaceElevated: '#252b3b',
    borderSubtle: '#2d3548',
    bandBadge: '#9a1a1a',
    pageBackground: '#141820',
    textMuted: '#8b95a8',
    ctaSurface: '#252b3b',
    ctaSurfaceAccent: 'rgba(59, 130, 246, 0.12)',
    ctaText: '#e8eaed',
};

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
            primary: '#e8eaed',
            secondary: designTokens.textMuted,
        },
        success: {
            main: '#4caf50',
            dark: '#357a38',
            contrastText: '#ffffff',
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
                    backgroundColor: 'rgba(14, 20, 32, 0.78)',
                    backdropFilter: 'blur(6px)',
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

export const cssVariables = `
    :root {
        --primary-main: ${muiDarkTheme.palette.primary.main};
        --secondary-main: ${muiDarkTheme.palette.secondary.main};
        --background-default: ${designTokens.pageBackground};
        --background-paper: ${designTokens.cardSurface};
        --text-primary: ${muiDarkTheme.palette.text.primary};
        --text-secondary: ${muiDarkTheme.palette.text.secondary};
        --text-muted: ${designTokens.textMuted};
        --success-main: ${muiDarkTheme.palette.success.main};
        --success-dark: ${muiDarkTheme.palette.success.dark};
        --accent-blue: ${designTokens.accentBlue};
        --accent-blue-glow: ${designTokens.accentBlueGlow};
        --card-surface: ${designTokens.cardSurface};
        --card-surface-elevated: ${designTokens.cardSurfaceElevated};
        --border-subtle: ${designTokens.borderSubtle};
        --band-badge: ${designTokens.bandBadge};
        --cta-surface: ${designTokens.ctaSurface};
        --cta-text: ${designTokens.ctaText};
    }
`;
