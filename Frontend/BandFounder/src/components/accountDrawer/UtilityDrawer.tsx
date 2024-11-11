import React, {FC} from 'react';
import {useDisclosure} from "@mantine/hooks";
import {Drawer, IconButton, Box, Typography} from "@mui/material";
import {DeleteAccountButton} from "./DeleteAccountButton";
import {UpdateAccountButton} from "./UpdateAccountButton";
import {SpotifyConnectionButton} from "./spotifyConnection/SpotifyConnectionButton";

interface UtilityDrawerProps {
}

export const UtilityDrawer: FC<UtilityDrawerProps> = () => {
    const [opened, {open, close}] = useDisclosure(false);

    return (
        <>
            <Drawer
                open={opened}
                onClose={close}
                anchor="left"
                sx={{
                    width: 200,
                    flexShrink: 0,
                    '& .MuiDrawer-paper': {
                        width: 250,
                        boxSizing: 'border-box',
                        backgroundColor: 'background.default', // Kolor tła szuflady z motywu
                        color: 'text.primary', // Kolor tekstu w szufladzie
                    },
                }}
            >
                <Box sx={{padding: 2}}>
                    <Typography variant="h6" gutterBottom>
                        Account Utilities
                    </Typography>

                    <Box
                        sx={{
                            display: 'flex',
                            flexDirection: 'column',
                            gap: 2,
                            justifyContent: 'center',
                            alignItems: 'center',
                        }}
                    >
                        <UpdateAccountButton/>
                        <DeleteAccountButton/>
                        <SpotifyConnectionButton/>
                    </Box>
                </Box>
            </Drawer>

            <div>
                <IconButton onClick={open} size="large" color="primary">
                    <svg xmlns="http://www.w3.org/2000/svg" width={36} height={36} viewBox="0 0 24 24" fill="none"
                         stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round"
                         className="icon icon-tabler icons-tabler-outline icon-tabler-adjustments">
                        <path stroke="none" d="M0 0h24v24H0z" fill="none"/>
                        <path d="M4 10a2 2 0 1 0 4 0a2 2 0 0 0 -4 0"/>
                        <path d="M6 4v4"/>
                        <path d="M6 12v8"/>
                        <path d="M10 16a2 2 0 1 0 4 0a2 2 0 0 0 -4 0"/>
                        <path d="M12 4v10"/>
                        <path d="M12 18v2"/>
                        <path d="M16 7a2 2 0 1 0 4 0a2 2 0 0 0 -4 0"/>
                        <path d="M18 4v1"/>
                        <path d="M18 9v11"/>
                    </svg>
                </IconButton>
            </div>
        </>
    );
};
