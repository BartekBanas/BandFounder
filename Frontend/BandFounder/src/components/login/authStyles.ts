import {SxProps, Theme} from "@mui/material";
import {designTokens} from "../../styles/muiDarkTheme";

export const authPageSx: SxProps<Theme> = {
    minHeight: '100vh',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: '24px',
    backgroundColor: designTokens.pageBackground,
    color: 'text.primary',
};

export const authCardSx: SxProps<Theme> = {
    width: '100%',
    maxWidth: '400px',
    padding: {xs: '28px', sm: '36px'},
    borderRadius: '16px',
    border: `1px solid ${designTokens.borderSubtle}`,
    backgroundColor: designTokens.cardSurface,
    color: 'text.primary',
};
