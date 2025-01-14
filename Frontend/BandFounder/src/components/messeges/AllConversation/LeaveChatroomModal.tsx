import {useDisclosure} from '@mantine/hooks';
import {Box, Button, IconButton, Modal, Stack, Typography} from '@mui/material';
import {muiDarkTheme} from "../../../styles/muiDarkTheme";
import {mantineErrorNotification} from "../../common/mantineNotification";
import {leaveChatroom} from "../../../api/chatroom";
import {FC} from "react";
import LogoutIcon from "@mui/icons-material/Logout";
import {ChatRoom} from "../../../types/ChatRoom";

interface LeaveChatroomModalProps {
    chatroom: ChatRoom;
    setRefreshConversations: React.Dispatch<React.SetStateAction<boolean>>;
}

export const LeaveChatroomModal: FC<LeaveChatroomModalProps> = ({chatroom, setRefreshConversations}) => {
    const [opened, {close, open}] = useDisclosure(false);

    const handleLeaveChatroom = async () => {
        try {
            await leaveChatroom(chatroom.id);
            window.location.href = '/messages';
        } catch (error) {
            mantineErrorNotification("Failed to leave chatroom");
        }

        close();
    };

    return (
        <>
            <IconButton onClick={open}>
                <LogoutIcon/>
            </IconButton>

            <Modal open={opened} onClose={close}>
                <Box
                    sx={{
                        position: 'absolute',
                        top: '50%',
                        left: '50%',
                        transform: 'translate(-50%, -50%)',
                        width: 'auto',
                        maxWidth: 400,
                        bgcolor: muiDarkTheme.palette.background.default,
                        borderRadius: 2,
                        boxShadow: 24,
                        p: 4,
                    }}
                >
                    <Typography variant="body1" align="center">
                        {
                            chatroom.type === 'Direct' ?
                                `Are you sure you want to delete your conversation with ${chatroom.name}?` :
                                `Are you sure you want to leave ${chatroom.name}?`
                        }
                    </Typography>
                    <Typography variant="body2" align="center" sx={{color: 'text.secondary', mt: 1}}>
                        This action cannot be reversed and members will still be able to view the conversation.
                    </Typography>

                    <Stack direction="row" spacing={2} justifyContent="center" sx={{mt: 3}}>
                        <Button variant="outlined" color="primary" onClick={close}>
                            No, don't delete it
                        </Button>
                        <Button variant="contained" color="error" onClick={handleLeaveChatroom}>
                            {
                                chatroom.type === 'Direct' ?
                                    `Delete conversation` :
                                    `Leave conversation`
                            }
                        </Button>
                    </Stack>
                </Box>
            </Modal>
        </>
    );
}