import React, {FC, useState, useEffect} from "react";
import {ChatRoom} from "../../../types/ChatRoom";
import {getMyChatrooms, createDirectChatroom, createGroupChatroom} from "../../../api/chatroom";
import {Autocomplete, Button, CircularProgress, TextField, Chip, Modal, Box} from "@mui/material";
import './styles.css'
import './../../../assets/CustomScrollbar.css'
import {getUserId} from "../../../hooks/authentication";
import {getAccounts, getUser} from "../../../api/account";
import {Account} from "../../../types/Account";
import {mantineErrorNotification} from "../../common/mantineNotification";
import {LeaveChatroomModal} from "./LeaveChatroomModal";
import ProfilePicture from "../../profile/ProfilePicture";

interface AllConversationsProps {
    onSelectConversation: (id: string) => void;
}

export const AllConversations: FC<AllConversationsProps> = ({onSelectConversation}) => {
    const [chatRooms, setChatRooms] = useState<ChatRoom[]>([]);
    const [otherUsers, setOtherUsers] = useState<Account[]>([]);
    const [selectedUsers, setSelectedUsers] = useState<Account[]>([]);
    const [modalOpen, setModalOpen] = useState<boolean>(false);
    const [refreshConversations, setRefreshConversations] = useState<boolean>(false);
    const [groupName, setGroupName] = useState<string>("");


    useEffect(() => {
        fetchConversations();
        fetchOtherUsers();
    }, [refreshConversations]);

    const fetchConversations = async () => {
        const chatrooms = await getMyChatrooms();

        for (const chatroom of chatrooms) {
            if (chatroom.type === 'Direct') {
                const otherUserId = chatroom.membersIds.find((id) => id !== getUserId());
                if (otherUserId) {
                    const user = await getUser(otherUserId);
                    chatroom.name = user.name;
                    chatroom.membersIds = [otherUserId];
                } else {
                    chatroom.name = 'Unknown';
                }
            } else {
                const otherUsersIds = chatroom.membersIds.filter((id) => id !== getUserId());
                if (chatroom.name !== '' && chatroom.name !== null && chatroom.name !== undefined && chatroom.name !== 'string') {
                    chatroom.name = chatroom.name;
                } else if (otherUsersIds.length !== 0) {
                    const user1 = await getUser(otherUsersIds[0]);
                    if (otherUsersIds.length === 1) {
                        chatroom.name = 'Group chat with ' + user1.name;
                    }

                    if (otherUsersIds.length > 1) {
                        const user2 = await getUser(otherUsersIds[1]);
                        chatroom.name = user1.name + ' & ' + user2.name;
                    }

                    if (otherUsersIds.length > 2) {
                        chatroom.name += ' & ' + (otherUsersIds.length - 2) + ' more';
                    }
                    chatroom['membersIds'] = otherUsersIds;
                }
            }
        }

        if (chatrooms) {
            setChatRooms(chatrooms);
        }
    };

    const fetchOtherUsers = async () => {
        const myId = getUserId();
        const accounts = await getAccounts();
        setOtherUsers(accounts.filter((account: Account) => account.id !== myId));
    };

    const checkIdOfChatroom = (userName: string): string => {
        let id: string = '';

        chatRooms.forEach((chatRoom) => {
            if (chatRoom.name.toLowerCase() === userName.toLowerCase()) {
                id = chatRoom.id;
            }
        });
        return id;
    }

    const handleCreateChatroom = async (userName: string | null) => {
        if (!userName) return;

        try {
            const userId = otherUsers.find((user) => user.name === userName)?.id;
            if (typeof userId === "string") {
                const newChatRoom = await createDirectChatroom(userId);
                window.location.href = `/messages/${newChatRoom.id}`;
                await fetchConversations();
            } else {
                throw new Error('User not found');
            }
        } catch (error: any) {
            if (error.status === 409) {
                const chatroomId = checkIdOfChatroom(userName);
                window.location.href = `/messages/${chatroomId}`;
            } else {
                mantineErrorNotification('Failed to create chatroom with ' + userName);
                console.error("Error creating chatroom:", error);
            }
        }
    };

    const handleSelectConversation = (id: string) => {
        window.location.href = `/messages/${id}`;
    };

    const handleAddUser = (event: any, newValue: Account | null) => {
        if (newValue && !selectedUsers.includes(newValue)) {
            setSelectedUsers([...selectedUsers, newValue]);
        }
    };

    const handleDeleteUser = (userToDelete: Account) => {
        setSelectedUsers(selectedUsers.filter((user) => user.id !== userToDelete.id));
    };

    const handleGroupChatCreation = async () => {
        try {
            const invitedAccountsUsernames = selectedUsers.map((user) => user.id);
            console.log(invitedAccountsUsernames);
            const newChatRoom = await createGroupChatroom(invitedAccountsUsernames, groupName);
            window.location.href = `/messages/${newChatRoom.id}`;
            await fetchConversations();
        } catch (error: any) {
            mantineErrorNotification('Failed to create group chat');
            console.error("Error creating group chat:", error);
        }
    }

    return (
        <div id="mainChatroomsList">
            <div id={'topOfMainChatoomList'}>
                <Autocomplete
                    options={otherUsers.map((user) => user.name)}
                    onChange={(event, newValue) => {
                        handleCreateChatroom(newValue);
                    }}
                    renderInput={(params) => <TextField {...params} label="Search Users" variant="outlined"/>}
                    style={{margin: "20px", maxWidth: "100%", flexGrow: 1}}
                />
                <Button color="primary" variant="contained" onClick={() => setModalOpen(true)}>Create group
                    chat</Button>
            </div>
            <div style={{display: 'flex', justifyContent: 'center', alignItems: 'center'}}>
                {
                    chatRooms.length > 0 ? <h2>Open Conversations</h2> :
                        <CircularProgress size={30} sx={{marginBottom: '20px'}}/>
                }
            </div>
            <ul id={'openConversationsList'} className={'custom-scrollbar'}>
                {chatRooms.map((chatRoom) => (
                    <li className={'singleConversationShortcut'} key={chatRoom.id}
                        onClick={() => handleSelectConversation(chatRoom.id)}>
                        <div className={'userGroupChatPictures'}>
                            <ProfilePicture accountId={chatRoom.membersIds[0]} isMyProfile={false} size={40}/>
                            {chatRoom.membersIds.length > 1 &&
                                <ProfilePicture accountId={chatRoom.membersIds[1]} isMyProfile={false} size={40}/>}
                        </div>
                        <div>{chatRoom.name}</div>
                        <div onClick={(e) => e.stopPropagation()}>
                            <LeaveChatroomModal
                                chatroom={chatRoom}
                                setRefreshConversations={setRefreshConversations}
                            />
                        </div>
                    </li>
                ))}
            </ul>
            <Modal open={modalOpen} onClose={() => setModalOpen(false)}>
                <Box sx={{...modalStyle}} onClick={(e) => e.stopPropagation()}>
                    <h2>Create Group Chat</h2>
                    <TextField
                        label="Group Chat Name"
                        variant="outlined"
                        fullWidth
                        margin="normal"
                        value={groupName}
                        onChange={(e) => setGroupName(e.target.value)}
                    />
                    <Autocomplete
                        options={otherUsers}
                        getOptionLabel={(option) => option.name}
                        onChange={handleAddUser}
                        renderInput={(params) => <TextField {...params} label="Add Users" variant="outlined"/>}
                    />
                    <div>
                        {selectedUsers.map((user) => (
                            <Chip
                                key={user.id}
                                label={user.name}
                                onDelete={() => handleDeleteUser(user)}
                                style={{margin: '5px'}}
                            />
                        ))}
                    </div>
                    <Button variant="contained" color="primary" onClick={handleGroupChatCreation}>
                        Create Group Chat
                    </Button>
                </Box>
            </Modal>
        </div>
    );
};

const modalStyle = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 400,
    bgcolor: 'background.paper',
    border: '2px solid #FFF',
    borderRadius: 8,
    boxShadow: 24,
    p: 4,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    '& > *': {
        width: '100%',
        margin: '10px 0',
    },
};