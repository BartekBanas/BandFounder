export const designTokens = {
    accentBlue: '#3b82f6',
    accentBlueGlow: 'rgba(59, 130, 246, 0.35)',
    accentBlueBorder: 'rgba(59, 130, 246, 0.25)',
    pageBackground: '#141820',
    cardSurface: '#1e2433',
    cardSurfaceElevated: '#252b3b',
    borderSubtle: '#2d3548',
    borderStrong: '#3d465c',
    bandBadge: '#9a1a1a',
    textPrimary: '#e8eaed',
    textMuted: '#8b95a8',
    textOnDark: '#ffffff',
    ctaSurface: '#252b3b',
    ctaSurfaceAccent: 'rgba(59, 130, 246, 0.12)',
    ctaText: '#e8eaed',
    successMain: '#4caf50',
    successDark: '#357a38',
    successBorder: 'rgba(76, 175, 80, 0.4)',
    successSurface: 'rgba(76, 175, 80, 0.08)',
    errorMain: '#ef4444',
    link: '#3b82f6',
    overlayBackdrop: 'rgba(14, 20, 32, 0.78)',
    headerBackground: 'rgba(20, 24, 32, 0.85)',
    surfaceOverlaySubtle: 'rgba(255, 255, 255, 0.06)',
    scrollbarThumb: '#3d465c',
    scrollbarThumbHover: '#4f5a72',
    authGradientStart: '#1c1c1c',
    authGradientEnd: '#2e2e2e',
} as const;

export const cssVariableMap: Record<string, string> = {
    '--primary-main': designTokens.accentBlue,
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
    '--accent-blue': designTokens.accentBlue,
    '--accent-blue-glow': designTokens.accentBlueGlow,
    '--accent-blue-border': designTokens.accentBlueBorder,
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
