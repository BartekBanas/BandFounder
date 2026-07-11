import {MantineTheme} from '@mantine/core';

const notificationStyles = (theme: MantineTheme) => ({
    root: {
        backgroundColor: 'rgba(248, 254, 255, 0.94)',
        borderColor: 'rgba(20, 134, 180, 0.28)',
        borderRadius: '16px',
        boxShadow: '0 12px 30px rgba(20, 112, 145, 0.18)',
        fontFamily: `'Segoe UI', sans-serif`,
        '&::before': {backgroundColor: theme.colors.cyan[5]},
    },
    title: {color: '#12374a'},
    description: {color: '#537383'},
    closeButton: {
        color: '#537383',
        backgroundColor: 'rgba(223, 248, 255, 0.8)',
        '&:hover': {
            color: '#12374a',
            backgroundColor: '#dff8ff !important',
        },
    },
});

export default notificationStyles;