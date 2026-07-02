import {useDisclosure} from '@mantine/hooks';
import {Autocomplete, Box, Button, Divider, IconButton, Modal, Stack, TextField, Typography} from '@mui/material';
import {muiDarkTheme} from "../../../styles/muiDarkTheme";
import {mantineErrorNotification, mantineSuccessNotification} from "../../common/mantineNotification";
import {deleteChatroom, inviteToChatroom} from "../../../api/chatroom";
import {FC, useEffect, useState} from "react";
import GroupsIcon from "@mui/icons-material/Groups";
import {Account} from "../../../types/Account";
import {ChatRoom} from "../../../types/ChatRoom";
import {getAccounts} from "../../../api/account";
import {getUserId} from "../../../hooks/authentication";
import UserAvatar from "../../common/UserAvatar";

interface GroupMembersPanelProps {
    chatroom: ChatRoom;
    participants: Account[];
    onMembersChanged: () => void;
}

export const GroupMembersPanel: FC<GroupMembersPanelProps> = ({chatroom, participants, onMembersChanged}) => {
    const [opened, {close, open}] = useDisclosure(false);
    const [invitableUsers, setInvitableUsers] = useState<Account[]>([]);
    const [selectedUser, setSelectedUser] = useState<Account | null>(null);
    const [busy, setBusy] = useState<boolean>(false);

    const isOwner = chatroom.ownerId === getUserId();

    useEffect(() => {
        if (!opened) return;

        const fetchInvitableUsers = async () => {
            const accounts = await getAccounts();
            const memberIds = new Set(participants.map((participant) => participant.id));
            setInvitableUsers(accounts.filter((account: Account) => !memberIds.has(account.id)));
        };

        fetchInvitableUsers();
    }, [opened, participants]);

    const handleInvite = async () => {
        if (!selectedUser) return;

        setBusy(true);
        try {
            await inviteToChatroom(chatroom.id, selectedUser.id);
            mantineSuccessNotification(`${selectedUser.name} has been invited to the group`);
            setSelectedUser(null);
            onMembersChanged();
        } catch (error) {
            console.error("Error inviting user to chatroom:", error);
        } finally {
            setBusy(false);
        }
    };

    const handleDeleteGroup = async () => {
        setBusy(true);
        try {
            await deleteChatroom(chatroom.id);
            window.location.href = '/messages';
        } catch (error) {
            mantineErrorNotification('Failed to delete group');
            console.error("Error deleting chatroom:", error);
            setBusy(false);
        }
    };

    return (
        <>
            <IconButton onClick={open} title="Group members" sx={{marginLeft: 'auto'}}>
                <GroupsIcon/>
            </IconButton>

            <Modal open={opened} onClose={close}>
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
                    <Typography variant="h6" align="center" sx={{mb: 2}}>
                        {chatroom.name} — {participants.length} {participants.length === 1 ? 'member' : 'members'}
                    </Typography>

                    <Stack spacing={1} sx={{maxHeight: 260, overflowY: 'auto', mb: 2}}>
                        {participants.map((participant) => (
                            <Stack key={participant.id} direction="row" spacing={2} alignItems="center">
                                <UserAvatar userId={participant.id} size={36}/>
                                <Typography variant="body1" sx={{flex: 1}}>
                                    {participant.name}
                                </Typography>
                                {participant.id === chatroom.ownerId && (
                                    <Typography variant="caption" sx={{color: 'text.secondary'}}>
                                        Owner
                                    </Typography>
                                )}
                            </Stack>
                        ))}
                    </Stack>

                    <Divider sx={{mb: 2}}/>

                    <Stack direction="row" spacing={1} alignItems="center">
                        <Autocomplete
                            options={invitableUsers}
                            getOptionLabel={(user) => user.name}
                            value={selectedUser}
                            onChange={(event, newValue) => setSelectedUser(newValue)}
                            renderInput={(params) =>
                                <TextField {...params} label="Invite a user" variant="outlined" size="small"/>
                            }
                            sx={{flex: 1}}
                        />
                        <Button variant="contained" onClick={handleInvite} disabled={!selectedUser || busy}>
                            Invite
                        </Button>
                    </Stack>

                    {isOwner && (
                        <Stack direction="row" justifyContent="center" sx={{mt: 3}}>
                            <Button variant="contained" color="error" onClick={handleDeleteGroup} disabled={busy}>
                                Delete group
                            </Button>
                        </Stack>
                    )}
                </Box>
            </Modal>
        </>
    );
}
