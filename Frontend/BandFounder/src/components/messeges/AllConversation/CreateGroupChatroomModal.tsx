import {useDisclosure} from '@mantine/hooks';
import {Autocomplete, Box, Button, IconButton, Modal, Stack, TextField, Typography} from '@mui/material';
import {muiDarkTheme} from "../../../styles/muiDarkTheme";
import {mantineErrorNotification} from "../../common/mantineNotification";
import {createGroupChatroom, inviteToChatroom} from "../../../api/chatroom";
import {FC, useState} from "react";
import GroupAddIcon from "@mui/icons-material/GroupAdd";
import {Account} from "../../../types/Account";

interface CreateGroupChatroomModalProps {
    otherUsers: Account[];
    onCreated: (chatroomId: string) => void;
}

export const CreateGroupChatroomModal: FC<CreateGroupChatroomModalProps> = ({otherUsers, onCreated}) => {
    const [opened, {close, open}] = useDisclosure(false);
    const [groupName, setGroupName] = useState<string>('');
    const [selectedUsers, setSelectedUsers] = useState<Account[]>([]);
    const [creating, setCreating] = useState<boolean>(false);

    const handleClose = () => {
        setGroupName('');
        setSelectedUsers([]);
        close();
    };

    const handleCreateGroup = async () => {
        const trimmedName = groupName.trim();
        if (!trimmedName) {
            mantineErrorNotification('Group name is required');
            return;
        }

        setCreating(true);
        try {
            const chatroom = await createGroupChatroom(trimmedName);

            for (const user of selectedUsers) {
                await inviteToChatroom(chatroom.id, user.id);
            }

            handleClose();
            onCreated(chatroom.id);
        } catch (error) {
            mantineErrorNotification('Failed to create group chatroom');
            console.error("Error creating group chatroom:", error);
        } finally {
            setCreating(false);
        }
    };

    return (
        <>
            <IconButton onClick={open} title="Create group chat" sx={{alignSelf: 'center'}}>
                <GroupAddIcon/>
            </IconButton>

            <Modal open={opened} onClose={handleClose}>
                <Box
                    sx={{
                        position: 'absolute',
                        top: '50%',
                        left: '50%',
                        transform: 'translate(-50%, -50%)',
                        width: '90%',
                        maxWidth: 440,
                        bgcolor: muiDarkTheme.palette.background.default,
                        borderRadius: 2,
                        boxShadow: 24,
                        p: 4,
                    }}
                >
                    <Typography variant="h6" align="center" sx={{mb: 3}}>
                        Create a group chat
                    </Typography>

                    <Stack spacing={2}>
                        <TextField
                            label="Group name"
                            variant="outlined"
                            fullWidth
                            value={groupName}
                            onChange={(event) => setGroupName(event.target.value)}
                        />

                        <Autocomplete
                            multiple
                            options={otherUsers}
                            getOptionLabel={(user) => user.name}
                            value={selectedUsers}
                            onChange={(event, newValue) => setSelectedUsers(newValue)}
                            renderInput={(params) =>
                                <TextField {...params} label="Invite users" variant="outlined"/>
                            }
                        />
                    </Stack>

                    <Stack direction="row" spacing={2} justifyContent="center" sx={{mt: 3}}>
                        <Button variant="outlined" color="primary" onClick={handleClose}>
                            Cancel
                        </Button>
                        <Button variant="contained" color="primary" onClick={handleCreateGroup} disabled={creating}>
                            Create group
                        </Button>
                    </Stack>
                </Box>
            </Modal>
        </>
    );
}
