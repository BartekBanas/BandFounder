import {alpha, createTheme} from "@mui/material";
import KeyboardArrowDownRoundedIcon from "@mui/icons-material/KeyboardArrowDownRounded";
import {designTokens} from "./designTokens";

export {designTokens} from "./designTokens";

const subpixelLayer = {
    isolation: 'isolate' as const,
    transform: 'translateZ(0)',
    WebkitBackfaceVisibility: 'hidden' as const,
    backfaceVisibility: 'hidden' as const,
};

const insetRing = (color: string) => `inset 0 0 0 1px ${color}`;

// Shared by every dropdown popup (Select menus and Autocomplete listboxes).
const dropdownPaper = {
    marginTop: 6,
    padding: 6,
    backgroundColor: designTokens.cardSurfaceElevated,
    backgroundImage: 'none',
    borderRadius: 14,
    boxShadow: `${insetRing(designTokens.borderSubtle)}, 0 20px 40px rgba(0, 0, 0, 0.5)`,
    scrollbarWidth: 'thin' as const,
    scrollbarColor: `${designTokens.scrollbarThumb} transparent`,
};

// `selected` is the selector that marks the chosen item. MenuItem uses a class, Autocomplete uses aria.
const dropdownItem = (selected: string) => ({
    position: 'relative' as const,
    minHeight: 36,
    paddingInline: '14px',
    borderRadius: 10,
    fontSize: '0.9rem',
    color: designTokens.textBody,
    transition: 'background-color 0.12s ease, color 0.12s ease',
    '&:hover, &.Mui-focused, &.Mui-focusVisible': {
        backgroundColor: designTokens.surfaceOverlaySubtle,
        color: designTokens.textPrimary,
    },
    [selected]: {
        color: designTokens.textOnDark,
        fontWeight: 600,
        backgroundColor: designTokens.ctaSurfaceAccent,
    },
    [`${selected}:hover, ${selected}.Mui-focused`]: {
        backgroundColor: 'rgba(255, 255, 255, 0.14)',
    },
    [`${selected}::before`]: {
        content: '""',
        position: 'absolute',
        left: 0,
        top: '50%',
        width: 3,
        height: 16,
        borderRadius: 2,
        backgroundColor: designTokens.accent,
        transform: 'translateY(-50%)',
    },
});

export const muiDarkTheme = createTheme({
    palette: {
        mode: 'dark',
        primary: {
            main: designTokens.accent,
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
            main: designTokens.accent,
        },
        error: {
            main: designTokens.errorMain,
            contrastText: designTokens.textOnDark,
        },
    },
    typography: {
        fontFamily: '"Geist", "Helvetica", "Arial", sans-serif',
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
                        outline: `2px solid ${designTokens.accent}`,
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
                        boxShadow: `${insetRing(designTokens.accent)}, 0 0 0 3px ${designTokens.accentGlow}`,
                    },
                },
                outlinedPrimary: {
                    border: 'none',
                    boxShadow: insetRing(alpha(designTokens.accent, 0.5)),
                    '&:hover': {
                        border: 'none',
                        backgroundColor: designTokens.ctaSurfaceAccent,
                        boxShadow: insetRing(designTokens.accent),
                    },
                },
            },
        },
        MuiIconButton: {
            styleOverrides: {
                root: subpixelLayer,
            },
        },
        MuiSelect: {
            defaultProps: {
                IconComponent: KeyboardArrowDownRoundedIcon,
            },
            styleOverrides: {
                icon: {
                    right: 12,
                    color: designTokens.textMuted,
                },
            },
        },
        MuiMenu: {
            styleOverrides: {
                paper: dropdownPaper,
                list: {
                    paddingBlock: 0,
                },
            },
        },
        MuiMenuItem: {
            styleOverrides: {
                root: dropdownItem('&.Mui-selected'),
            },
        },
        MuiAutocomplete: {
            styleOverrides: {
                paper: dropdownPaper,
                listbox: {
                    padding: 0,
                    maxHeight: '40vh',
                },
                option: dropdownItem('&[aria-selected="true"]'),
            },
        },
    },
});
