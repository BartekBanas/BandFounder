export const designTokens = {
    // Grayscale accent: near-white for high-emphasis surfaces, dark text on top.
    accent: '#f2f2f2',
    accentGlow: 'rgba(255, 255, 255, 0.22)',
    accentBorder: 'rgba(255, 255, 255, 0.3)',
    accentText: '#161616',
    pageBackground: '#121212',
    cardSurface: '#1a1a1a',
    cardSurfaceElevated: '#232323',
    borderSubtle: '#2d2d2d',
    borderStrong: '#3d3d3d',
    bandBadge: '#3a3a3a',
    textPrimary: '#e8e8e8',
    textMuted: '#8f8f8f',
    textOnDark: '#ffffff',
    ctaSurface: '#232323',
    ctaSurfaceAccent: 'rgba(255, 255, 255, 0.1)',
    ctaText: '#e8e8e8',
    // Semantic status colors are intentionally kept (success/error/warning).
    successMain: '#4caf50',
    successDark: '#357a38',
    successBorder: 'rgba(76, 175, 80, 0.4)',
    successSurface: 'rgba(76, 175, 80, 0.08)',
    errorMain: '#ef4444',
    link: '#d9d9d9',
    overlayBackdrop: 'rgba(0, 0, 0, 0.78)',
    headerBackground: 'rgba(18, 18, 18, 0.85)',
    surfaceOverlaySubtle: 'rgba(255, 255, 255, 0.06)',
    scrollbarThumb: '#3d3d3d',
    scrollbarThumbHover: '#4d4d4d',
    authGradientStart: '#1c1c1c',
    authGradientEnd: '#2e2e2e',
} as const;

export const cssVariableMap: Record<string, string> = {
    '--primary-main': designTokens.accent,
    '--secondary-main': designTokens.cardSurfaceElevated,
    '--background-default': designTokens.pageBackground,
    '--background-paper': designTokens.cardSurface,
    '--text-primary': designTokens.textPrimary,
    '--text-secondary': designTokens.textMuted,
    '--text-muted': designTokens.textMuted,
    '--text-on-dark': designTokens.textOnDark,
    '--success-main': designTokens.successMain,
    '--success-dark': designTokens.successDark,
    '--success-border': designTokens.successBorder,
    '--success-surface': designTokens.successSurface,
    '--accent': designTokens.accent,
    '--accent-glow': designTokens.accentGlow,
    '--accent-border': designTokens.accentBorder,
    '--accent-text': designTokens.accentText,
    '--card-surface': designTokens.cardSurface,
    '--card-surface-elevated': designTokens.cardSurfaceElevated,
    '--border-subtle': designTokens.borderSubtle,
    '--border-strong': designTokens.borderStrong,
    '--band-badge': designTokens.bandBadge,
    '--cta-surface': designTokens.ctaSurface,
    '--cta-surface-accent': designTokens.ctaSurfaceAccent,
    '--cta-text': designTokens.ctaText,
    '--error-main': designTokens.errorMain,
    '--link': designTokens.link,
    '--overlay-backdrop': designTokens.overlayBackdrop,
    '--header-background': designTokens.headerBackground,
    '--surface-overlay-subtle': designTokens.surfaceOverlaySubtle,
    '--scrollbar-thumb': designTokens.scrollbarThumb,
    '--scrollbar-thumb-hover': designTokens.scrollbarThumbHover,
};

let themeVariablesApplied = false;

export function applyThemeVariables(): void {
    if (themeVariablesApplied) {
        return;
    }

    const root = document.documentElement;
    for (const [name, value] of Object.entries(cssVariableMap)) {
        root.style.setProperty(name, value);
    }

    themeVariablesApplied = true;
}
