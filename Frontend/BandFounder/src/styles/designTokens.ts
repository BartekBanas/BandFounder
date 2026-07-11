export const designTokens = {
    accentBlue: '#087fcb',
    accentBlueGlow: 'rgba(22, 175, 232, 0.35)',
    accentBlueBorder: 'rgba(8, 127, 203, 0.28)',
    pageBackground: '#dff7ff',
    cardSurface: 'rgba(255, 255, 255, 0.78)',
    cardSurfaceElevated: 'rgba(244, 253, 255, 0.9)',
    borderSubtle: 'rgba(20, 134, 180, 0.22)',
    borderStrong: 'rgba(0, 128, 184, 0.5)',
    bandBadge: '#c64b5d',
    textPrimary: '#12374a',
    textMuted: '#537383',
    textOnDark: '#ffffff',
    ctaSurface: '#e8fbff',
    ctaSurfaceAccent: 'rgba(24, 184, 229, 0.14)',
    ctaText: '#12374a',
    successMain: '#258b57',
    successDark: '#12683d',
    successBorder: 'rgba(37, 139, 87, 0.35)',
    successSurface: 'rgba(91, 204, 132, 0.16)',
    errorMain: '#c84254',
    link: '#0076bc',
    overlayBackdrop: 'rgba(15, 86, 112, 0.34)',
    headerBackground: 'rgba(246, 254, 255, 0.76)',
    surfaceOverlaySubtle: 'rgba(255, 255, 255, 0.64)',
    scrollbarThumb: '#79cce5',
    scrollbarThumbHover: '#269ac3',
    authGradientStart: '#c5f2ff',
    authGradientEnd: '#ecfff1',
    glassHighlight: 'rgba(255, 255, 255, 0.88)',
    glassShadow: '0 12px 32px rgba(20, 112, 145, 0.16), inset 0 1px 0 rgba(255, 255, 255, 0.9)',
    glassShadowRaised: '0 18px 38px rgba(20, 112, 145, 0.22), inset 0 1px 0 rgba(255, 255, 255, 0.96)',
    focusRing: '0 0 0 3px rgba(23, 174, 226, 0.38)',
    skyGradient: 'linear-gradient(180deg, #8edcf4 0%, #dff8ff 36%, #f8fffd 70%, #dff8e9 100%)',
    glossyBlueGradient: 'linear-gradient(180deg, #4dcdf3 0%, #159bd2 48%, #0873b4 100%)',
    glossyGreenGradient: 'linear-gradient(180deg, #98e9b0 0%, #43b977 52%, #208956 100%)',
    radiusCard: '20px',
    radiusControl: '14px',
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
    '--glass-highlight': designTokens.glassHighlight,
    '--glass-shadow': designTokens.glassShadow,
    '--glass-shadow-raised': designTokens.glassShadowRaised,
    '--focus-ring': designTokens.focusRing,
    '--sky-gradient': designTokens.skyGradient,
    '--glossy-blue-gradient': designTokens.glossyBlueGradient,
    '--glossy-green-gradient': designTokens.glossyGreenGradient,
    '--radius-card': designTokens.radiusCard,
    '--radius-control': designTokens.radiusControl,
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
