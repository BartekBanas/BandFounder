import {MantineTheme} from '@mantine/core';

const notificationStyles = (theme: MantineTheme) => ({
    root: {
        backgroundColor: theme.colors.dark[6],
        borderColor: theme.colors.dark[7],
        '&::before': {backgroundColor: theme.colors.yellow},
    },
    title: {color: theme.colors.gray[2]},
    description: {color: theme.colors.gray[5]},
    closeButton: {
        color: theme.colors.gray[5],
        '&:hover': {
            color: theme.colors.dark[0],
            backgroundColor: theme.colors.gray[7],
        }
    },
});

export default notificationStyles;